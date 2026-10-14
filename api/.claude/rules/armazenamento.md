---
paths:
  - "**/NoiseVault.Infra.Data/Storage/**"
  - "**/NoiseVault.Infra.Data/Media/**"
  - "**/NoiseVault.Application/Interfaces/IFileStorage.cs"
  - "**/NoiseVault.Application/Interfaces/IImageProcessor.cs"
  - "**/NoiseVault.Application/Common/Media/**"
---

# Decisões — Armazenamento de arquivos e imagens

Todo arquivo que a comunidade envia (avatar, capa e mídia de post hoje; fotos de galeria e banner de evento depois) passa por aqui. O ambiente de produção é **AWS**: S3 para guardar, CloudFront para servir. Em dev, o **LocalStack** emula o S3 no docker-compose.

## Duas camadas: o que fazer com a imagem e onde guardar

- **`IImageProcessor`** (`Application/Interfaces/`, implementado em `Infra.Data/Media/ImageSharpProcessor.cs`) recebe o stream enviado e um `EImagePreset`, e devolve a imagem pronta (bytes WebP, largura, altura) ou uma rejeição tipada (`Unsupported`, `TooSmall`). Ele **decodifica de verdade**: um `.txt` renomeado para `.png` não passa, e o `Content-Type` do cliente é ignorado.
- **`IFileStorage`** (`Application/Interfaces/`, implementado em `Infra.Data/Storage/S3FileStorage.cs`) só sabe guardar, apagar e montar a URL pública de uma chave. Não conhece imagem, usuário nem regra.
- O AppService dono do dado (perfil e banda) orquestra: valida o tamanho → processa → guarda → grava a chave no banco → apaga a antiga. A sequência comum mora em `Application/Common/Media/ImageReplacement.cs` (`UploadAsync` valida, processa e sobe; `SaveAsync` grava, desfaz o upload se a gravação falhar e apaga a antiga), extraída quando a banda virou o segundo caso. A `Application` nunca vê o SDK da AWS nem o ImageSharp.

## Presets: o servidor decide o tamanho final

| Preset | Entrada máxima | Mínimo | Saída |
|---|---|---|---|
| `Avatar` | 5 MB | 128×128 | 512×512, corte central quadrado |
| `Banner` | 10 MB | 800 de largura | até 1600 de largura, proporção original, sem corte |

- Formatos aceitos na entrada: JPEG, PNG, WebP. GIF animado e HEIC ficam fora (HEIC exigiria lib nativa).
- Saída sempre **WebP** qualidade 82, com **todos os metadados removidos** (EXIF, ICC, XMP). Foto de celular carrega GPS no EXIF; publicar o original vazaria onde a pessoa mora.
- Orientação EXIF é aplicada antes de remover os metadados (`AutoOrient`), senão foto de celular fica deitada.
- A capa **não é cortada no servidor**: o corte vertical é a `BannerPosition` (0–100, `object-position: center Y%`) que o front aplica. Assim a pessoa reposiciona sem reenviar.
- O limite de tamanho é checado **antes** de decodificar (`IFormFile.Length`) e reforçado por `[RequestSizeLimit]` no controller. Decodificar 100 MB para depois recusar é o caminho para derrubar a API.

## Chaves: previsíveis no prefixo, imprevisíveis no nome

`users/{userId}/avatar/{guid}.webp`, `users/{userId}/banner/{guid}.webp`, `bands/{bandId}/avatar/{guid}.webp`, `bands/{bandId}/banner/{guid}.webp`, `events/{eventId}/banner/{guid}.webp` (banner de evento), `users/{userId}/photos/{guid}.webp` (foto de post e do mural; o preset `Archive` só muda o tamanho) e `users/{userId}/videos/{guid}/` (vídeo convertido: `master.m3u8`, uma pasta por resolução com `index.m3u8` e segmentos `.ts`, e `poster.webp`; os primeiros vídeos, de antes do HLS, têm `video.mp4`). O original do vídeo fica em `uploads/users/{userId}/{guid}` só até a conversão, e é o único prefixo com regra de lifecycle (`midia-e-video.md`). O prefixo por dono facilita limpeza e auditoria; o GUID impede adivinhar a URL de uma imagem antes de ela ser publicada e garante que **trocar a imagem troca a URL** — cache de CDN e de navegador nunca serve a imagem velha, então não existe invalidação.

O legado montava `avatars/{id}{guid}` sem separador nem extensão e usava título digitado pelo usuário na chave de foto. Nada disso volta: chave nunca contém texto do usuário.

## Ordem: sobe o novo, grava, só então apaga o antigo

1. Processa e guarda o arquivo novo (`PutAsync`).
2. Grava a chave nova no banco.
3. **Depois do commit**, apaga a chave antiga (`DeleteAsync`). Falha aqui só gera `STORAGE_DELETE_FAILED` (Warning): sobra um arquivo órfão, nunca um perfil apontando para arquivo apagado.

Se o passo 2 falhar, o arquivo recém-subido é apagado na hora (best effort). O legado fazia o contrário — apagava o antigo antes de subir o novo — e uma falha deixava o perfil sem imagem.

## Leitura: URL pública fixa, não URL assinada

Avatar e capa são públicos por natureza. A URL é `{Storage:PublicBaseUrl}/{key}`, montada sem ida à AWS.

- **Dev**: `PublicBaseUrl` aponta para o LocalStack em path-style (`http://localhost:4566/noisevault-media`).
- **Produção**: bucket **privado** + **CloudFront** com Origin Access Control; `PublicBaseUrl` é o domínio do CloudFront. Bucket público direto é desaconselhado pela própria AWS (Block Public Access) e não dá cache de borda.
- O legado gerava uma URL assinada por imagem, guardava no Redis por mais tempo do que a URL valia e servia link morto. Nada de URL assinada nem Redis para mídia pública. URL assinada volta só se houver mídia privada de verdade.

## Dev: LocalStack, só S3

- `docker-compose.yml` sobe `localstack/localstack:4.14.0` (versão fixada) com `SERVICES=s3`, porta 4566. O init hook `infra/localstack/ready.d/10-s3.sh` cria o bucket `noisevault-media` com política de leitura pública a cada boot (idempotente).
- A versão fixada (4.14.0, fevereiro/2026) **sobe sem token**. As imagens mais novas passaram a exigir um token gratuito: o compose já repassa `LOCALSTACK_AUTH_TOKEN` do `.env` da raiz (fora do Git; `.env.example` mostra a forma), vazio por padrão. Ao subir a versão, criar a conta e preencher o token — o README explica.
- 🔴 **O bucket de dev não sobrevive a um restart.** A imagem Community guarda o S3 em memória: o `PERSISTENCE: "1"` do compose é ignorado sem um plano da LocalStack que inclua persistência, e `/var/lib/localstack/state` fica vazio. Parar o container (ou reiniciar o Docker) apaga todos os arquivos, enquanto o Postgres continua com as chaves: avatar, capa e mídia aparecem quebrados. Verificado em 14/10/2026. A troca por MinIO foi tentada e caiu porque a imagem `minio/minio` saiu do Docker Hub (e do quay.io); SeaweedFS e Garage baixam e seriam as alternativas. **Decisão do usuário: manter o LocalStack** e aceitar que imagem de dev é descartável. Depois de um restart, o caminho é limpar o banco ou subir as imagens de novo.
- A API usa o mesmo `AWSSDK.S3` da produção: em dev `Storage:ServiceUrl` (`http://localhost:4566`) liga `ForcePathStyle` e as credenciais `test`/`test` do `appsettings.Development.json` (não são segredo). Em produção `ServiceUrl` fica vazio e as credenciais vêm da **cadeia padrão da AWS** (IAM role da task) — nenhuma chave em configuração.
- **Escrita direta pelo navegador** existe só para vídeo: `PUT` de parte por URL pré-assinada (`IFileStorage.StartMultipartAsync`, `PresignPartUrl`, `CompleteMultipartAsync`, `AbortMultipartAsync`), com CORS e lifecycle no bucket (`midia-e-video.md`). A leitura continua pública e fixa.
- SES, SQS e outros serviços ficam de fora até terem um caso real (o e-mail continua no Mailpit, ver `email.md`).

## Licença do ImageSharp

SixLabors.ImageSharp 4.x é 100% gerenciado (roda no container sem lib nativa, ao contrário de SkiaSharp). A licença Six Labors Split é gratuita para projeto open source ou com receita anual abaixo de US$ 1 milhão. **Risco aceito em 13/10/2026**: projeto pessoal, sem receita. Se o projeto virar negócio acima do teto, trocar a implementação de `IImageProcessor` não toca no resto.

## Eventos de log (contexto `Storage`)

| Evento | Nível | Quando |
|---|---|---|
| `STORAGE_UPLOADED` | Info | objeto gravado: `Key`, `Bytes` |
| `STORAGE_UPLOAD_FAILED` | Error | `PutObject` falhou; o caso de uso devolve `STORAGE_UNAVAILABLE` |
| `STORAGE_DELETE_FAILED` | Warning | apagar a chave antiga falhou; sobra órfão |
| `IMAGE_REJECTED` | Info | imagem recusada: `Preset`, `Reason` (nunca o conteúdo) |

## Roteiro de verificação

1. `docker compose up -d` → `curl http://localhost:4566/_localstack/health` mostra `"s3": "running"` (ou `available`); `curl http://localhost:4566/_localstack/init` mostra `10-s3.sh` como `SUCCESSFUL`.
2. `curl -I http://localhost:4566/noisevault-media/inexistente.webp` → 404 (bucket existe e responde anônimo).
3. Subir um avatar pela API (`perfil-e-midia.md`) → `STORAGE_UPLOADED` no log; a `avatarUrl` abre no navegador e é WebP 512×512; `exiftool`/propriedades do arquivo não mostram GPS.
4. Trocar o avatar → a chave antiga responde 404 no LocalStack.
5. Parar o LocalStack e subir avatar → 500 `STORAGE_UNAVAILABLE` + `STORAGE_UPLOAD_FAILED` no log; o perfil continua com o avatar anterior.
