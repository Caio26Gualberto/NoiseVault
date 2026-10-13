---
paths:
  - "**/NoiseVault.Application/Media/**"
  - "**/NoiseVault.Application/Interfaces/IVideoTranscoder.cs"
  - "**/NoiseVault.Infra.Data/Media/**"
  - "**/NoiseVault.Infra.Data/Storage/**"
  - "**/NoiseVault.Workers/**/Media/**"
  - "**/NoiseVault.Api/Controllers/MediaController.cs"
  - "infra/localstack/**"
---

# Decisões — Mídia de post: fotos e vídeo

A mídia é enviada **antes** do post e vive como `MediaUpload` até ser anexada. O post (`feed.md`) só recebe ids. Imagem segue `armazenamento.md`; este arquivo cobre o vídeo e o ciclo de vida da mídia solta.

## `MediaUpload`: a área de preparo

`MediaUpload` (`OwnerUserId`, `Kind: Photo | Video`, `Status: Uploading | Processing | Ready | Failed`, `StorageKey`, `OriginalKey`, `MultipartUploadId`, `PosterKey`, `Width`, `Height`, `DurationSeconds`, `SizeBytes`, `FailureReason`, `ExpiresAt`). `PostMedia (PostId, MediaUploadId, Order)` liga ao post, com `MediaUploadId` único: a navegação `MediaUpload.Attachment` diz se a mídia já está em uso.

Substitui o desenho de `dominio.md`, que previa `StorageKey` e dimensões em `PostMedia`: com vídeo convertido em segundo plano, a mídia precisa existir e ter estado **antes** do post.

`ExpiresAt` é o prazo **da etapa atual**: 6 h para terminar o envio (`Uploading`), 2 h para converter (`Processing`) e 24 h para entrar num post (`Ready`/`Failed` solta).

## Foto: pela API

`POST /api/media/photos` (form, `[RequestSizeLimit]` 16 MB) usa o `ImageReplacement` com o preset `Post`: até 15 MB na entrada, mínimo 64 px, maior lado até 2048, WebP 82, **metadados removidos** (o GPS do celular não vaza). Chave `users/{userId}/photos/{guid}.webp`. Nasce `Ready`.

## Vídeo: direto para o S3, em partes

Decisão do usuário em 13/10/2026: "o upload igual o do YouTube", com o arquivo indo do navegador para o storage, sem passar pela API.

1. `POST /api/media/videos { sizeBytes, contentType }`. A API valida o tamanho (até **500 MB**, `VIDEO_TOO_LARGE`) e se o tipo é `video/*` (`VIDEO_UNSUPPORTED`).
   - Limite de **3 vídeos** em envio ou processamento por pessoa (`VIDEO_UPLOAD_LIMIT_REACHED`).
   - Abre um multipart no S3 em `uploads/users/{userId}/{guid}` e devolve uma URL pré-assinada de `PUT` por parte: 10 MB por parte, no máximo 50 partes, válidas por 6 h.
2. O navegador envia as partes direto ao S3, 4 em paralelo, até 3 tentativas por parte, com o progresso calculado no cliente (`frontend/src/api/multipart-upload.ts`). Ele lê o `ETag` de cada resposta, por isso o bucket expõe `ETag` no CORS.
3. `POST /api/media/videos/{id}/complete { parts }`.
   - A lista precisa ter todas as partes, de 1 a N, com ETag; senão `VIDEO_UPLOAD_INCOMPLETE`.
   - A API chama `CompleteMultipartUpload` e confere o tamanho real com `HeadObject`. Passou de 500 MB: apaga e devolve `VIDEO_TOO_LARGE`.
   - Muda para `Processing` e enfileira `TranscodeVideo(mediaId)` na fila `media`.
   - Chamada fora de `Uploading` → `VIDEO_UPLOAD_NOT_PENDING`.
4. `GET /api/media/{id}` devolve o status (só para o dono). `DELETE /api/media/{id}` apaga mídia solta (aborta o multipart se ainda estiver enviando); mídia em post → `MEDIA_IN_USE`.

**Por que não pela API:** 500 MB atravessando o Kestrel prenderia uma conexão e memória por minutos, e a API teria que repassar tudo ao S3 de novo. Com URL pré-assinada, a API só autoriza; o tráfego pesado vai direto ao storage, que é o que ele faz bem.

A duração (até 10 min) é checada no navegador pelos metadados, para recusar cedo. **Quem decide é o servidor**, no `ffprobe` do job: o navegador não lê metadado de todo formato.

## Conversão: ffmpeg num job

Sem conversão, vídeo de iPhone (`.mov`/HEVC) não toca no Chrome e o arquivo sai do tamanho que o celular gravou. O job `MediaEnqueuedJobsTrigger.TranscodeVideoAsync` → `PostAppService.ProcessVideo` → `MediaUploadAppService.ProcessVideo`:

1. Baixa o original para uma pasta temporária e roda o `ffprobe`. As rejeições definitivas viram `Failed`, com `VIDEO_REJECTED` no log e sem retry:
   - não lê → `UNSUPPORTED`;
   - sem stream de vídeo → `NO_VIDEO_STREAM`;
   - mais de 10 min (+1 s de tolerância) → `TOO_LONG`.
2. `ffmpeg` gera **MP4 H.264** (`yuv420p`, perfil high, `veryfast`, CRF 23), **AAC 128k estéreo** e `+faststart`. O vídeo é **reduzido até o lado menor ter 720 px**, sem aumentar vídeo menor; vertical continua vertical (1080×1920 vira 720×1280).
3. Frame de 1 s (ou 0 em vídeo curto) vira o poster, processado pelo `ImageSharpProcessor` (preset `Post`, WebP).
4. Sobe `users/{userId}/videos/{guid}/video.mp4` e `poster.webp`, grava largura, altura, duração e tamanho, marca `Ready`, apaga o original e publica os posts que esperavam (`feed.md`).

- **Retry**: 2 tentativas extras (60 s e 300 s) para falha transitória (download, upload, ffmpeg que quebrou). A **última** tentativa marca `Failed` com `TRANSCODE_FAILED` e loga `VIDEO_TRANSCODE_FAILED` (Error). As anteriores logam `VIDEO_TRANSCODE_RETRY` (Warning) e relançam. O trigger lê o `RetryCount` do Hangfire para saber se é a última.
- **Isolamento**: a fila `media` roda num segundo servidor do Hangfire (`noisevault-media`, `WorkerCount = 2`). Conversão é CPU pesada; sem isso, 20 vídeos ocupariam todos os workers do servidor principal.
- **Configuração**: `Video:FfmpegPath` e `Video:FfprobePath` (padrão `ffmpeg`/`ffprobe` no PATH), `Video:ShortSideMax` (720) e `Video:Crf` (23). Cada processo tem teto de 30 min e é morto ao estourar. Os argumentos vão em `ArgumentList`, nunca numa string de shell.
- **Por que MP4 720p progressivo e não HLS**: toca em qualquer navegador sem player extra, e o `faststart` começa a tocar antes de baixar tudo. HLS adaptativo é a evolução, e é o que o MediaConvert entrega pronto (abaixo). O contrato não muda: a tela recebe `url` e `posterUrl`.

## Limpeza: `media:cleanup` de hora em hora

| Situação | O que faz |
|---|---|
| `Uploading` com `ExpiresAt` vencido (envio abandonado) | `AbortMultipartUpload` e apaga a linha |
| `Processing` com `ExpiresAt` vencido (job perdido) | `Failed` com `PROCESSING_TIMEOUT` |
| `Ready`/`Failed` solta há mais de 24 h | apaga os arquivos e a linha |
| anexada a post apagado e ainda com chave | apaga os arquivos e zera as chaves |

**Rede de segurança no bucket:** regra de lifecycle `AbortIncompleteMultipartUpload` de 1 dia no prefixo `uploads/`. Partes de multipart não completado são cobradas e não aparecem na listagem normal; sem a regra, um abort que falhou vira custo invisível.

## Bucket: CORS e lifecycle

`infra/localstack/ready.d/10-s3.sh` aplica, a cada boot:
- **CORS**: origem `http://localhost:5173`, métodos `GET`/`PUT`, `ExposeHeaders: ETag`;
- **lifecycle**: a regra acima.

Para um LocalStack que já estava de pé: `docker exec noisevault-localstack bash /etc/localstack/init/ready.d/10-s3.sh`.

## Produção: AWS MediaConvert

O ffmpeg no processo da API serve para dev e para o começo. Em produção na AWS, a conversão vai para o **AWS Elemental MediaConvert**. Decisão registrada para quando houver volume, não implementada:

- **Disparo**: o `CompleteVideo` continua igual, mas em vez de enfileirar o job do ffmpeg ele cria um job do MediaConvert (`CreateJob`), com o original em `s3://…/uploads/…` e um template de saída.
- **Saída**:
  - HLS adaptativo em 360p, 720p e 1080p (até a resolução do original);
  - um MP4 720p de fallback;
  - thumbnails, tudo em `users/{userId}/videos/{guid}/`, servidos pelo **CloudFront**.
  - `StorageKey` passa a apontar para o `.m3u8`; a tela troca o `<video src>` por um player HLS (hls.js), porque o Chrome não toca HLS nativo.
- **Conclusão**: o MediaConvert publica o fim do job no **EventBridge**, que entrega numa fila **SQS**. Um consumidor (job do Hangfire lendo a fila, ou endpoint de webhook com assinatura) marca `Ready`/`Failed` e chama a mesma publicação de posts.
- **O que muda no código**: `IVideoTranscoder` ganha uma implementação assíncrona (disparar e esperar evento), e o `ProcessVideo` se divide em "disparar" e "concluir". Entidade, estados, DTO e tela continuam iguais.
- **Infra**:
  - IAM role do MediaConvert com leitura em `uploads/` e escrita em `users/`;
  - fila SQS com DLQ;
  - CORS do bucket real com o domínio de produção;
  - a regra de lifecycle de multipart;
  - bucket privado + CloudFront com OAC (`armazenamento.md`); o `PUT` pré-assinado continua indo direto ao S3.
- **Custo**: MediaConvert cobra por minuto de saída e por resolução. Com 10 min de teto por vídeo, o custo fica previsível. Medir antes de ligar 1080p para todo mundo.

## Eventos de log (contexto `Media`)

| Evento | Nível | Dados |
|---|---|---|
| `VIDEO_UPLOAD_STARTED` | Info | `MediaId`, `SizeBytes`, `Parts` |
| `VIDEO_UPLOAD_COMPLETED` | Info | `MediaId`, `SizeBytes` |
| `VIDEO_ENQUEUE_FAILED` | Error | `MediaId` (a limpeza marca `Failed` depois de 2 h) |
| `VIDEO_TRANSCODED` | Info | `MediaId`, `Width`, `Height`, `DurationSeconds`, `SizeBytes` |
| `VIDEO_REJECTED` | Info | `MediaId`, `Reason` |
| `VIDEO_TRANSCODE_RETRY` | Warning | `MediaId`, erro |
| `VIDEO_TRANSCODE_FAILED` | Error | `MediaId`, exceção |
| `MEDIA_TEMP_CLEANUP_FAILED` | Warning | pasta temporária que sobrou |
| `MEDIA_CLEANED` | Info | contagem por situação |

No contexto `Storage`, o multipart acrescenta `STORAGE_MULTIPART_START_FAILED` (Error), `STORAGE_MULTIPART_COMPLETE_FAILED` (Warning), `STORAGE_MULTIPART_ABORT_FAILED` (Warning) e `STORAGE_DOWNLOAD_FAILED` (Error).

## Roteiro de verificação

Pré-requisito: ffmpeg instalado (`winget install Gyan.FFmpeg`; se a API não enxergar o PATH novo, passar `Video__FfmpegPath` e `Video__FfprobePath`). Arquivos de teste gerados com o próprio ffmpeg: `testsrc2` 1920×1080 de 40 s (~100 MB), `testsrc` 1080×1920 em `.mov`, e um `.mp4` que é texto.

1. **Foto.** `POST /api/media/photos` → `PHOTO_UPLOADED`, a URL responde `image/webp`. Um arquivo de texto → `IMAGE_UNSUPPORTED`.
2. **Validação.** `start` com 501 MB → `VIDEO_TOO_LARGE`; `contentType: image/png` → `VIDEO_UNSUPPORTED`.
3. **Multipart.** O vídeo de 100 MB gera 10 URLs. Cada `PUT` com `Origin: http://localhost:5173` responde com `ETag` e `Access-Control-Expose-Headers: ETag`.
   - `complete` faltando parte → `VIDEO_UPLOAD_INCOMPLETE`; por outra conta → `MEDIA_NOT_FOUND`.
   - Certo → `VIDEO_UPLOAD_COMPLETED` com `status: 1`; de novo → `VIDEO_UPLOAD_NOT_PENDING`.
4. **Conversão.** Em segundos: `status: 2`, 1280×720, 40 s, MP4 e poster respondendo 200 no LocalStack; `VIDEO_TRANSCODED` no log. O `.mov` vertical sai 720×1280. O falso termina `status: 3` com `VIDEO_REJECTED`.
5. **Mídia solta.** `DELETE` de mídia em post → `MEDIA_IN_USE`; solta → `MEDIA_DELETED`.
6. **Limpeza.** Um `start` sem `complete` e uma foto solta com `ExpiresAt` forçado no passado; disparar `media:cleanup` no `/hangfire` (`POST /hangfire/recurring/trigger`, `jobs[]=media:cleanup`) → as duas somem (`MEDIA_NOT_FOUND`) e sai `MEDIA_CLEANED`.
