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

⚠️ O LocalStack Community guarda os arquivos em memória: parar o container apaga avatares, capas e mídias, mas o banco continua apontando para eles. O que você subiu pela tela é descartável; o que veio do seed de desenvolvimento volta sozinho no próximo boot da API (`api/.claude/rules/seed-de-desenvolvimento.md`).

O LocalStack está fixado na 4.14.0, que roda sem conta. Ao atualizar a imagem para uma versão mais nova, crie uma conta gratuita em https://app.localstack.cloud, copie o auth token e coloque em `LOCALSTACK_AUTH_TOKEN` num `.env` na raiz (veja `.env.example`; o `.env` não vai para o Git).

Se o LocalStack já estava rodando antes de uma mudança em `infra/localstack/ready.d/10-s3.sh`, reaplique com `docker exec noisevault-localstack bash /etc/localstack/init/ready.d/10-s3.sh`.

Migration e seed rodam no boot da API em Development. Admin de dev: `caiogualbertodev@outlook.com` / `12341234`. E-mails de confirmação e de redefinição de senha caem no Mailpit.

Para importar discografia do Spotify, crie um app em https://developer.spotify.com/dashboard (o modo desenvolvimento basta) e passe `Spotify__ClientId` e `Spotify__ClientSecret` como variáveis de ambiente da API; sem elas o botão "Importar do Spotify" responde que não está configurado e o resto da discografia funciona (`api/.claude/rules/discografia.md`).

Em Development a API também cria um conjunto de exemplo (`Seed:DevData` no `appsettings.Development.json`): oito pessoas com e-mail `{nickname}@noisevault.dev` e senha `12341234` (`lucia.ferraz`, `rafa.tavares`, `marina.k`, `denis.moura`, `paty.lins`, `jorge.okamoto`, `bia.santana`, `tiago.nunes`), quatro bandas (o admin é Owner da Ferrugem), feed com fotos e vídeo, eventos, mural, fichas e vagas do Ensaio, tickets de suporte e notificações. As fotos e os vídeos vêm de uma pasta no seu PC (`%LOCALAPPDATA%\NoiseVault\seed-media`), preenchida uma vez por `api\scripts\baixar-midia-seed.ps1` (fotos de Metallica, Slayer, Kreator e cia., retratos de ícones do metal e vídeos de show, tudo do Wikimedia Commons com licença CC, uns 180 MB; o script corta os vídeos com o ffmpeg e grava os créditos), e são reenviados ao LocalStack sempre que faltarem. Sem a pasta, o seed usa imagens geradas e avisa no log. Para subir sem o conjunto, `Seed__DevData=false`; para voltar ao original, `docker compose down -v`.

## Documentação

Comece por `CLAUDE.md` na raiz, depois `api/CLAUDE.md` e `frontend/CLAUDE.md`. As decisões por domínio estão em `*/.claude/rules/`.

## Produção

No ar em `https://noisevault.net` desde 17/10/2026. O deploy na AWS, pelo console e passo a passo, está em [`DEPLOY.md`](DEPLOY.md). Os arquivos do servidor ficam em `infra/lightsail/`; a API sobe pelo workflow "Deploy API" a cada push na `main`, e o front pelo Amplify.
