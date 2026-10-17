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

## Perfis: feed e arquivo

`MediaUpload.Profile` (`EMediaProfile`: `Post` 0, `Archive` 1, `Support` 2) define os limites (`MediaRules.For`):

| Perfil | Vídeo | Foto | Prazo de processamento | Quem sobe |
|---|---|---|---|---|
| `Post` | 500 MB, 10 min | 15 MB, lado maior até 2048 | 2 h | qualquer pessoa (`/api/media`) |
| `Archive` | 4 GB, 2 h | 40 MB, lado maior até 3072 | 8 h | `Archive.Create` (`/api/admin/archive/media`) e, para o envio da comunidade, qualquer pessoa (`/api/archive/media`, `mural.md`) |
| `Support` | 200 MB, 2 min | 15 MB, lado maior até 2048 | 2 h | qualquer pessoa (`/api/support/media`, `suporte.md`) |

- A mídia só entra no lugar do próprio perfil (`ListAttachable` filtra o perfil): foto de feed não vai para o mural, e vice-versa.
- `MediaUpload.ArchiveAttachment` é o vínculo com o mural, e `MediaUpload.SupportAttachment`, com o ticket de suporte. A limpeza e o `DELETE /api/media/{id}` tratam os dois como "em uso", igual ao `Attachment` do post. Ticket nunca é apagado, então a mídia dele não tem o caso "anexada a algo apagado".
- O teto de cada processo do ffmpeg é `Video:ProcessTimeoutMinutes` (padrão 240), para caber uma fita inteira.

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
2. Um único `ffmpeg` gera **HLS adaptativo** (decisão do usuário em 13/10/2026: "igual o YouTube", porque a produção vai precisar converter assim de qualquer jeito):
   - **escada de resoluções pelo lado menor**: 360p (800 kbps), 480p (1400), 720p (2800) e 1080p (5000). Só entram as que **não passam do original**: um vídeo 720p gera 360/480/720. Original menor que 360p vira uma rendição só, no tamanho dele. Vertical continua vertical (1080×1920 gera 1080p = 1080×1920). Teto em `Video:MaxShortSide` (1080);
   - **fps do original, até 60** (`-fpsmax 60`); acima de 30 fps o teto de bitrate sobe 50%. O `master.m3u8` ganha `FRAME-RATE` em cada variante, que o player usa para mostrar "1080p60";
   - H.264 (`yuv420p`, high, `veryfast`) com **CRF 23 limitado por `maxrate`/`bufsize`** por rendição: qualidade constante, sem passar do teto da rede. AAC 128k estéreo em todas as variantes (se o original tiver áudio);
   - segmentos de **4 s** (`.ts`), keyframe forçado a cada 2 s em todas as rendições, para a troca de qualidade cair sempre em fronteira de segmento; `-hls_playlist_type vod` e `independent_segments`;
   - o `ffprobe` lê a rotação do celular (`side_data`) para decidir a escada pelo lado certo;
   - **desentrelaçamento**: quando o `field_order` do original não é `progressive`, o caso de VHS e câmera de fita digitalizados, o filtro começa com `bwdif=mode=send_frame` (também no poster). Sem isso aparecem os "pentes" nas cenas de movimento. O log `VIDEO_TRANSCODED` traz `Deinterlaced` e `Profile`. VHS (480 linhas) gera só 360p e 480p.
3. Frame de 1 s (ou 0 em vídeo curto), reduzido a no máximo 720p, vira o poster, processado pelo `ImageSharpProcessor` (preset `Post`, WebP).
4. Sobe a pasta inteira em `users/{userId}/videos/{guid}/` (`master.m3u8`, uma pasta por rendição `360p/`, `480p/`… com `index.m3u8` e os segmentos, e `poster.webp`), 4 arquivos em paralelo, com `Content-Type` por extensão (`application/vnd.apple.mpegurl`, `video/mp2t`). Se algum arquivo falhar, apaga o que subiu e relança (retry do job). `StorageKey` aponta para o `master.m3u8`; largura e altura são as da maior rendição; `SizeBytes` é a soma da pasta. Marca `Ready`, apaga o original e publica os posts que esperavam (`feed.md`).
   - Apagar mídia de vídeo HLS apaga **o prefixo da pasta** (`IFileStorage.DeletePrefixAsync`, `ListObjectsV2` + `DeleteObjects`), não chave a chave.
   - **Vídeos convertidos antes do HLS** (`video.mp4`, 720p) continuam valendo e não foram reconvertidos (decisão do usuário): o player toca MP4 direto, sem menu de qualidade.

- **Retry**: 2 tentativas extras (60 s e 300 s) para falha transitória (download, upload, ffmpeg que quebrou). A **última** tentativa marca `Failed` com `TRANSCODE_FAILED` e loga `VIDEO_TRANSCODE_FAILED` (Error). As anteriores logam `VIDEO_TRANSCODE_RETRY` (Warning) e relançam. O trigger lê o `RetryCount` do Hangfire para saber se é a última.
- **Isolamento**: a fila `media` roda num segundo servidor do Hangfire (`noisevault-media`, `WorkerCount = 2`). Conversão é CPU pesada; sem isso, 20 vídeos ocupariam todos os workers do servidor principal.
- **Configuração**: `Video:FfmpegPath` e `Video:FfprobePath` (padrão `ffmpeg`/`ffprobe` no PATH), `Video:MaxShortSide` (1080) e `Video:Crf` (23). Cada processo tem teto de 30 min e é morto ao estourar. Os argumentos vão em `ArgumentList`, nunca numa string de shell.
- **Custo do HLS**: a conversão leva umas 3 a 4 vezes mais que um MP4 único, e o armazenamento por vídeo mais ou menos dobra. Em troca, quem está no 4G recebe 360p sem travar e quem está no Wi-Fi recebe 1080p, e o player deixa escolher. O contrato não mudou: a tela recebe `url` (agora `.m3u8`) e `posterUrl`.
- **Por que `.ts` e não fMP4 (CMAF)**: `.ts` toca em todo player HLS, inclusive em Safari antigo. Quando houver DASH ou DRM, CMAF passa a valer a pena; o MediaConvert gera os dois.

## Limpeza: `media:cleanup` de hora em hora

| Situação | O que faz |
|---|---|
| `Uploading` com `ExpiresAt` vencido (envio abandonado) | `AbortMultipartUpload` e apaga a linha |
| `Processing` com `ExpiresAt` vencido (job perdido) | `Failed` com `PROCESSING_TIMEOUT` |
| `Ready`/`Failed` solta há mais de 24 h | apaga os arquivos e a linha |
| anexada a post apagado e ainda com chave | apaga os arquivos e zera as chaves |

As marcações de pessoas (`PhotoTag`, `marcacoes.md`) são filhas da mídia com FK `Cascade`: quando a linha do `MediaUpload` é apagada, elas saem junto.

**Rede de segurança no bucket:** regra de lifecycle `AbortIncompleteMultipartUpload` de 1 dia no prefixo `uploads/`. Partes de multipart não completado são cobradas e não aparecem na listagem normal; sem a regra, um abort que falhou vira custo invisível.

## Bucket: CORS e lifecycle

`infra/localstack/ready.d/10-s3.sh` aplica, a cada boot:
- **CORS**: origem `http://localhost:5173`, métodos `GET`/`PUT`, `ExposeHeaders: ETag`. O `GET` com CORS é também o que deixa o hls.js baixar playlist e segmentos (ele usa XHR, não `<video src>`);
- **lifecycle**: a regra acima.

Para um LocalStack que já estava de pé: `docker exec noisevault-localstack bash /etc/localstack/init/ready.d/10-s3.sh`.

## Produção: AWS MediaConvert

O ffmpeg no processo da API serve para dev. Em produção, a conversão vai para o **AWS Elemental MediaConvert**: o servidor é um Lightsail pequeno, de CPU "burstable", e uma fita de 2 h derrubaria a API (decisão de 14/10/2026, `hospedagem.md`). **Implementado em 17/10/2026**, a pedido do usuário, antes do primeiro deploy. O MediaConvert é serviço gerenciado: não se instala nada, a API cria o job e consulta o resultado.

### O motor é escolhido por configuração

`Video:Engine` = `Ffmpeg` (padrão; dev) ou `MediaConvert` (produção). Sem `if` de ambiente: `StorageRegistration` registra `IRemoteVideoTranscoder` como `MediaConvertVideoTranscoder` (com o `IAmazonMediaConvert` nas mesmas credenciais e região do S3) ou como `DisabledRemoteVideoTranscoder` (`Enabled = false`). O `IVideoTranscoder` do ffmpeg continua registrado sempre, porque o seed de dev usa. Com `MediaConvert`, `MediaConvert:RoleArn` é obrigatório (o boot lança sem ele); `MediaConvert:QueueArn` e `MediaConvert:Endpoint` são opcionais (fila padrão e endpoint regional do SDK).

### Disparar e concluir, em dois jobs

O mesmo `TranscodeVideoAsync` da fila `media` chama `MediaUploadAppService.ProcessVideo`; com o motor remoto ligado, ele vira **`SubmitRemoteAsync`**: escolhe a pasta `users/{userId}/videos/{guid}/`, cria o job (`CreateJob`) com o original em `s3://…/uploads/…`, guarda `TranscodeJobId` e já grava `StorageKey` = `{pasta}/master.m3u8` (a mídia segue `Processing`, então o DTO ainda não expõe a URL). Falha no `CreateJob` segue o retry do Hangfire, e a última tentativa marca `Failed` (`VIDEO_TRANSCODE_FAILED`), como no ffmpeg.

O job recorrente **`media:transcode-poll`** (a cada minuto, só registrado quando o motor remoto está ligado; com ffmpeg ele é removido no boot) chama `PollRemoteTranscodes`: para cada mídia `Processing` com `TranscodeJobId`, um `GetJob`:
- **em andamento**: nada;
- **erro ou cancelado**: apaga o prefixo da pasta, `Failed` com `TRANSCODE_FAILED`, `VIDEO_TRANSCODE_FAILED` no log com a mensagem do MediaConvert;
- **completo**: `FinalizeRemoteAsync` lê largura, altura e duração dos `OutputGroupDetails` do job, recusa com `TOO_LONG` se passou do limite do perfil (apagando a saída; não há `ffprobe` antes, então a duração só é conhecida no fim), acha o poster, soma o tamanho da pasta (`IFileStorage.SizeOfPrefixAsync`), marca `Ready`, apaga o original e loga `VIDEO_TRANSCODED` com o `JobId`. O trigger então chama `PostAppService.PublishPendingPosts` (extraído do `ProcessVideo`), que publica os posts que esperavam e avisa os seguidores da banda (`feed.md`).
- `GetJob` que falha (rede, credencial) loga `VIDEO_TRANSCODE_POLL_FAILED` (Warning) e tenta no minuto seguinte. A consulta não é cobrada. Quando o volume pedir, o fim do job passa a chegar pelo EventBridge numa fila SQS; o polling fica como rede de segurança.
- `media:cleanup` continua marcando `PROCESSING_TIMEOUT` quando o prazo do perfil vence; com `TranscodeJobId` ele também apaga a pasta de saída. O job no MediaConvert não é cancelado: ele termina sozinho e a pasta órfã some no próximo apagar da mídia.

### O job reproduz a escada do ffmpeg

Montado em código (`MediaConvertVideoTranscoder`), não em template do console, para a mudança de escada ser um commit:
- **Entrada**: `Rotate: AUTO` (a rotação do celular), seletor de áudio padrão;
- **Grupo Apple HLS**, destino `s3://{bucket}/{pasta}/master`: o manifesto principal sai como `master.m3u8`, e cada rendição como `master_N.m3u8` com segmentos `master_N_00001.ts` **na mesma pasta** (`SINGLE_DIRECTORY`); o layout por subpasta do ffmpeg (`360p/index.m3u8`) não é reproduzido, e não precisa: `StorageKey` aponta para o `master.m3u8` e apagar é por prefixo. Segmentos de 4 s, `STREAM-INF` com resolução e `FRAME-RATE` (o player mostra "1080p60");
- **Automated ABR**: até 4 rendições entre 600 kbps e 5 Mbps, escolhidas pelo MediaConvert a partir do original, **nunca acima dele**; é o equivalente da escada 360/480/720/1080 limitada pelo lado menor. H.264 High, QVBR, GOP de 2 s fechado, detecção de corte de cena, fps do original. AAC 128k estéreo 48 kHz;
- **Desentrelaçamento** `ADAPTIVE`: só age quando o original é entrelaçado (VHS e fita digitalizada), como o `bwdif` condicional do ffmpeg;
- **Poster**: grupo File com Frame Capture JPEG (qualidade 80), 1 frame por segundo, 2 capturas: a API prefere `poster.0000001.jpg` (1 s) e cai para `poster.0000000.jpg` em vídeo curto, o mesmo critério do ffmpeg. O poster fica **JPEG do tamanho do original**, não WebP 720p: converter exigiria baixar o frame no servidor, e a diferença de bytes não paga a ida.

**O primeiro vídeo em produção (17/10/2026) recusou o job**: com Automated ABR o `qualityTuningLevel` do H.264 é obrigatório (`BadRequestException: qualityTuningLevel is a required property`); entrou `MULTI_PASS_HQ`, que é o que o ABR automático exige. Visto no log como `VIDEO_TRANSCODE_RETRY`, sem ir ao painel.

**O que o primeiro vídeo em produção precisa confirmar**, porque o LocalStack não emula o MediaConvert e nada disto rodou antes do deploy: o job é aceito com o Automated ABR sem `QvbrSettings` explícito; vídeo **sem áudio** não falha por causa do seletor padrão; o poster de 1 s existe; o `master.m3u8` toca no hls.js pelo CloudFront com os nomes `master_N`. Qualquer ajuste entra nesta seção.

### Infra

- **IAM role do MediaConvert** (`noisevault-mediaconvert`, trust `mediaconvert.amazonaws.com`): `s3:GetObject` em `uploads/*`, `s3:PutObject` em `users/*`;
- **usuário IAM da API**: `mediaconvert:CreateJob`, `mediaconvert:GetJob`, `mediaconvert:DescribeEndpoints` e `iam:PassRole` só nessa role (o Lightsail não tem IAM role, `hospedagem.md`);
- CORS do bucket e a **Response Headers Policy** do CloudFront com CORS para `GET` (o hls.js baixa `.m3u8` e `.ts` por XHR); cache longo (as chaves têm GUID);
- **Custo**: por minuto de saída e por resolução. Ordem de grandeza: 0,30 a 0,50 USD por vídeo de 10 min em 1080p com 4 rendições, uns 2 USD por fita VHS de 2 h. Mês sem vídeo custa zero. Medir antes de liberar vídeo longo para todo mundo.
- O `Dockerfile` continua instalando o ffmpeg: é a saída de emergência (`Video__Engine=Ffmpeg`) se o MediaConvert falhar no primeiro dia.

## Eventos de log (contexto `Media`)

| Evento | Nível | Dados |
|---|---|---|
| `VIDEO_UPLOAD_STARTED` | Info | `MediaId`, `SizeBytes`, `Parts` |
| `VIDEO_UPLOAD_COMPLETED` | Info | `MediaId`, `SizeBytes` |
| `VIDEO_ENQUEUE_FAILED` | Error | `MediaId` (a limpeza marca `Failed` depois de 2 h) |
| `VIDEO_TRANSCODED` | Info | `MediaId`, `Width`, `Height`, `DurationSeconds`, `SizeBytes` |
| `VIDEO_REJECTED` | Info | `MediaId`, `Reason` |
| `VIDEO_TRANSCODE_RETRY` | Warning | `MediaId`, erro |
| `VIDEO_TRANSCODE_SUBMITTED` | Info | `MediaId`, `Profile`, `JobId` (motor remoto) |
| `VIDEO_TRANSCODE_POLL_FAILED` | Warning | `MediaId`, `JobId`, erro do `GetJob`; tenta no minuto seguinte |
| `MEDIACONVERT_JOB_CREATED` | Info | `MediaId`, `JobId` |
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
