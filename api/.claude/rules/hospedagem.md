---
paths:
  - "**/Dockerfile"
  - "**/docker-compose*.yml"
  - "infra/**"
  - "**/NoiseVault.Api/Program.cs"
  - "**/appsettings.Production.json"
---

# Decisões — Hospedagem em produção

Decidido em 14/10/2026. Decisão do usuário: "vai ser tudo na AWS". **O roteiro do deploy pelo console, passo a passo, é o `DEPLOY.md` da raiz** (escrito em 17/10/2026, quando o usuário disse "chegou o dia"); os arquivos do servidor estão em `infra/lightsail/` (compose de produção, `Caddyfile`, `.env.example`, `backup.sh`), e os do front em `amplify.yml` (raiz, monorepo) e `frontend/customHttp.yml`. Esta rule guarda o porquê; o `DEPLOY.md` guarda o como.

## A arquitetura: um servidor e serviços gerenciados por uso

| Peça | Onde | Observação |
|---|---|---|
| API + Hangfire + SignalR + Postgres | **Lightsail em São Paulo**, bundle de 4 GB (2 vCPU, 80 GB SSD, 4 TB de transferência), 23,54 USD/mês | `docker compose` com a API, o Postgres e um Caddy na frente (HTTPS e WebSocket) |
| Fotos e vídeos | **S3** em São Paulo, bucket privado + **CloudFront** com OAC | como já previsto em `armazenamento.md`. Plano flat-rate Free do CloudFront no começo, Pro (15 USD) quando chegar perto do limite |
| Conversão de vídeo | **MediaConvert** em São Paulo, por uso (`Video__Engine=MediaConvert`) | implementado em 17/10/2026 (`midia-e-video.md`): a API cria o job e um recorrente consulta o fim. O `Dockerfile` ainda leva o ffmpeg como saída de emergência (`Video__Engine=Ffmpeg`) |
| E-mail | **SES** pela SDK (`Email__Provider=Ses`) | `SesEmailSender`, com a mesma chave IAM da API (`email.md`) |
| Front | **Amplify** | ver "Front e a prévia de links" abaixo |

### Por que não Fargate + Load Balancer + RDS

Foi a primeira estimativa (PDFs na área de trabalho, 13/10/2026). Para **uma** API e **um** banco, as três peças somavam 83 USD/mês em São Paulo, e o Load Balancer (25 USD) só existia porque o Fargate exige um. O Lightsail troca as três por um servidor de preço fixo com transferência inclusa. O preço do Lightsail é o mesmo em São Paulo e em Ohio.

### Por que o MediaConvert desde o começo

Com o ffmpeg no mesmo servidor, uma conversão longa (fita de 2 h do mural) disputa CPU com a API e esgota os créditos de CPU do bundle (as vCPUs do Lightsail são "burstable"). O MediaConvert lê e grava direto no S3, sem passar pelo servidor, e cobra por minuto convertido: mês sem vídeo custa zero. Um segundo servidor só para converter custaria fixo, usado ou não.

### Por que São Paulo e não Ohio

| Cenário | Ohio | São Paulo |
|---|---|---|
| Volume do PDF (370 GB, 54 h de vídeo convertido por mês, CloudFront Pro) | 84,43 USD | 100,00 USD |
| Lançamento (dezenas de GB, poucos vídeos, CloudFront Free) | ~25 a 30 USD | ~25 a 30 USD |

A diferença vem só do MediaConvert e do S3, que crescem com o uso. Em troca, cada requisição chega uns 110 a 130 ms mais rápido para quem está no ABC, e os dados ficam no Brasil. Com o banco no RDS em vez de dentro do Lightsail, São Paulo vai a 129,20 USD no cenário do PDF. **Fica fora** até a operação do banco pesar.

## Domínio: `noisevault.net`

Comprado pelo usuário em 14/10/2026. O `.com` já tinha dono, o `.com.br` o usuário não quis, e o `.net` ganhou do `.social` pela confiança (e-mail chegando fora do spam), pela facilidade de falar e digitar e pela renovação mais barata.

| Endereço | Aponta para | Observação |
|---|---|---|
| `noisevault.net` | o front (Amplify) | o site; `Frontend:BaseUrl`, o CORS da API e o CORS do bucket usam este |
| `www.noisevault.net` | redireciona para `noisevault.net` | um endereço só, para cache, cookies e prévia não se dividirem |
| `api.noisevault.net` | o Lightsail (Caddy com HTTPS) | API, SignalR (`/hubs`) e a rota de prévia `/s/...` (que o Amplify repassa) |
| `media.noisevault.net` | o CloudFront do bucket | `Storage:PublicBaseUrl` |
| e-mail `nao-responda@noisevault.net` | SES | o domínio verificado no SES, com SPF, DKIM e DMARC no DNS, para confirmação e redefinição de senha não caírem no spam |
| `ensaio.noisevault.net` | o mesmo app do Amplify, que mostra a moldura do módulo Ensaio pelo endereço | o módulo existe (`ensaio.md`, `frontend/.claude/rules/ensaio.md`); no deploy, o domínio entra no Amplify como domínio do mesmo app, `https://ensaio.noisevault.net` entra em `Cors:AllowedOrigins` e o build do front leva `VITE_ENSAIO_URL` e `VITE_MAIN_URL` |
| `mercado.noisevault.net` | idem, para venda e troca | planejado (`IDEIAS.md`, 8b) |

- ~~Quem guarda o DNS ainda não foi decidido.~~ **O DNS fica no Cloudflare**, onde o domínio foi registrado (visto em 17/10/2026, no dia do deploy): domínio do Cloudflare Registrar não pode apontar para outros servidores de nome, então o Route 53 saiu. Todo registro entra à mão no painel do Cloudflare, sempre com **proxy desligado** ("DNS only"): o proxy laranja na frente da API quebraria o certificado do Caddy e o WebSocket do SignalR, e na frente do Amplify e do CloudFront só duplicaria CDN. O `DEPLOY.md` tem cada registro.
- O login social (`IDEIAS.md`, item 6) volta a ser possível com o site em `https://noisevault.net`: as origens do Google e os domínios do SDK do Facebook são esses.

## Pendências antes do primeiro deploy

- **Credenciais**: o Lightsail não tem IAM role como o EC2. Um usuário IAM com chave em variável de ambiente (`Storage__AccessKey`, `Storage__SecretKey`; o `StorageSettings` já aceita), com o mínimo:
  - S3 no bucket da mídia e no prefixo de backup;
  - `mediaconvert:CreateJob`, `mediaconvert:GetJob`, `mediaconvert:DescribeEndpoints` e `iam:PassRole` só na role de serviço do MediaConvert (`noisevault-mediaconvert`, que lê `uploads/*` e grava `users/*`); a role vai em `MediaConvert__RoleArn`;
  - ~~as credenciais SMTP do SES são de outro usuário IAM~~ o e-mail sai pela SDK com a mesma chave, então a política do `noisevault-api` leva `ses:SendEmail` (17/10/2026).
- **Spotify**: `Spotify__ClientId` e `Spotify__ClientSecret` do app no painel de desenvolvedor (modo desenvolvimento basta; `discografia.md`), por variável de ambiente. Renomear o app "A New Level Music" para Noise Vault, site `https://noisevault.net`, e apagar o redirect de `localhost:7213`.
- **Postgres no servidor**: `pg_dump` diário para o S3 (prefixo próprio, retenção definida no deploy) e snapshot automático do Lightsail. Restaurar um backup faz parte do roteiro de verificação do deploy, não fica para quando precisar.
- ~~**Data Protection**: persistir as chaves antes de subir~~ feito em 17/10/2026: `PersistKeysToDbContext<AppDbContext>` com a tabela `DataProtectionKeys` (migration `AddDataProtectionKeys`) e `SetApplicationName("noisevault")`. As chaves vão no `pg_dump`.
- **Migrations em produção rodam no boot**: o compose de `infra/lightsail/` liga `Database__MigrateOnStartup=true`. É uma instância só, o servidor não tem o SDK do .NET para `dotnet ef`, e o `MigrateAsync` é idempotente. A frase "só em desenvolvimento" do `CLAUDE.md` vale para ambientes com mais de uma instância subindo ao mesmo tempo, que é quando duas migrations concorrem. Volta a ser proibido no Fargate.
- **Caddy** na frente da API (`infra/lightsail/Caddyfile`): HTTPS automático, corpo de requisição até 60 MB (o maior upload que passa pela API é a foto do mural, 41 MB; vídeo vai direto ao S3) e `read_timeout` de 10 min para o upload lento. O `ForwardedHeaders` da API já confia em qualquer proxy (`KnownProxies` limpo), então o `X-Forwarded-Proto` do Caddy vale e o cookie sai `Secure`.
- **Primeiro admin em produção**: o seed não cria conta de admin fora de Development. A pessoa se cadastra pelo site e recebe o papel por SQL no servidor (`DEPLOY.md`, passo 9).
- **Backup**: `infra/lightsail/backup.sh` faz `pg_dump -Fc` e manda para `s3://noisevault-media/backups/postgres/`, por cron às 4h30; a regra de lifecycle do bucket apaga com 30 dias. Mais o snapshot automático do Lightsail.
- **Hangfire**: o dashboard já não é mapeado em `Production` (`jobs-hangfire.md`).
- **Prévia de link**: rewrite `/s/<*>` → `https://api.noisevault.net/s/<*>` (200, proxy) no Amplify, **antes** do rewrite da SPA (`compartilhamento.md`). Sem ele, o link do botão "Compartilhar" cai na SPA e a prévia some.
- **PWA**: o `sw.js` vai com `Cache-Control: no-cache` no Amplify ou no CloudFront, e o `manifest.webmanifest` com `application/manifest+json` (`frontend/.claude/rules/pwa.md`). Com cache longo no `sw.js`, a versão nova do site demora a chegar em quem instalou.
- **Licença do ImageSharp**: o build em `Release` **falha** sem chave da Six Labors ("No Six Labors license found"; o `Debug` só avisa). Visto em 14/10/2026. A licença Split é gratuita abaixo de US$ 1 milhão de receita (`armazenamento.md`). O `Dockerfile` recebe a chave pelo build arg `SIXLABORS_LICENSE_KEY` (o compose lê do `.env`) e passa como `/p:SixLaborsLicenseKey` ao `publish`; nunca no Git.
- **SignalR**: uma instância, sem backplane. O Caddy repassa WebSocket; o keep-alive de 15 s sobra para qualquer idle timeout (`notificacoes.md`).
- **CORS**: `Cors:AllowedOrigins` e o CORS do bucket com `https://noisevault.net` (`midia-e-video.md`). O CORS da API aceita credenciais (o cookie do refresh), então a lista é exata e ganha cada subdomínio do front quando ele nascer (`https://ensaio.noisevault.net`, `https://mercado.noisevault.net`); a mesma lista é a do `OriginGuard`.
- **Cookie do refresh**: `Auth__RefreshCookie__Domain=.noisevault.net`, para a sessão valer em todos os subdomínios. Sem a variável, o cookie fica preso a `api.noisevault.net`, o que também funciona enquanto só existir o site principal (`auth-e-contas.md`).
- **E-mail**: com provedor real, o envio do esqueci-senha vai para a fila `emails` (o risco aceito de tempo de resposta em `auth-e-contas.md`).

## Front e a prévia de links

O usuário escolheu o **Amplify** para o front (estava na estimativa, 1,77 USD/mês). Ele hospeda a SPA sem esforço, mas fecha um caminho da prévia Open Graph (`IDEIAS.md`, ideia 1):

- o CloudFront do Amplify é gerenciado por ele, e não aceita uma função nossa;
- os rewrites do Amplify filtram por caminho e país, não por `User-Agent`.

Com o Amplify, a prévia sai por **rota de compartilhamento**: um rewrite `/s/<*>` do Amplify repassa para a API, que devolve o HTML com as tags `og:*` e redireciona a pessoa para a página. Funciona no link do botão "Compartilhar", não no link copiado da barra do navegador. **Feita em 14/10/2026** (`compartilhamento.md`); a prévia no link copiado da barra virou o item 1.b do `IDEIAS.md`.

A alternativa que cobre os dois links é o front em **S3 + o mesmo CloudFront da mídia** (sem custo extra no plano flat-rate), com uma CloudFront Function de viewer request que manda os robôs (`facebookexternalhit`, `WhatsApp`, `Twitterbot`, `TelegramBot`, `Discordbot`) para a API com `cf.updateRequestOrigin`. O deploy vira `aws s3 sync` + invalidação no CI. **A escolha fica para quando a ideia 1 virar módulo.**

## Quando o banco sair do Lightsail para o RDS

Decisão do usuário em 16/10/2026: a migração pode parar a produção (madrugada, menor fluxo), e a API deve ir para o Fargate num segundo momento, quando o RDS já existir. O caminho é `pg_dump` + `pg_restore`, nunca exportar tabela em JSON: o dump leva tabelas, índices, chaves estrangeiras, sequências e a `__EFMigrationsHistory` de uma vez. Mídia não se move: já está no S3.

1. Criar o RDS Postgres 17 na mesma região, acessível só pelo IP do Lightsail. Ensaiar o roteiro inteiro num RDS de teste uma semana antes.
2. Parar a API no servidor (`docker compose stop api`), para ninguém escrever durante a cópia.
3. No Lightsail:

```
pg_dump -Fc -h localhost -U postgres noisevault > noisevault.dump
pg_restore -h <endpoint-do-rds> -U <usuario> -d noisevault --no-owner --no-privileges noisevault.dump
```

4. Trocar a connection string da API pela do RDS (variável de ambiente) e subir de novo. As migrations já constam no banco; a API só segue.
5. Conferir login, feed, upload e um link de e-mail pendente (as chaves do Data Protection vão no dump se `PersistKeysToDbContext` estiver ligado, `auth-e-contas.md`). Só então apagar o container do Postgres e o volume.

O tempo parado é o do dump mais o do restore: minutos para alguns GB. Migração sem parar (AWS DMS replicando ao vivo) só se esses minutos passarem a incomodar.

Depois do RDS, a API vai para o **Fargate** com Load Balancer (a primeira estimativa, descartada em 14/10/2026 pelo custo fixo): nesse passo entram o backplane do SignalR (`notificacoes.md`) e o Hangfire numa instância só, e o Lightsail é desligado.

## Quando revisar

- A CPU do bundle vive no teto fora das conversões: subir de bundle, ou ir para os bundles "compute-optimized".
- A API precisa de 2 instâncias: entra o backplane do SignalR (`notificacoes.md`), o Postgres sai do servidor (RDS) e volta um balanceador.
- O CloudFront chega perto do limite do plano Free: plano Pro.
