---
paths:
  - "**/Dockerfile"
  - "**/docker-compose*.yml"
  - "infra/**"
  - "**/NoiseVault.Api/Program.cs"
  - "**/appsettings.Production.json"
---

# Decisões — Hospedagem em produção

Decidido em 14/10/2026. Decisão do usuário: "vai ser tudo na AWS". **No ar desde 17/10/2026**: `https://noisevault.net` (Amplify), `https://api.noisevault.net` (Lightsail) e `https://media.noisevault.net` (CloudFront), com o primeiro admin criado pelo cadastro do site e o SQL do `DEPLOY.md`. O que saiu diferente do plano naquele dia está tachado nesta rule: DNS no Cloudflare em vez do Route 53, SES pela SDK em vez de SMTP, MediaConvert implementado antes do deploy, licença do ImageSharp como secret de build, deploy por pipeline em vez de SSH. **O roteiro do deploy pelo console, passo a passo, é o `DEPLOY.md` da raiz** (escrito em 17/10/2026, quando o usuário disse "chegou o dia"); os arquivos do servidor estão em `infra/lightsail/` (compose de produção, `Caddyfile`, `.env.example`, `backup.sh`), e os do front em `.github/workflows/deploy-front.yml` e `infra/cloudfront/` (desde 20/10/2026; antes, `amplify.yml` e `customHttp.yml`). Esta rule guarda o porquê; o `DEPLOY.md` guarda o como.

## A arquitetura: um servidor e serviços gerenciados por uso

| Peça | Onde | Observação |
|---|---|---|
| API + Hangfire + SignalR + Postgres | **Lightsail em São Paulo**, bundle de 4 GB (2 vCPU, 80 GB SSD, 4 TB de transferência), 23,54 USD/mês | `docker compose` com a API, o Postgres e um Caddy na frente (HTTPS e WebSocket) |
| Fotos e vídeos | **S3** em São Paulo, bucket privado + **CloudFront** com OAC | como já previsto em `armazenamento.md`. Plano flat-rate Free do CloudFront no começo, Pro (15 USD) quando chegar perto do limite |
| Conversão de vídeo | **MediaConvert** em São Paulo, por uso (`Video__Engine=MediaConvert`) | implementado em 17/10/2026 (`midia-e-video.md`): a API cria o job e um recorrente consulta o fim. O `Dockerfile` ainda leva o ffmpeg como saída de emergência (`Video__Engine=Ffmpeg`) |
| E-mail | **SES** pela SDK (`Email__Provider=Ses`) | `SesEmailSender`, com a mesma chave IAM da API (`email.md`) |
| Front | ~~**Amplify**~~ **S3 + CloudFront próprio** (20/10/2026) | ver "Front e a prévia de links" abaixo |

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
| `noisevault.net` | o front (CloudFront `noisevault-web`; antes, o Amplify) | o site; `Frontend:BaseUrl`, o CORS da API e o CORS do bucket usam este |
| `www.noisevault.net` | redireciona para `noisevault.net` | um endereço só, para cache, cookies e prévia não se dividirem |
| `api.noisevault.net` | o Lightsail (Caddy com HTTPS) | API, SignalR (`/hubs`) e a rota de prévia `/s/...` (que o CloudFront do front repassa) |
| `media.noisevault.net` | o CloudFront do bucket | `Storage:PublicBaseUrl` |
| e-mail `nao-responda@noisevault.net` | SES | o domínio verificado no SES, com SPF, DKIM e DMARC no DNS, para confirmação e redefinição de senha não caírem no spam |
| `ensaio.noisevault.net` | a mesma distribuição do front, que mostra a moldura do módulo Ensaio pelo endereço | o módulo existe (`ensaio.md`, `frontend/.claude/rules/ensaio.md`); no deploy, o domínio entra como nome alternativo da mesma distribuição, `https://ensaio.noisevault.net` entra em `Cors:AllowedOrigins` e o build do front leva `VITE_ENSAIO_URL` e `VITE_MAIN_URL` |
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
- **Deploy da API por pipeline, não por SSH** (pedido do usuário em 17/10/2026, ao ver os passos manuais): `.github/workflows/deploy-api.yml` builda a imagem no GitHub Actions (com `SIXLABORS_LICENSE_KEY` em secret), publica em `ghcr.io/caio26gualberto/noisevault-api` (`latest` e o sha do commit) e entra no Lightsail por SSH (`LIGHTSAIL_HOST`, `LIGHTSAIL_SSH_KEY`) para `git reset --hard origin/main`, `docker compose pull` e `up -d`. O servidor de 4 GB não compila nada; o `build:` fica no compose só como saída de emergência. A preparação do servidor é um script só (`infra/lightsail/bootstrap.sh`), rodado uma vez pelo SSH do navegador; o `.env` continua no servidor, preenchido à mão, para o segredo ficar num lugar só. O servidor lê o GHCR com um token clássico `read:packages` (`docker login ghcr.io`), porque o pacote é privado como o código.
- **Migrations em produção rodam no boot**: o compose de `infra/lightsail/` liga `Database__MigrateOnStartup=true`. É uma instância só, o servidor não tem o SDK do .NET para `dotnet ef`, e o `MigrateAsync` é idempotente. A frase "só em desenvolvimento" do `CLAUDE.md` vale para ambientes com mais de uma instância subindo ao mesmo tempo, que é quando duas migrations concorrem. Volta a ser proibido no Fargate.
- **Caddy** na frente da API (`infra/lightsail/Caddyfile`): HTTPS automático, corpo de requisição até 60 MB (o maior upload que passa pela API é a foto do mural, 41 MB; vídeo vai direto ao S3) e `read_timeout` de 10 min para o upload lento. O `ForwardedHeaders` da API já confia em qualquer proxy (`KnownProxies` limpo), então o `X-Forwarded-Proto` do Caddy vale e o cookie sai `Secure`.
- **Primeiro admin em produção**: o seed não cria conta de admin fora de Development. A pessoa se cadastra pelo site e recebe o papel por SQL no servidor (`DEPLOY.md`, passo 9).
- **Backup**: `infra/lightsail/backup.sh` faz `pg_dump -Fc` e manda para `s3://noisevault-media/backups/postgres/`, por cron às 4h30; a regra de lifecycle do bucket apaga com 30 dias. Mais o snapshot automático do Lightsail.
- **Hangfire**: o dashboard já não é mapeado em `Production` (`jobs-hangfire.md`).
- **Prévia de link**: ~~rewrite `/s/<*>` no Amplify, antes do rewrite da SPA~~ a função do CloudFront do front manda `/s/...` para a origem da API ("Front e a prévia de links" abaixo, `compartilhamento.md`).
- **PWA**: o `sw.js` vai com `Cache-Control: no-cache` (gravado no objeto do S3 pelo `deploy-front.yml`), e o `manifest.webmanifest` com `application/manifest+json` (`frontend/.claude/rules/pwa.md`). Com cache longo no `sw.js`, a versão nova do site demora a chegar em quem instalou.
- **Licença do ImageSharp**: o build em `Release` **falha** sem chave da Six Labors ("No Six Labors license found"; o `Debug` só avisa). Visto em 14/10/2026. A licença Split é gratuita abaixo de US$ 1 milhão de receita (`armazenamento.md`). ~~O `Dockerfile` recebe a chave pelo build arg `SIXLABORS_LICENSE_KEY` e passa como `/p:SixLaborsLicenseKey`~~: passar por `/p:` falhou no primeiro build do pipeline (17/10/2026, `LicenseSerializer.Deserialize` quebrou com a chave escapada pelo MSBuild). A chave entra como **secret de build do BuildKit** montado em `src/NoiseVault.Infra.Data/sixlabors.lic` só durante o `publish` (o target do pacote procura `**/sixlabors.lic`), sem virar camada da imagem nem `ARG`. No GitHub Actions, `secrets: sixlabors=…` do `build-push-action` no deploy, e no CI um passo grava o mesmo secret em `src/NoiseVault.Infra.Data/sixlabors.lic` antes do `dotnet build` (o arquivo está no `.gitignore`); no compose do servidor, `secrets.sixlabors` lido da variável `SIXLABORS_LICENSE_KEY` do `.env`. Nunca no Git.
- **SignalR**: uma instância, sem backplane. O Caddy repassa WebSocket; o keep-alive de 15 s sobra para qualquer idle timeout (`notificacoes.md`).
- **CORS**: `Cors:AllowedOrigins` e o CORS do bucket com `https://noisevault.net` (`midia-e-video.md`). O CORS da API aceita credenciais (o cookie do refresh), então a lista é exata e ganha cada subdomínio do front quando ele nascer (`https://ensaio.noisevault.net`, `https://mercado.noisevault.net`); a mesma lista é a do `OriginGuard`.
- **Cookie do refresh**: `Auth__RefreshCookie__Domain=.noisevault.net`, para a sessão valer em todos os subdomínios. Sem a variável, o cookie fica preso a `api.noisevault.net`, o que também funciona enquanto só existir o site principal (`auth-e-contas.md`).
- **E-mail**: com provedor real, o envio do esqueci-senha vai para a fila `emails` (o risco aceito de tempo de resposta em `auth-e-contas.md`).

## Front e a prévia de links

~~O usuário escolheu o **Amplify** para o front (estava na estimativa, 1,77 USD/mês).~~ O Amplify hospedou a SPA do deploy (17/10/2026) até 20/10/2026, mas fechava um caminho da prévia Open Graph:

- o CloudFront do Amplify é gerenciado por ele, e não aceita uma função nossa;
- os rewrites do Amplify filtram por caminho e país, não por `User-Agent`.

Com ele, a prévia saía só pela **rota de compartilhamento** (`/s/<*>` repassado à API, `compartilhamento.md`). Funcionava no link do botão "Compartilhar", mas não no link copiado da barra do navegador, que é o que quase todo mundo cola no grupo.

**Decisão do usuário em 20/10/2026: o front vai para S3 + CloudFront**, para a prévia valer também no link da barra (era o item 1.b do `IDEIAS.md`). A peça é a função `infra/cloudfront/web-viewer-request.js` (CloudFront Functions, JavaScript runtime 2.0, *viewer request*):

| Pedido | O que a função faz |
|---|---|
| `www.noisevault.net/...` | 301 para `https://noisevault.net/...`, com a query (o Amplify fazia esse redirect) |
| `/s/...` | escolhe a origem `noisevault-api` (`cf.selectRequestOriginById`): a rota de compartilhamento de sempre |
| robô de prévia em `/{feed,mural,eventos,bandas,perfil}/{chave}`, fora do Ensaio | troca o caminho para `/s/...` e escolhe a origem da API: o robô recebe as tags Open Graph da mesma página |
| caminho com extensão de arquivo estático | segue para o bucket como está |
| qualquer outro caminho (rota da SPA) | troca para `/index.html` |

- **Robôs de prévia**, reconhecidos pelo `User-Agent`: `facebookexternalhit`, `Facebot`, `WhatsApp`, `Twitterbot`, `TelegramBot`, `Discordbot`, `Slackbot`, `LinkedInBot` e `SkypeUriPreview`. **O Googlebot fica de fora de propósito**: entregar ao buscador um HTML diferente do da pessoa é assunto do SEO (`IDEIAS.md`, 16), que pede HTML com o conteúdo, não só as tags.
- **Fora do `ensaio.noisevault.net`**: o Ensaio tem outras rotas, e `ensaio.noisevault.net/bandas/...` não existe. A checagem é "qualquer host menos o Ensaio", e não "só o `noisevault.net`", para o desvio poder ser testado pelo endereço `dxxxx.cloudfront.net` antes da virada.
- Robô que pede `/mural/novo` ou `/bandas/nova` recebe o 404 com a prévia genérica da API. É inofensivo, e a regex fica simples.
- **A SPA sai sem custom error response**: com OAC, o S3 responde 403 para chave que não existe, e transformar 403/404 em `index.html` na distribuição também engoliria o 404 da prévia genérica da API. A função decide antes, pelo caminho. A lista de extensões estáticas vem do rewrite antigo do Amplify, para `/perfil/joao.silva` (apelido com ponto) continuar caindo na SPA.
- **Cache**: a função roda antes da busca no cache, então a chave do cache é o caminho já trocado. O `index.html` é um objeto só para todas as rotas, e o `/s/mural/12` do robô é outro, com os 5 minutos do `Cache-Control` da API. O `User-Agent` não entra na chave nem vai à origem.

### Por que uma distribuição própria, e não a da mídia

A versão anterior desta rule dizia "o mesmo CloudFront da mídia". Ficou uma distribuição nova, `noisevault-web`, também no plano flat-rate **Free**:

- a cota do Free é **por distribuição** (1 milhão de requisições e 100 GB por mês). Juntos, o site e os vídeos dividiriam uma cota só; separados, cada um tem a sua. A conta aceita até 3 planos Free;
- os cabeçalhos são outros: a mídia usa a política SimpleCORS, e o site não precisa de CORS;
- a distribuição da mídia continua com um host só (`media.noisevault.net`), sem nenhuma função no caminho do vídeo.

### O bucket e o deploy

- Bucket **`noisevault-web`** em São Paulo, privado, lido só pela distribuição por OAC. A mídia continua no `noisevault-media`, sem mistura.
- **Deploy por pipeline**, `.github/workflows/deploy-front.yml`, a cada push na `main` que toque `frontend/` ou `infra/cloudfront/`:
  - builda com as `VITE_*` das **variables** do repositório (são valores públicos, não secrets);
  - entra na AWS por **OIDC** (`aws-actions/configure-aws-credentials`, role `noisevault-web-deploy`), sem chave de acesso guardada no GitHub. A role só pode gravar no `noisevault-web`, atualizar e publicar a função e invalidar a distribuição;
  - sobe `assets/` com `public, max-age=31536000, immutable` e **sem `--delete`**, porque quem está com o site aberto ainda pede os chunks da versão anterior;
  - o resto sobe com `--delete` e um dia de cache. `index.html`, `sw.js`, `offline.html` e os manifestos vão com `no-cache` (o manifesto com `application/manifest+json`). O `index.html` sobe por último, quando os arquivos novos já existem;
  - publica o código da função a partir do repositório, que é a fonte (editar a função no console é sobrescrito no próximo deploy), e invalida `/*`.
- O job só roda com a variable `WEB_BUCKET` preenchida: antes da virada, o push não falha.
- O custo continua perto de zero: o plano Free cobre a distribuição, e o bucket tem poucos MB.

### A virada

Um domínio só pode estar numa distribuição por vez, e a do Amplify é de uma conta da AWS, não da nossa. Por isso não dá para mover o domínio sem tirá-lo do Amplify antes. O site fica fora do ar por alguns minutos, entre remover o domínio do Amplify e o CloudFront aceitar o mesmo nome. O roteiro está no `DEPLOY.md`, passo 14. **Virada feita em 20/10/2026**, com o login, o Ensaio, o redirect do `www` e a prévia do link da barra no WhatsApp conferidos. O app do Amplify, o `amplify.yml` e o `customHttp.yml` saíram no mesmo dia.

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
