# Noise Vault

Comunidade online de metal e rock do ABC Paulista. Monorepo com `api/` (.NET 10) e `frontend/` (React 19 + Vite).

## Pré-requisitos

- .NET SDK 10 e `dotnet-ef` (`dotnet tool install -g dotnet-ef`)
- Node 24+
- Docker com Compose v2
- ffmpeg no PATH (`winget install Gyan.FFmpeg`): converte os vídeos dos posts. Sem ele, o upload funciona e o vídeo termina como falha de processamento

## Subindo em desenvolvimento

```bash
docker compose up -d                                  # Postgres (5436) + Mailpit (http://localhost:8025) + LocalStack S3 (4566)
cd api && dotnet run --project src/NoiseVault.Api     # http://localhost:5041 — /swagger, /health, /hangfire
cd frontend && npm install && npm run dev             # http://localhost:5173
```

⚠️ O LocalStack Community guarda os arquivos em memória: parar o container apaga avatares, capas e mídias, mas o banco continua apontando para eles. Imagem de dev é descartável; depois de um restart, limpe o banco ou suba as imagens de novo (`api/.claude/rules/armazenamento.md`).

O LocalStack está fixado na 4.14.0, que roda sem conta. Ao atualizar a imagem para uma versão mais nova, crie uma conta gratuita em https://app.localstack.cloud, copie o auth token e coloque em `LOCALSTACK_AUTH_TOKEN` num `.env` na raiz (veja `.env.example`; o `.env` não vai para o Git).

Se o LocalStack já estava rodando antes de uma mudança em `infra/localstack/ready.d/10-s3.sh`, reaplique com `docker exec noisevault-localstack bash /etc/localstack/init/ready.d/10-s3.sh`.

Migration e seed rodam no boot da API em Development. Admin de dev: `caiogualbertodev@outlook.com` / `12341234`. E-mails de confirmação e de redefinição de senha caem no Mailpit.

## Documentação

Comece por `CLAUDE.md` na raiz, depois `api/CLAUDE.md` e `frontend/CLAUDE.md`. As decisões por domínio estão em `*/.claude/rules/`.
