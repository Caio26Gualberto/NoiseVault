# Deploy do Noise Vault na AWS (São Paulo), pelo console

Roteiro do primeiro deploy, feito pelo painel da AWS, na região **América do Sul (São Paulo), `sa-east-1`**. As decisões por trás de cada peça estão em `api/.claude/rules/hospedagem.md`; aqui é só o passo a passo. Siga na ordem: cada bloco usa algo criado no anterior.

O que vai existir no fim:

| Peça | Serviço | Endereço |
|---|---|---|
| API, Hangfire, SignalR, Postgres, Caddy | Lightsail (1 instância, Docker) | `api.noisevault.net` |
| Fotos e vídeos | S3 privado + CloudFront | `media.noisevault.net` |
| Conversão de vídeo | MediaConvert (por uso) | sem endereço; a API cria o job |
| E-mail de confirmação e redefinição | SES por SMTP | `nao-responda@noisevault.net` |
| Site e o Ensaio | Amplify (um app, dois domínios) | `noisevault.net`, `ensaio.noisevault.net` |
| DNS | Route 53 | zona `noisevault.net` |

Arquivos do repositório usados aqui: `infra/lightsail/` (compose, Caddyfile, `.env.example`, `backup.sh`), `api/Dockerfile`, `amplify.yml` na raiz e `frontend/customHttp.yml`.

## 0. Antes de abrir o console

Tenha em mãos:

- a conta AWS com MFA ligado no usuário root, e um usuário IAM administrador para o dia a dia (nunca opere com o root);
- o acesso ao registrador onde o `noisevault.net` foi comprado, para trocar os servidores de nome;
- a **chave de licença da Six Labors** (ImageSharp). A licença Split é gratuita abaixo de US$ 1 milhão de receita, pedida em https://sixlabors.com/pricing/. Sem ela o build em Release falha;
- o **Client ID e Client Secret do Spotify** (painel de desenvolvedor). Renomeie o app para "Noise Vault", site `https://noisevault.net`, e apague o redirect de `localhost:7213`;
- o repositório no GitHub com a `main` atualizada (o Amplify vai ler de lá);
- um segredo para o JWT. Gere no seu terminal e guarde: `openssl rand -base64 48`.

No canto superior direito do console, escolha a região **South America (São Paulo)**. Tudo abaixo é nessa região, com uma exceção marcada no CloudFront.

## 1. Route 53: a zona do domínio

1. **Route 53 → Hosted zones → Create hosted zone.** Domain name `noisevault.net`, tipo Public. Crie.
2. Abra a zona e copie os quatro servidores de nome do registro **NS** (terminam em `awsdns-xx.org`, `.com`, `.net`, `.co.uk`).
3. No **registrador** do domínio, troque os servidores de nome pelos quatro. A propagação leva de minutos a algumas horas. Enquanto não propaga, os passos seguintes funcionam; só a validação de certificado e o acesso pelo nome esperam.

Os registros A e CNAME entram nos passos de cada serviço.

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

## 3. Certificado para a mídia (região Virgínia)

O CloudFront só aceita certificado emitido na região **us-east-1 (N. Virginia)**. Troque a região só para este passo.

1. **Certificate Manager → Request → Request a public certificate.** Nome `media.noisevault.net`. Validação por DNS. Request.
2. Abra o certificado e clique **Create records in Route 53**. Ele cria o CNAME de validação na zona. Em alguns minutos o status vira **Issued**.
3. Volte a região para **São Paulo**.

## 4. CloudFront: servir a mídia

1. **CloudFront → Create distribution.**
   - Origin domain: escolha o bucket `noisevault-media`.
   - Origin access: **Origin access control settings (recommended)** → **Create new OAC**, aceite o padrão (Sign requests). Depois de criar a distribuição, o console mostra um aviso com **Copy policy**: copie e cole em **S3 → bucket → Permissions → Bucket policy**. É o que deixa só o CloudFront ler o bucket.
   - Viewer protocol policy: **Redirect HTTP to HTTPS**.
   - Allowed HTTP methods: **GET, HEAD**.
   - Cache policy: **CachingOptimized**.
   - Response headers policy: **Create policy** (abre em outra aba): nome `noisevault-media-cors`, ligue **CORS** com Access-Control-Allow-Origin `https://noisevault.net` e `https://ensaio.noisevault.net`, methods `GET, HEAD`, headers `*`, sem credentials, **Origin override** ligado. Volte e selecione. Sem isso o player de vídeo (hls.js) não consegue baixar as playlists.
   - Web Application Firewall: **Do not enable** (custa).
   - Price class: **Use only North America, Europe and South America**, ou all edge locations se preferir.
   - Alternate domain name (CNAME): `media.noisevault.net`. Custom SSL certificate: o certificado do passo 3.
   - Crie. A distribuição leva uns minutos para "Deployed".
2. **Route 53 → zona → Create record.** Nome `media`, tipo **A**, **Alias** ligado, destino "Alias to CloudFront distribution", escolha a distribuição. Crie.
3. Teste depois do passo 7 (quando houver um arquivo): `https://media.noisevault.net/<chave>` abre a imagem.

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

1. **Amazon SES → Identities → Create identity → Domain.** `noisevault.net`. Deixe **Easy DKIM** ligado, e marque **Publish DNS records to Route 53**. Crie. Em minutos a identidade fica **Verified**.
2. Na identidade → **Custom MAIL FROM domain → Edit**: `mail.noisevault.net`, com publicação no Route 53. Isso alinha o SPF.
3. **Route 53 → zona → Create record** tipo **TXT**, nome `_dmarc`, valor `"v=DMARC1; p=quarantine; rua=mailto:<seu e-mail>"`. Com SPF e DKIM alinhados, a confirmação de conta não cai no spam.
4. **SES → SMTP settings → Create SMTP credentials.** Nome `noisevault-smtp`. Guarde o **SMTP user name** e a **SMTP password** (`Email__Smtp__Username` e `Email__Smtp__Password`). O host é `email-smtp.sa-east-1.amazonaws.com`, porta 587, STARTTLS.
5. **SES → Account dashboard → Request production access.** A conta nasce em *sandbox* (só manda para endereços verificados). Preencha: caso de uso "transactional", site `https://noisevault.net`, descrição "confirmação de cadastro e redefinição de senha de uma comunidade musical; sem marketing", volume baixo. A aprovação leva até 24 h. Enquanto espera, verifique o seu próprio e-mail em **Identities → Create identity → Email address** para testar.

## 7. Lightsail: o servidor da API

### 7.1 Criar a instância

1. **Lightsail → Instances → Create instance.** Região São Paulo (`sa-east-1`, zona A). Plataforma Linux, blueprint **OS Only → Ubuntu 24.04 LTS**. Plano **4 GB RAM, 2 vCPUs, 80 GB SSD** (23,54 USD). Nome `noisevault-api`. Crie.
2. **Networking → IPv4 networking → Attach static IP**: crie `noisevault-api-ip` e anexe. Anote o IP.
3. Na instância → **Networking → IPv4 Firewall**: deixe `SSH 22`, `HTTP 80` e adicione **HTTPS 443** (TCP). Adicione também **443 UDP** se quiser HTTP/3 pelo Caddy; opcional.
4. **Route 53 → zona → Create record.** Nome `api`, tipo **A**, valor o IP estático, TTL 300. Crie.
5. **Lightsail → instância → Snapshots → Automatic snapshots: Enable**, horário de madrugada. É o backup do disco inteiro.

### 7.2 Preparar o servidor

Conecte pelo botão **Connect using SSH** do console (ou baixe a chave em Account → SSH keys e use `ssh ubuntu@<ip>`). No terminal:

```bash
sudo apt-get update && sudo apt-get upgrade -y
curl -fsSL https://get.docker.com | sudo sh
sudo usermod -aG docker ubuntu
sudo apt-get install -y git unzip
curl "https://awscli.amazonaws.com/awscli-exe-linux-x86_64.zip" -o awscliv2.zip && unzip -q awscliv2.zip && sudo ./aws/install && rm -rf aws awscliv2.zip

sudo fallocate -l 2G /swapfile && sudo chmod 600 /swapfile && sudo mkswap /swapfile && sudo swapon /swapfile
echo '/swapfile none swap sw 0 0' | sudo tee -a /etc/fstab
```

O swap é para o build da imagem .NET caber em 4 GB. Saia e entre de novo no SSH para o grupo `docker` valer.

### 7.3 Subir

```bash
git clone https://github.com/Caio26Gualberto/NoiseVault.git ~/noisevault
cd ~/noisevault/infra/lightsail
cp .env.example .env
nano .env
```

Preencha o `.env` com tudo que você guardou: senha do Postgres (invente uma longa), chave da Six Labors, segredo do JWT, chaves do IAM, o ARN da role do MediaConvert, credenciais SMTP, Spotify, e o seu e-mail em `Email__VerificationInbox`. Nenhum valor fica vazio exceto os que o exemplo já deixa em branco de propósito.

```bash
docker compose build
docker compose up -d
docker compose logs -f api
```

O primeiro build leva uns 10 minutos. No log da API, espere `Now listening on: http://[::]:8080`. O compose liga `Database__MigrateOnStartup=true`, então as migrations rodam nesse primeiro boot (e em todo boot seguinte, sem efeito quando não há nada novo).

O Caddy pede o certificado do `api.noisevault.net` sozinho na primeira requisição. Teste no navegador: `https://api.noisevault.net/health` responde `Healthy`.

Se o log mostrar `Cors:AllowedOrigins não configurado` ou `JWT:SecretKey não configurado`, falta variável no `.env`. Corrija e `docker compose up -d` de novo.

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

6. **Hosting → Custom headers**: o Amplify lê o `frontend/customHttp.yml` do repositório (`sw.js` e manifestos sem cache, `assets/` com cache de um ano). Confira que aparece na tela; se não, cole o conteúdo do arquivo ali.
7. **Hosting → Custom domains → Add domain.** Escolha `noisevault.net` (ele acha a zona no Route 53). Configure:
   - `noisevault.net` → branch `main`;
   - `www` → **Redirect to** `https://noisevault.net` (marque a opção de redirect, não de branch);
   - **Add** `ensaio` → branch `main`.
   
   O Amplify emite o certificado e cria os registros na zona. Leva de 10 a 30 minutos para "Available".

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

**Nova versão da API:**

```bash
cd ~/noisevault && git pull
cd infra/lightsail && docker compose build api && docker compose up -d api
docker compose logs -f api
```

A migration roda no boot. O site fica fora do ar o tempo da troca do container (segundos). **Voltar atrás**: `git checkout <commit anterior>` e os mesmos dois comandos; migration já aplicada não é desfeita sozinha, então antes de um `git checkout` para trás, restaure o backup se a versão nova criou tabela.

**Nova versão do front:** `git push` na `main`. O Amplify builda e publica sozinho; o CI do GitHub continua validando antes.

**Logs:** `docker compose logs --tail 200 api`. Cada evento é uma linha `LEVEL data (contexto) EVENTO: mensagem | ...`. Um 500 aparece como `UNHANDLED_EXCEPTION` com `X-Trace-Id`.

**Espaço em disco:** `docker system prune -f` de vez em quando apaga imagens antigas do build.

## 12. O que fica para depois do primeiro deploy

- **MediaConvert.** Primeiro vídeo real confirma o job (`api/.claude/rules/midia-e-video.md`, "O que o primeiro vídeo em produção precisa confirmar"). O ffmpeg continua no container como saída de emergência.
- **CloudWatch.** Os logs ficam no Docker do servidor (5 arquivos de 50 MB). Se precisar de busca ou alarme, o agente do CloudWatch entra no Lightsail.
- **Fila de e-mail.** O envio do "esqueci a senha" é síncrono; com o SES real, mover para a fila `emails` (`auth-e-contas.md`).
- **Plano do CloudFront.** Começa no flat-rate Free; subir para Pro quando o console avisar que está perto do limite.
- **RDS e Fargate.** O roteiro de sair do Postgres do servidor está em `hospedagem.md`, seção "Quando o banco sair do Lightsail para o RDS".
