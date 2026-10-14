---
paths:
  - "**/Dockerfile"
  - "**/docker-compose*.yml"
  - "infra/**"
  - "**/NoiseVault.Api/Program.cs"
  - "**/appsettings.Production.json"
---

# Decisões — Hospedagem em produção

Decidido em 14/10/2026, **sem deploy ainda**. Nada aqui existe na AWS. A rule guarda a escolha e as pendências para o dia em que o deploy acontecer. Decisão do usuário: "vai ser tudo na AWS".

## A arquitetura: um servidor e serviços gerenciados por uso

| Peça | Onde | Observação |
|---|---|---|
| API + Hangfire + SignalR + Postgres | **Lightsail em São Paulo**, bundle de 4 GB (2 vCPU, 80 GB SSD, 4 TB de transferência), 23,54 USD/mês | `docker compose` com a API, o Postgres e um Caddy na frente (HTTPS e WebSocket) |
| Fotos e vídeos | **S3** em São Paulo, bucket privado + **CloudFront** com OAC | como já previsto em `armazenamento.md`. Plano flat-rate Free do CloudFront no começo, Pro (15 USD) quando chegar perto do limite |
| Conversão de vídeo | **MediaConvert** em São Paulo, por uso | desde o primeiro deploy (`midia-e-video.md`). O ffmpeg fica só para dev |
| E-mail | **SES** por SMTP | o `SmtpEmailSender` atual serve (`email.md`) |
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

## Pendências antes do primeiro deploy

- **Credenciais**: o Lightsail não tem IAM role como o EC2. Um usuário IAM com chave em variável de ambiente (`Storage__AccessKey`, `Storage__SecretKey`; o `StorageSettings` já aceita), com o mínimo:
  - S3 no bucket da mídia e no prefixo de backup;
  - `mediaconvert:CreateJob`, `mediaconvert:GetJob` e `iam:PassRole` só na role de serviço do MediaConvert;
  - as credenciais SMTP do SES são de outro usuário IAM, também por variável de ambiente (`Email__Smtp__*`).
- **Postgres no servidor**: `pg_dump` diário para o S3 (prefixo próprio, retenção definida no deploy) e snapshot automático do Lightsail. Restaurar um backup faz parte do roteiro de verificação do deploy, não fica para quando precisar.
- **Data Protection**: persistir as chaves antes de subir (`auth-e-contas.md`), senão cada restart do container invalida os links de confirmação e de redefinição.
- **Hangfire**: o dashboard já não é mapeado em `Production` (`jobs-hangfire.md`).
- **Licença do ImageSharp**: o build em `Release` **falha** sem chave da Six Labors ("No Six Labors license found"; o `Debug` só avisa). Visto em 14/10/2026. A licença Split é gratuita abaixo de US$ 1 milhão de receita (`armazenamento.md`), mas a chave precisa existir no build da imagem (`SixLaborsLicenseKey` por variável, nunca no Git).
- **SignalR**: uma instância, sem backplane. O Caddy repassa WebSocket; o keep-alive de 15 s sobra para qualquer idle timeout (`notificacoes.md`).
- **CORS**: `Cors:AllowedOrigins` e o CORS do bucket com o domínio de produção (`midia-e-video.md`).
- **E-mail**: com provedor real, o envio do esqueci-senha vai para a fila `emails` (o risco aceito de tempo de resposta em `auth-e-contas.md`).

## Front e a prévia de links

O usuário escolheu o **Amplify** para o front (estava na estimativa, 1,77 USD/mês). Ele hospeda a SPA sem esforço, mas fecha um caminho da prévia Open Graph (`IDEIAS.md`, ideia 1):

- o CloudFront do Amplify é gerenciado por ele, e não aceita uma função nossa;
- os rewrites do Amplify filtram por caminho e país, não por `User-Agent`.

Com o Amplify, a prévia sai por **rota de compartilhamento**: um rewrite `/s/<*>` do Amplify repassa para a API, que devolve o HTML com as tags `og:*` e redireciona a pessoa para a página. Funciona no link do botão "Compartilhar", não no link copiado da barra do navegador.

A alternativa que cobre os dois links é o front em **S3 + o mesmo CloudFront da mídia** (sem custo extra no plano flat-rate), com uma CloudFront Function de viewer request que manda os robôs (`facebookexternalhit`, `WhatsApp`, `Twitterbot`, `TelegramBot`, `Discordbot`) para a API com `cf.updateRequestOrigin`. O deploy vira `aws s3 sync` + invalidação no CI. **A escolha fica para quando a ideia 1 virar módulo.**

## Quando revisar

- A CPU do bundle vive no teto fora das conversões: subir de bundle, ou ir para os bundles "compute-optimized".
- A API precisa de 2 instâncias: entra o backplane do SignalR (`notificacoes.md`), o Postgres sai do servidor (RDS) e volta um balanceador.
- O CloudFront chega perto do limite do plano Free: plano Pro.
