# Deploy do Noise Vault na AWS (São Paulo), pelo console

**Feito em 17/10/2026**: o site está no ar nos três endereços, com o pipeline da API funcionando. Este roteiro é o que foi seguido, corrigido no caminho onde o console divergiu; serve para recriar o ambiente do zero ou para conferir uma peça.

Roteiro do primeiro deploy, feito pelo painel da AWS, na região **América do Sul (São Paulo), `sa-east-1`**. As decisões por trás de cada peça estão em `api/.claude/rules/hospedagem.md`; aqui é só o passo a passo. Siga na ordem: cada bloco usa algo criado no anterior.

O que vai existir no fim:

| Peça | Serviço | Endereço |
|---|---|---|
| API, Hangfire, SignalR, Postgres, Caddy | Lightsail (1 instância, Docker) | `api.noisevault.net` |
| Fotos e vídeos | S3 privado + CloudFront | `media.noisevault.net` |
| Conversão de vídeo | MediaConvert (por uso) | sem endereço; a API cria o job |
| E-mail de confirmação e redefinição | SES pela SDK, com a chave da API | `nao-responda@noisevault.net` |
| Site e o Ensaio | ~~Amplify (um app, dois domínios)~~ S3 + CloudFront próprio desde 20/10/2026 (passo 14) | `noisevault.net`, `ensaio.noisevault.net` |
| DNS | Cloudflare (onde o domínio está registrado) | zona `noisevault.net` |

Arquivos do repositório usados aqui: `infra/lightsail/` (compose, Caddyfile, `.env.example`, `backup.sh`), `api/Dockerfile`, `amplify.yml` e `customHttp.yml`, os dois na raiz (o Amplify só lê os dois da raiz, mesmo em monorepo). Em monorepo o `customHttp.yml` também precisa do formato `applications: - appRoot: frontend` com os `customHeaders` dentro; sem isso o build passa e a etapa de deploy falha com `Monorepo spec provided without "applications" key` (os deploys 2 a 8 de 17/10/2026 falharam assim).

## 0. Antes de abrir o console

Tenha em mãos:

- a conta AWS com MFA ligado no usuário root, e um usuário IAM administrador para o dia a dia (nunca opere com o root);
- o painel do **Cloudflare**, onde o `noisevault.net` está registrado: é lá que todo registro de DNS entra. Domínio do Cloudflare Registrar não pode trocar de servidores de nome, então o Route 53 fica de fora;
- a **chave de licença da Six Labors** (ImageSharp). A licença Split é gratuita abaixo de US$ 1 milhão de receita, pedida em https://sixlabors.com/pricing/. Sem ela o build em Release falha;
- o **Client ID e Client Secret do Spotify** (painel de desenvolvedor). Renomeie o app para "Noise Vault", site `https://noisevault.net`, e apague o redirect de `localhost:7213`;
- o repositório no GitHub com a `main` atualizada (o Amplify e o pipeline da API leem de lá);
- um segredo para o JWT. Gere no seu terminal e guarde: `openssl rand -base64 48`.

No canto superior direito do console, escolha a região **South America (São Paulo)**. Tudo abaixo é nessa região, com uma exceção marcada no CloudFront.

## 1. DNS: a zona no Cloudflare

Nada a criar: a zona `noisevault.net` já existe no Cloudflare, em **DNS → Records**. Os registros entram à mão nos passos de cada serviço, sempre por **Add record**. Duas regras valem para todos:

- **Proxy status: DNS only** (a nuvem cinza, não a laranja). Com o proxy ligado, o Cloudflare se põe na frente do tráfego e quebra a validação de certificado da AWS, o HTTPS do Caddy e o WebSocket do sino. O CDN da mídia já é o CloudFront.
- **TTL: Auto.**

O Cloudflare aceita CNAME na raiz (`noisevault.net`) por "CNAME flattening", que é o que o Amplify precisa no passo 8.

## 2. S3: o bucket da mídia

1. **S3 → Create bucket.** Nome `noisevault-media`, região São Paulo. Mantenha **Block all public access ligado** (o CloudFront é quem serve). Crie.
2. Abra o bucket → **Permissions → Cross-origin resource sharing (CORS) → Edit**, cole:

```json
[
  {
    "AllowedOrigins": ["https://noisevault.net", "https://ensaio.noisevault.net"],
    "AllowedMethods": ["GET", "PUT"],
    "AllowedHeaders": ["*"],
    "ExposeHeaders": ["ETag"],
    "MaxAgeSeconds": 3000
  }
]
```

O `PUT` é o envio de vídeo em partes, que vai do navegador direto para o bucket por URL pré-assinada.

3. **Management → Lifecycle rules → Create lifecycle rule.** Nome `abort-incomplete-multipart`, escopo "Limit the scope" com prefixo `uploads/`, ação **Delete expired object delete markers or incomplete multipart uploads → Delete incomplete multipart uploads** após **1** dia. Crie.
4. Crie uma segunda regra `expire-backups`: prefixo `backups/`, ação **Expire current versions of objects** após **30** dias. É a retenção dos dumps do Postgres.

A política do bucket (quem pode ler) é escrita no passo do CloudFront.

## 3. Certificado coringa (região Virgínia)

Um certificado só, coringa, cobre a mídia (CloudFront) e o site (Amplify): `*.noisevault.net` mais a raiz. O CloudFront e o Amplify só aceitam certificado emitido na região **us-east-1 (N. Virginia)**. Troque a região só para este passo.

1. **Certificate Manager → Request → Request a public certificate.** Em Fully qualified domain name, `noisevault.net`; clique **Add another name to this certificate** e acrescente `*.noisevault.net`. Validação por DNS. Request.
2. Abra o certificado. Em **Domains** os dois nomes mostram o **mesmo** registro de validação (um CNAME vale para a raiz e para o coringa), com **CNAME name** e **CNAME value**. No Cloudflare, **Add record** tipo **CNAME**: em Name cole o CNAME name **sem** o `.noisevault.net.` do fim (o Cloudflare completa), em Target o CNAME value, DNS only. Em alguns minutos o status vira **Issued**.
3. Copie o **ARN** do certificado (começa com `arn:aws:acm:us-east-1:`): o Amplify pede no passo 8. Volte a região para **São Paulo**.

O `api.noisevault.net` não entra aqui: o certificado dele é do Let's Encrypt, emitido pelo Caddy no próprio servidor, porque certificado do Certificate Manager não sai da AWS.

## 4. CloudFront: servir a mídia

O console novo cria a distribuição por um assistente de plano (flat-rate). Feito em 17/10/2026 assim:

1. **CloudFront → Create distribution.**
   - **Choose a plan:** Flat-rate plans → **Free**. Sem excedente; o Pro (15 USD) entra quando o uso passar do que o Free foi pensado.
   - **Get started:** nome `noisevault-media`.
   - **Specify origin:** Amazon S3, origem `noisevault-media.s3.sa-east-1.amazonaws.com`, sem origin path. **Allow private S3 bucket access to CloudFront** marcado (é o Origin Access Control; o assistente grava a bucket policy sozinho). Origin settings e cache settings nos recomendados para S3.
   - **Enable security:** as proteções incluídas ficam; marque **Use monitor mode** até o vídeo estar tocando, depois desmarque para bloquear de verdade. A proteção DDoS de camada 7 é do plano Business; fica desligada.
   - **Review and create.** O assistente não pergunta domínio nem certificado.
2. Na distribuição criada, **General → Alternate domain names → Add domain**: `media.noisevault.net`, com o certificado coringa do passo 3 (não peça outro).
3. **Behaviors → behavior padrão → Edit → Response headers policy:** escolha a gerenciada **SimpleCORS** (`Access-Control-Allow-Origin: *` em `GET`/`HEAD`). É o que o player de vídeo (hls.js) precisa para baixar as playlists. A política personalizada com as duas origens exatas é do plano pago; como a mídia é pública e sem cookie, liberar toda origem não abre nada.
4. **Origins:** a origem deve mostrar "Origin access control". Em **S3 → noisevault-media → Permissions → Bucket policy** deve existir uma regra permitindo `cloudfront.amazonaws.com` com o ARN desta distribuição; se estiver vazia, a aba Origins tem o botão para copiar a policy.
5. Copie o **Distribution domain name** (`dxxxx.cloudfront.net`). No Cloudflare, **Add record** tipo **CNAME**, Name `media`, Target o domínio da distribuição, DNS only.
6. Teste depois do passo 7 (quando houver um arquivo): `https://media.noisevault.net/<chave>` abre a imagem.

## 5. IAM: a role do MediaConvert e o usuário da API

### 5.1 A role que o MediaConvert assume

1. **IAM → Policies → Create policy → JSON**, cole:

```json
{
  "Version": "2012-10-17",
  "Statement": [
    { "Effect": "Allow", "Action": "s3:GetObject", "Resource": "arn:aws:s3:::noisevault-media/uploads/*" },
    { "Effect": "Allow", "Action": "s3:PutObject", "Resource": "arn:aws:s3:::noisevault-media/users/*" }
  ]
}
```

Nome `noisevault-mediaconvert-storage`. Crie.

2. **IAM → Roles → Create role.** Trusted entity type **AWS service**, use case **MediaConvert**. Em permissões, marque `noisevault-mediaconvert-storage`. Nome `noisevault-mediaconvert`. Crie.
3. Abra a role e copie o **ARN** (`arn:aws:iam::<conta>:role/noisevault-mediaconvert`): vira `MediaConvert__RoleArn` no `.env`.

### 5.2 O usuário que a API usa

O Lightsail não tem IAM role; a API usa uma chave de acesso.

1. **IAM → Policies → Create policy → JSON**, cole (troque `<conta>` pelo número da sua conta, que aparece no ARN da role acima):

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Sid": "Objects",
      "Effect": "Allow",
      "Action": [
        "s3:PutObject",
        "s3:GetObject",
        "s3:DeleteObject",
        "s3:AbortMultipartUpload",
        "s3:ListMultipartUploadParts"
      ],
      "Resource": "arn:aws:s3:::noisevault-media/*"
    },
    {
      "Sid": "Bucket",
      "Effect": "Allow",
      "Action": ["s3:ListBucket", "s3:ListBucketMultipartUploads"],
      "Resource": "arn:aws:s3:::noisevault-media"
    },
    {
      "Sid": "Email",
      "Effect": "Allow",
      "Action": "ses:SendEmail",
      "Resource": "*"
    },
    {
      "Sid": "MediaConvert",
      "Effect": "Allow",
      "Action": ["mediaconvert:CreateJob", "mediaconvert:GetJob", "mediaconvert:DescribeEndpoints"],
      "Resource": "*"
    },
    {
      "Sid": "PassMediaConvertRole",
      "Effect": "Allow",
      "Action": "iam:PassRole",
      "Resource": "arn:aws:iam::<conta>:role/noisevault-mediaconvert"
    }
  ]
}
```

Nome `noisevault-api-access`. Crie.

2. **IAM → Users → Create user.** Nome `noisevault-api`, **sem** acesso ao console. Em permissões, "Attach policies directly" → `noisevault-api-access`. Crie.
3. Abra o usuário → **Security credentials → Create access key → Application running outside AWS**. Guarde o **Access key ID** e a **Secret access key**: viram `Storage__AccessKey` e `Storage__SecretKey` no `.env` do servidor. A secret só aparece uma vez.

A mesma chave serve para o `backup.sh` (ele grava em `backups/` do mesmo bucket).

## 6. SES: o e-mail

1. **Amazon SES → Identities → Create identity → Domain.** `noisevault.net`, com **Easy DKIM** ligado. Crie. A tela da identidade mostra **três CNAMEs de DKIM** (nome `xxxx._domainkey.noisevault.net`, valor `xxxx.dkim.amazonses.com`). No Cloudflare, três **Add record** tipo **CNAME**, DNS only, com o nome sem o `.noisevault.net` do fim. Em minutos a identidade fica **Verified**.
2. Na identidade → **Custom MAIL FROM domain → Edit**: `mail.noisevault.net`. O SES mostra dois registros para criar no Cloudflare: um **MX** com Name `mail`, Mail server `feedback-smtp.sa-east-1.amazonses.com`, Priority 10; e um **TXT** com Name `mail`, Content `v=spf1 include:amazonses.com ~all`. Isso alinha o SPF.
3. No Cloudflare, **Add record** tipo **TXT**, Name `_dmarc`, Content `v=DMARC1; p=quarantine; rua=mailto:<seu e-mail>`. Com SPF e DKIM alinhados, a confirmação de conta não cai no spam.
4. Nada de credencial SMTP: a API manda pela SDK com a chave do `noisevault-api`, cuja política já tem `ses:SendEmail`. No `.env`, `Email__Provider=Ses`.
5. **SES → Account dashboard → Request production access.** A conta nasce em *sandbox* (só manda para endereços verificados). Preencha: caso de uso "transactional", site `https://noisevault.net`, descrição "confirmação de cadastro e redefinição de senha de uma comunidade musical; sem marketing", volume baixo. A aprovação leva até 24 h. Enquanto espera, verifique o seu próprio e-mail em **Identities → Create identity → Email address** para testar.

## 7. Lightsail: o servidor da API

### 7.1 Criar a instância

1. **Lightsail → Instances → Create instance.** Região São Paulo (`sa-east-1`, zona A). Plataforma Linux, blueprint **OS Only → Ubuntu 24.04 LTS**. Plano **4 GB RAM, 2 vCPUs, 80 GB SSD** (23,54 USD). Nome `noisevault-api`. Crie.
2. **Networking → IPv4 networking → Attach static IP**: crie `noisevault-api-ip` e anexe. Anote o IP.
3. Na instância → **Networking → IPv4 Firewall**: deixe `SSH 22`, `HTTP 80` e adicione **HTTPS 443** (TCP). Adicione também **443 UDP** se quiser HTTP/3 pelo Caddy; opcional.
4. No Cloudflare, **Add record** tipo **A**, Name `api`, IPv4 address o IP estático, **DNS only** (nuvem cinza; com o proxy laranja o Caddy não consegue emitir o certificado e o WebSocket cai).
5. **Lightsail → instância → Snapshots → Automatic snapshots: Enable**, horário de madrugada. É o backup do disco inteiro.

### 7.2 Preparar o servidor (uma vez, pelo SSH do navegador)

É a única sessão manual no servidor. Clique **Connect using SSH** na página da instância e cole:

```bash
curl -fsSL https://raw.githubusercontent.com/Caio26Gualberto/NoiseVault/main/infra/lightsail/bootstrap.sh | bash
```

O `bootstrap.sh` atualiza o Ubuntu, instala Docker e AWS CLI, cria 2 GB de swap, clona o repositório em `~/noisevault` e copia o `.env.example` para `.env`. Leva uns 3 minutos. Ao terminar, ele lista o que falta; faça nesta ordem:

1. `nano ~/noisevault/infra/lightsail/.env` e preencha tudo que você guardou: senha do Postgres (invente uma longa), chave da Six Labors, segredo do JWT, chaves do IAM, o ARN da role do MediaConvert, Spotify, e o seu e-mail em `Email__VerificationInbox`. Nenhum valor fica vazio exceto os que o exemplo já deixa em branco de propósito.
2. `docker login ghcr.io -u <seu usuário do GitHub>`, com um **token clássico** do GitHub só com `read:packages` (GitHub → Settings → Developer settings → Personal access tokens → Tokens (classic)). É o que deixa o servidor baixar a imagem que o pipeline publica. O token fica em `~/.docker/config.json`.
3. Saia do SSH e entre de novo, para o grupo `docker` valer.

### 7.3 O pipeline que sobe a API

A API **não é compilada no servidor**: o workflow `.github/workflows/deploy-api.yml` builda a imagem no GitHub Actions a cada push na `main` que toque `api/` ou `infra/lightsail/`, publica em `ghcr.io/caio26gualberto/noisevault-api` e entra no Lightsail por SSH para `docker compose pull` e `up -d`. Antes do primeiro deploy, cadastre os segredos em **GitHub → repositório → Settings → Secrets and variables → Actions**:

| Secret | Valor |
|---|---|
| `SIXLABORS_LICENSE_KEY` | a chave da Six Labors (o build da imagem precisa dela) |
| `LIGHTSAIL_HOST` | o IP estático da instância |
| `LIGHTSAIL_SSH_KEY` | a chave privada SSH da instância, inteira, com as linhas `-----BEGIN ... KEY-----`. No Lightsail: **Account → SSH keys → Download** a chave padrão da região São Paulo (`LightsailDefaultKey-sa-east-1.pem`) e cole o conteúdo do arquivo |

Depois, **Actions → Deploy API → Run workflow** (ou qualquer push na `main`). O job "Build and push image" leva uns 5 minutos na primeira vez; o "Deploy to Lightsail" termina com o `docker compose ps` mostrando os três containers de pé. O compose liga `Database__MigrateOnStartup=true`, então as migrations rodam no primeiro boot (e em todo boot seguinte, sem efeito quando não há nada novo).

O Caddy pede o certificado do `api.noisevault.net` sozinho na primeira requisição. Teste no navegador: `https://api.noisevault.net/health` responde `Healthy`.

Se der errado, no SSH: `cd ~/noisevault/infra/lightsail && docker compose logs --tail 100 api`. `Cors:AllowedOrigins não configurado` ou `JWT:SecretKey não configurado` é variável faltando no `.env`; corrija e `docker compose up -d`. O `build:` continua no compose como saída de emergência: `docker compose build api && docker compose up -d api` compila no próprio servidor se o GitHub estiver fora do ar.

### 7.4 Backup diário do banco

```bash
mkdir -p ~/.aws && cat > ~/.aws/credentials <<'EOF'
[default]
aws_access_key_id = <Access key ID do noisevault-api>
aws_secret_access_key = <Secret access key>
EOF
chmod 600 ~/.aws/credentials
echo -e "[default]\nregion = sa-east-1" > ~/.aws/config
chmod +x ~/noisevault/infra/lightsail/backup.sh
~/noisevault/infra/lightsail/backup.sh
(crontab -l 2>/dev/null; echo "30 4 * * * /home/ubuntu/noisevault/infra/lightsail/backup.sh >> /home/ubuntu/backup.log 2>&1") | crontab -
```

O primeiro `backup.sh` já deve imprimir `backup ok`. Confira em **S3 → noisevault-media → backups/postgres/**.

**Restaurar** (faz parte da verificação, não deixe para precisar):

```bash
aws s3 cp s3://noisevault-media/backups/postgres/<arquivo>.dump /tmp/restore.dump
docker compose exec -T postgres pg_restore -U noisevault -d noisevault --clean --if-exists < /tmp/restore.dump
```

## 8. Amplify: o site

**Substituído pelo passo 14** (front em S3 + CloudFront, decidido em 20/10/2026 para a prévia valer no link copiado da barra). Fica aqui como registro do primeiro deploy e como volta atrás durante a virada.

1. **AWS Amplify → Create new app → GitHub.** Autorize o GitHub e escolha o repositório `NoiseVault`, branch `main`.
2. Marque **My app is a monorepo** e informe a pasta `frontend`. O Amplify lê o `amplify.yml` da raiz (já aponta `appRoot: frontend`).
3. Em **Environment variables**, adicione:

| Variável | Valor |
|---|---|
| `VITE_API_BASE_URL` | `https://api.noisevault.net` |
| `VITE_MAIN_URL` | `https://noisevault.net` |
| `VITE_ENSAIO_URL` | `https://ensaio.noisevault.net` |

4. Nome do app `noisevault`. **Save and deploy.** O primeiro build leva uns 3 minutos e publica numa URL `*.amplifyapp.com`.
5. **Hosting → Rewrites and redirects → Manage redirects.** Apague o que vier por padrão e deixe duas regras, **nesta ordem**, as duas com tipo `200 (Rewrite)`:

Regra 1, a prévia de link. Source e target:

```
/s/<*>
https://api.noisevault.net/s/<*>
```

Regra 2, a SPA. Source (uma linha só) e target:

```
</^[^.]+$|\.(?!(css|gif|ico|jpg|jpeg|js|png|txt|svg|woff|woff2|ttf|map|json|webmanifest|webp)$)([^.]+$)/>
/index.html
```

A primeira é a prévia de link do botão "Compartilhar" (a API devolve o HTML com as tags Open Graph). A segunda é a SPA. Se a primeira ficar depois da segunda, o link `/s/...` cai no `index.html` e a prévia some.

6. **Hosting → Custom headers**: o Amplify lê o `customHttp.yml` da raiz do repositório (`sw.js` e manifestos sem cache, `assets/` com cache de um ano). Confira que aparece na tela; se não, cole o conteúdo do arquivo ali.
7. **Hosting → Custom domains → Add domain.** Digite `noisevault.net` (ele não vai achar zona, porque o DNS é do Cloudflare; siga em frente). Em **Custom SSL certificate**, escolha o certificado coringa do passo 3 (aparece pelo ARN). Configure:
   - `noisevault.net` → branch `main`;
   - `www` → **Redirect to** `https://noisevault.net` (marque a opção de redirect, não de branch);
   - **Add** `ensaio` → branch `main`.

   Ao salvar, o Amplify mostra os registros que você precisa criar (botão **View DNS records**). Com o certificado próprio não há CNAME de verificação; são só os de tráfego, todos **CNAME**, **DNS only**, no Cloudflare:
   - o da raiz: Name `@` (ou `noisevault.net`), Target o domínio `xxxx.cloudfront.net` que o Amplify mostra (o Cloudflare aceita CNAME na raiz);
   - `www` e `ensaio`, cada um com o mesmo Target do Amplify.

   O Amplify valida os registros depois que propagam. Leva de 10 a 30 minutos para "Available". Se ficar parado em "Pending verification" por mais de uma hora, confira se algum registro ficou com o proxy laranja.

## 9. O primeiro administrador

Em produção o seed **não** cria conta de admin. O caminho é:

1. Abra `https://noisevault.net`, **crie a sua conta** pelo site e confirme pelo e-mail (é o teste do SES).
2. No servidor, dê o papel `Admin` a ela:

```bash
cd ~/noisevault/infra/lightsail
docker compose exec postgres psql -U noisevault -d noisevault -c "
INSERT INTO \"UserRoles\" (\"UserId\", \"RoleId\")
SELECT ui.\"Id\", r.\"Id\"
FROM \"UsersIdentity\" ui
JOIN \"Roles\" r ON r.\"Name\" = 'Admin'
WHERE ui.\"NormalizedEmail\" = upper('SEU@EMAIL.AQUI')
ON CONFLICT DO NOTHING;"
```

3. Saia e entre de novo no site. O menu "Administração" aparece. Se não aparecer, `docker compose restart api` (o cache de permissões é em memória).

## 10. Verificação ponta a ponta

Faça na ordem, com uma segunda conta de teste:

1. `https://api.noisevault.net/health` → `Healthy`. `https://noisevault.net` abre o site com cadeado, e `https://www.noisevault.net` redireciona.
2. Criar conta → e-mail chega na caixa de entrada (não no spam) → confirmar → entrar. Esqueci a senha → o link funciona **depois de** `docker compose restart api` (as chaves do Data Protection estão no banco).
3. Editar o perfil: subir avatar e capa → a imagem abre por `https://media.noisevault.net/...`.
4. Publicar um post com foto e outro com vídeo curto → no log aparece `VIDEO_TRANSCODE_SUBMITTED`; em **MediaConvert → Jobs** o job sai de "Progressing" para "Complete" em um ou dois minutos; no minuto seguinte o log mostra `VIDEO_TRANSCODED` e `POST_PUBLISHED`, e o vídeo toca com o menu de qualidade e o poster. Se o job der erro, a mensagem do MediaConvert está em `VIDEO_TRANSCODE_FAILED` no log e na aba do job; para seguir enquanto ajusta, `Video__Engine=Ffmpeg` no `.env` e `docker compose up -d api` converte no container.
5. Com duas abas, uma em cada conta: curtir o post da outra → o sino sobe sem recarregar (SignalR passou pelo Caddy).
6. Criar uma banda, convidar a outra conta → e-mail do convite chega.
7. Botão "Compartilhar" num post → colar o link `/s/...` numa conversa do WhatsApp → a prévia mostra foto e título.
8. `https://ensaio.noisevault.net` abre a moldura do Ensaio, e a sessão do site principal vale lá (cookie em `.noisevault.net`).
9. No celular: instalar o site como app, abrir sem conexão → página "sem conexão".
10. Rodar o `backup.sh` e restaurar num banco de teste (`createdb` + `pg_restore` em outro nome) para provar que o dump serve.

## 11. Operação do dia a dia

**Nova versão da API:** `git push` na `main`. O workflow "Deploy API" builda a imagem, publica e troca o container no Lightsail; acompanhe em **Actions**. A migration roda no boot. O site fica fora do ar o tempo da troca do container (segundos). **Voltar atrás**: cada imagem também é publicada com a tag do commit (`ghcr.io/caio26gualberto/noisevault-api:<sha>`). No SSH, sem mexer em arquivo nenhum:

```bash
cd ~/noisevault/infra/lightsail
docker pull ghcr.io/caio26gualberto/noisevault-api:<sha-anterior>
docker tag ghcr.io/caio26gualberto/noisevault-api:<sha-anterior> ghcr.io/caio26gualberto/noisevault-api:latest
docker compose up -d api
```

Migration já aplicada não é desfeita sozinha: se a versão nova criou tabela, restaure o backup antes. O próximo push na `main` volta a publicar o `latest` novo.

**Nova versão do front:** `git push` na `main`. O workflow "Deploy Front" builda, sobe para o bucket `noisevault-web`, publica a função da borda e invalida o cache (passo 14); o CI do GitHub continua validando em paralelo. Até a virada, quem publica é o Amplify.

**Logs:** `docker compose logs --tail 200 api`. Cada evento é uma linha `LEVEL data (contexto) EVENTO: mensagem | ...`. Um 500 aparece como `UNHANDLED_EXCEPTION` com `X-Trace-Id`.

**Espaço em disco:** `docker system prune -f` de vez em quando apaga imagens antigas do build.

## 12. O que fica para depois do primeiro deploy

Pendências abertas em 17/10/2026, no fim do deploy:

- **SES fora do sandbox.** O pedido de produção está aberto num caso de suporte (resposta enviada com caso de uso, volume e tratamento de bounce). Até aprovar, só endereços verificados no SES recebem e-mail: cadastro de outra pessoa fica em "confirme seu e-mail" sem o e-mail chegar.
- **Primeiro vídeo.** Nenhum vídeo passou pelo MediaConvert ainda; o passo 10.4 continua por fazer.
- **WAF do CloudFront em modo monitor.** Desligar o monitor em CloudFront → Security depois que o vídeo tocar.
- **`sudo reboot` do Lightsail** pelo kernel atualizado no bootstrap, num horário calmo.
- **Backup.** Rodar o `backup.sh` à mão uma vez e conferir o arquivo em `backups/postgres/`; o cron está agendado.

- **MediaConvert.** Primeiro vídeo real confirma o job (`api/.claude/rules/midia-e-video.md`, "O que o primeiro vídeo em produção precisa confirmar"). O ffmpeg continua no container como saída de emergência.
- **CloudWatch.** Os logs ficam no Docker do servidor (5 arquivos de 50 MB). Se precisar de busca ou alarme, o agente do CloudWatch entra no Lightsail.
- **Fila de e-mail.** O envio do "esqueci a senha" é síncrono; com o SES real, mover para a fila `emails` (`auth-e-contas.md`).
- **Plano do CloudFront.** Começa no flat-rate Free; subir para Pro quando o console avisar que está perto do limite.
- **RDS e Fargate.** O roteiro de sair do Postgres do servidor está em `hospedagem.md`, seção "Quando o banco sair do Lightsail para o RDS".

## 13. Entrar com Google e Facebook

As decisões estão em `api/.claude/rules/login-social.md`. Aqui ficam só os cliques. As páginas `/privacidade` e `/apagar-dados` já sobem com o front.

### 13.1 Google

1. **Google Cloud Console → New project** `noisevault`.
2. **APIs & Services → OAuth consent screen**: tipo **External**, nome "Noise Vault", e-mail de suporte, logo opcional, domínio autorizado `noisevault.net`, página inicial `https://noisevault.net`, política de privacidade `https://noisevault.net/privacidade`. Escopos: só os básicos (`openid`, `email`, `profile`). **Publish app**: com escopos básicos não precisa de verificação do Google.
3. **Credentials → Create credentials → OAuth client ID**, tipo **Web application**. Em **Authorized JavaScript origins**: `https://noisevault.net` e `http://localhost:5173`. Não precisa de redirect URI (o login é por popup). Copie o **Client ID**. O Client Secret não é usado.

### 13.2 Facebook

1. **developers.facebook.com → Meus apps → Criar app**, caso de uso **"Autenticar e solicitar dados de usuários com o Login do Facebook"**, nome "Noise Vault".
2. **Casos de uso → Login do Facebook → Permissões**: `public_profile` e `email` (as duas já vêm com acesso padrão; não precisa de análise do app).
3. **Configurações do app → Básico**: domínio `noisevault.net`, URL da política de privacidade `https://noisevault.net/privacidade`, URL das instruções de exclusão de dados `https://noisevault.net/apagar-dados`, categoria, ícone de 1024 px. Copie o **ID do app** e a **Chave secreta do app**.
4. **Login do Facebook → Configurações**: "Login com o SDK do JavaScript" **ligado**, domínios permitidos para o SDK `https://noisevault.net`.
5. **Publicar** o app (modo Ativo). Antes disso, só quem é administrador ou testador do app consegue entrar; é assim que se testa em `localhost`.

### 13.3 Onde vão as chaves

- **Lightsail** (`~/noisevault/infra/lightsail/.env`): `Auth__Google__ClientId`, `Auth__Facebook__AppId` e `Auth__Facebook__AppSecret`; depois `docker compose up -d api`. O boot deixa de avisar `EXTERNAL_PROVIDER_DISABLED`.
- **Amplify → Environment variables**: `VITE_GOOGLE_CLIENT_ID` e `VITE_FACEBOOK_APP_ID` (os mesmos valores públicos), e **Redeploy**. Sem elas os botões não aparecem.
- **Dev**: `frontend/.env` com as duas `VITE_*`, e na API o bloco `Auth` do `appsettings.Development.json` (`Google:ClientId`, `Facebook:AppId`, `Facebook:AppSecret`). O arquivo fica fora do Git.

## 14. Front em S3 + CloudFront (saída do Amplify)

Decidido em 20/10/2026, para a prévia do WhatsApp e do Facebook aparecer também no link copiado da barra do navegador, e não só no link do botão "Compartilhar". O porquê está em `api/.claude/rules/hospedagem.md`, "Front e a prévia de links". As peças do repositório são `infra/cloudfront/web-viewer-request.js` (a função da borda) e `.github/workflows/deploy-front.yml` (o deploy).

Os passos 14.1 a 14.5 não mexem no site no ar: o Amplify continua servindo até a virada (14.6). Tudo é feito na região **São Paulo**, menos o certificado, que já existe (o coringa do passo 3).

### 14.1 O bucket do site

1. **S3 → Create bucket.** Nome `noisevault-web`, região São Paulo, **Block all public access ligado**. Crie.
2. Não precisa de CORS nem de lifecycle. A política do bucket o assistente do CloudFront grava no 14.3.

### 14.2 A função da borda

1. **CloudFront → Functions → Create function.** Nome `noisevault-web-viewer-request` (exatamente esse, porque o workflow procura por ele), runtime **cloudfront-js-2.0**. Crie.
2. Na aba **Build**, apague o exemplo e cole o conteúdo de `infra/cloudfront/web-viewer-request.js`. **Save changes.**
3. Aba **Publish → Publish function.** Daqui em diante, o workflow republica o código do repositório a cada deploy; edição feita no console é sobrescrita.

### 14.3 A distribuição

1. **CloudFront → Create distribution.**
   - **Choose a plan:** Flat-rate plans → **Free**. É um segundo plano Free na conta (o limite é 3), com cota própria: o site não divide com a mídia o 1 milhão de requisições e os 100 GB.
   - **Get started:** nome `noisevault-web`.
   - **Specify origin:** Amazon S3, origem `noisevault-web.s3.sa-east-1.amazonaws.com`, sem origin path, **Allow private S3 bucket access to CloudFront** marcado (OAC). Settings recomendados para S3.
   - **Enable security:** as proteções incluídas ficam, com **Use monitor mode** marcado na primeira semana (desmarque depois, como na mídia).
   - **Review and create.**
2. **Origins → Create origin**, a origem da API:
   - Origin domain: `api.noisevault.net`;
   - Protocol: **HTTPS only**, porta 443, TLSv1.2;
   - **Name: `noisevault-api`**, exatamente esse, porque a função escolhe a origem por esse nome.
   - Crie.
3. **Behaviors → o behavior padrão → Edit:**
   - Viewer protocol policy: **Redirect HTTP to HTTPS**;
   - Allowed HTTP methods: **GET, HEAD**;
   - Cache policy: **CachingOptimized**. Ela respeita o `Cache-Control` de cada objeto e os 5 minutos da prévia da API;
   - Origin request policy e Response headers policy: **nenhuma**;
   - **Function associations → Viewer request:** Function type **CloudFront Functions**, função `noisevault-web-viewer-request`. Viewer response, origin request e origin response ficam vazios;
   - Save changes.
4. Na aba **General**, copie o **Distribution domain name** (`dxxxx.cloudfront.net`) e o **ID** (`E...`).
5. **Ainda não** adicione os domínios: eles estão no Amplify, e a AWS recusa o mesmo nome em duas distribuições. Isso fica para o 14.6.

### 14.4 O acesso do GitHub Actions (OIDC, sem chave guardada)

1. **IAM → Identity providers → Add provider:**
   - Provider type: **OpenID Connect**;
   - Provider URL: `https://token.actions.githubusercontent.com`;
   - Audience: `sts.amazonaws.com`;
   - Add provider.
2. **IAM → Policies → Create policy → JSON**. Cole a política abaixo, trocando `<conta>` e o ID da distribuição. Nome `noisevault-web-deploy`.

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Sid": "Site",
      "Effect": "Allow",
      "Action": ["s3:PutObject", "s3:GetObject", "s3:DeleteObject"],
      "Resource": "arn:aws:s3:::noisevault-web/*"
    },
    {
      "Sid": "SiteList",
      "Effect": "Allow",
      "Action": "s3:ListBucket",
      "Resource": "arn:aws:s3:::noisevault-web"
    },
    {
      "Sid": "Function",
      "Effect": "Allow",
      "Action": ["cloudfront:DescribeFunction", "cloudfront:UpdateFunction", "cloudfront:PublishFunction"],
      "Resource": "arn:aws:cloudfront::<conta>:function/noisevault-web-viewer-request"
    },
    {
      "Sid": "Invalidate",
      "Effect": "Allow",
      "Action": "cloudfront:CreateInvalidation",
      "Resource": "arn:aws:cloudfront::<conta>:distribution/<ID da distribuição>"
    }
  ]
}
```

3. **IAM → Roles → Create role:**
   - Trusted entity: **Web identity**, provider `token.actions.githubusercontent.com`, audience `sts.amazonaws.com`;
   - GitHub organization `Caio26Gualberto`, repository `NoiseVault`, branch `main`;
   - Permissions: `noisevault-web-deploy`;
   - Nome `noisevault-web-deploy`. Crie e copie o **ARN** da role.
4. **Corrija a relação de confiança.** O repositório usa o *subject* imutável do OIDC do GitHub, com os IDs numéricos do dono e do repositório. Ele se confere com `gh api repos/Caio26Gualberto/NoiseVault/actions/oidc/customization/sub`. O `sub` que o assistente grava (`repo:Caio26Gualberto/NoiseVault:...`) nunca casa, e o deploy falha com `Not authorized to perform sts:AssumeRoleWithWebIdentity` (visto em 20/10/2026). Em **Trust relationships → Edit trust policy**, a condição fica com o valor exato da `main`, sem `StringLike` nem `*`: só um workflow rodando na `main` deste repositório assume a role, e o disparo manual precisa sair da `main` (o padrão do "Run workflow").

```json
"StringEquals": {
  "token.actions.githubusercontent.com:aud": "sts.amazonaws.com",
  "token.actions.githubusercontent.com:sub": "repo:Caio26Gualberto@84203198/NoiseVault@1417948642:ref:refs/heads/main"
}
```

### 14.5 As variables do repositório e o primeiro deploy

1. **GitHub → repositório → Settings → Secrets and variables → Actions → aba Variables → New repository variable.** São valores públicos, por isso variable e não secret:

| Variable | Valor |
|---|---|
| `WEB_BUCKET` | `noisevault-web` |
| `WEB_DISTRIBUTION_ID` | o ID `E...` do 14.3 |
| `WEB_DEPLOY_ROLE_ARN` | o ARN da role do 14.4 |
| `VITE_API_BASE_URL` | `https://api.noisevault.net` |
| `VITE_MAIN_URL` | `https://noisevault.net` |
| `VITE_ENSAIO_URL` | `https://ensaio.noisevault.net` |
| `VITE_GOOGLE_CLIENT_ID` | o mesmo do Amplify |
| `VITE_FACEBOOK_APP_ID` | o mesmo do Amplify |

   Sem `WEB_BUCKET`, o job "Deploy Front" é pulado. É por isso que os pushes anteriores a este passo não falharam.
2. **Actions → Deploy Front → Run workflow.** Leva uns 2 minutos.
3. Teste pelo endereço da distribuição, antes de mexer no domínio:
   - `https://dxxxx.cloudfront.net/` abre o site. O login não funciona aqui, porque esse endereço não está no CORS da API. É esperado;
   - `https://dxxxx.cloudfront.net/eventos` (rota da SPA) abre o site, e não um erro XML do S3;
   - `curl -s -A "WhatsApp/2.23" https://dxxxx.cloudfront.net/mural/1 | grep og:title` mostra o título do registro;
   - `curl -sI https://dxxxx.cloudfront.net/sw.js` mostra `cache-control: no-cache`.

### 14.6 A virada (de madrugada: o site fica fora do ar alguns minutos)

1. **Amplify → app `noisevault` → Hosting → Custom domains → `noisevault.net` → Remove domain.** O domínio sai junto com o `www` e o `ensaio`. A partir daqui o site está fora do ar.
2. **CloudFront → `noisevault-web` → General → Alternate domain names → Add domain**: `noisevault.net`, `www.noisevault.net` e `ensaio.noisevault.net`, com o certificado coringa do passo 3. Se recusar com "already associated", o Amplify ainda não liberou o nome: espere uns minutos e tente de novo.
3. **Cloudflare → DNS → Records:** edite os três CNAMEs que apontavam para o Amplify (`noisevault.net`, `www` e `ensaio`). O Target de todos passa a ser o `dxxxx.cloudfront.net` do 14.3, sempre **DNS only**.
4. Espere a distribuição sair de "Deploying" e confira:
   - `https://noisevault.net` abre, o login funciona e a sessão vale em `https://ensaio.noisevault.net`;
   - `https://www.noisevault.net/eventos` redireciona para `https://noisevault.net/eventos`;
   - copie da barra o endereço de um registro do mural e cole numa conversa do WhatsApp: a prévia mostra foto e título. No Facebook, confira no Sharing Debugger (https://developers.facebook.com/tools/debug/), com **Scrape Again**;
   - o link do botão "Compartilhar" (`/s/...`) continua com prévia;
   - no celular com o app instalado, a versão nova chega (aviso de atualização) e a página sem conexão continua aparecendo.
5. **Se algo der errado:** remova os três domínios da distribuição, adicione de novo no Amplify (passo 8.7) e volte os CNAMEs para o domínio do Amplify. A verificação do Amplify leva de 10 a 30 minutos.

### 14.7 Depois da virada

- Apague o app no Amplify (**App settings → General → Delete app**), para ele não buildar mais a cada push.
- Apague `amplify.yml` e `customHttp.yml` da raiz do repositório, num commit `Repo:`.
- Desmarque o monitor mode da segurança da `noisevault-web` depois da primeira semana.
