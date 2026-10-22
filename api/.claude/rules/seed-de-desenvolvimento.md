---
paths:
  - "**/NoiseVault.Infra.Data/Context/Seeding/**"
  - "**/NoiseVault.Api/Program.cs"
  - "**/NoiseVault.Api/appsettings.Development.json"
---

# Decisões — Seed de desenvolvimento

Pedido do usuário em 15/10/2026: "logar e ter dados para ver como ficaria o site" sem cadastrar tudo na mão a cada banco novo, e sem perder as fotos e os vídeos a cada restart do LocalStack. O seed de dados (`DevSeed`) é separado do seed de sistema (`SeedData`: papéis, permissões, cidades, admin), que continua rodando em todo ambiente.

## Só em Development, e só com a chave ligada

`Program.cs` chama `DevSeed.SeedAsync` depois do `SeedData`, quando `IsDevelopment()` **e** `Seed:DevData` é `true`. O `appsettings.Development.json` liga a chave; o `appsettings.json` não a tem, então produção e qualquer ambiente sem o arquivo de dev ficam de fora por construção. Para subir um banco de dev limpo sem o conjunto, basta `Seed__DevData=false`.

O seed **nunca derruba o boot**: qualquer exceção vira `DEV_SEED_FAILED` (Error) e a API sobe sem os dados. Um LocalStack ainda subindo, um ffmpeg fora do PATH ou um bucket sem CORS não podem impedir o desenvolvedor de rodar a API.

## O conjunto

Contas, todas com senha `12341234` e e-mail `{nickname}@noisevault.dev`, já confirmadas, papel `User`:

| Nickname | Quem | Cidade |
|---|---|---|
| `lucia.ferraz` | vocal da Ferrugem | Santo André |
| `rafa.tavares` | guitarra e Owner da Cinzas | São Caetano |
| `marina.k` | bateria da Ferrugem, Owner da Pogo Mortal | São Bernardo |
| `denis.moura` | baixo da Ferrugem, Owner da Vulto | Diadema |
| `paty.lins` | fã e fotógrafa, sem banda | Mauá |
| `jorge.okamoto` | guitarrista procurando banda, baixo na Pogo Mortal | Ribeirão Pires |
| `bia.santana` | teclado na Vulto | Santo André |
| `tiago.nunes` | bateria na Pogo Mortal e na Cinzas | Rio Grande da Serra |

O admin de dev (`caiogualbertodev@outlook.com`) é o **Owner da Ferrugem** e recebe bio, cidade, gêneros, avatar e capa se ainda não tiver avatar. Assim quem loga como admin já vê a própria banda, os convites que mandou, a vaga aberta, o mural que publicou e a caixa de notificações cheia.

Bandas: **Ferrugem** (thrash, 1998, verificada), **Pogo Mortal** (punk/hardcore, 2015), **Vulto** (doom/sludge, 2021), **Cinzas** (death melódico, 2009, verificada). Convites pendentes para `jorge.okamoto` (Ferrugem) e `bia.santana` (Cinzas), um recusado.

**As bandas de verdade entram como nome livre, nunca como `Band` cadastrada**: Necromancia, MX, Sepultura, Destruction, Sodom, Tankard e Rage aparecem no line-up dos eventos, nas bandas dos registros do mural e nos textos (pedido do usuário em 15/10/2026, para a apresentação parecer a cena de verdade). Cadastrar `Sepultura` como banda com Owner do seed seria mentir sobre quem é dono do perfil; nome livre é exatamente o caso de uso do line-up e do mural (`eventos.md`, `mural.md`). Os lugares são reais quando existem (Santo Rock Bar em Santo André, Teatro Municipal de Santo André, Teatro Lauro Gomes, Teatro Clara Nunes, Praça da Matriz) e inventados quando não (Quadra do Jardim Zaíra).

Seguidores: a Ferrugem tem cinco (`paty`, `jorge`, `rafa`, `bia`, `tiago`), e o admin segue Pogo Mortal, Vulto e Cinzas, para a aba "Seguindo" do feed vir cheia já no primeiro acesso; a `paty` tem a notificação 33 do post de vídeo da Ferrugem. Feed: nove posts de pessoa e de banda (um com três fotos do show no Santo Rock Bar, um da Ferrugem com vídeo, um com a banda no palco e pessoas marcadas na foto, um procurando a fita do MX, @menções de pessoas e de bandas), curtidas e comentários, mais os posts automáticos de evento, mural e vaga. Eventos: Necromancia com Ferrugem e Vulto no Santo Rock Bar, MX com Cinzas no Santo Rock Bar, Pogo na Praça, um que já rolou no Teatro Clara Nunes, o Metal na Serra cancelado (com MX no line-up) e uma tarde de graça em Mauá, com "Vou" e comentários. Mural: quatro registros publicados (Necromancia e Ferrugem no Teatro Municipal em 1999, com fotos e fita VHS; Ozzy em Cardiff em 1980, com cidade de fora do Brasil; Korzus e Ferrugem no Teatro Lauro Gomes em 2003; a primeira edição do Pogo na Praça), um envio pendente da `paty.lins` e um recusado do `jorge.okamoto` com motivo. Ensaio: cinco fichas de músico (quatro "procurando banda", com alcance e fotos de flyer), quatro vagas (uma preenchida) e interesses. Suporte: três tickets (aberto com print, em andamento com conversa, resolvido com nota interna). Notificações lidas e não lidas para o admin e para as outras pessoas.

**Notícias** (`DevSeed.News.cs`): seis manchetes reais do Whiplash.Net coletadas em 21/10/2026, com o link e a foto deles, três de hoje e três de ontem em horas relativas ao boot. Entram só quando `NewsArticles` está vazia, então a coleta real e a purga seguem o curso normal. Existem porque em dev no Windows o Cloudflare bloqueia o coletor (`noticias.md`), e sem elas a página nasceria vazia.

Os nomes são inventados e nenhuma tribo manda: thrash, punk, hardcore, doom, death e gótico aparecem na mesma pista, como o `CLAUDE.md` da raiz pede.

## Idempotente por marcador, não por contagem

- As **pessoas** são procuradas pelo e-mail; quem já existe não é recriado nem alterado.
- O **conteúdo** é criado uma vez, numa transação só, e o marcador é a banda com slug `ferrugem`. Com ela no banco, nada de conteúdo é tocado: o que o desenvolvedor editar, curtir ou apagar fica como está. Para o conjunto voltar ao original, derrube o banco (`docker compose down -v`), não há "reset" parcial.
- As datas de criação são **retroativas** (`ExecuteUpdateAsync` em `CreatedAt` depois do insert, porque o `ApplyAuditing` do contexto carimba `UtcNow` em tudo que entra). Sem isso o feed inteiro nasceria no mesmo segundo e a ordem das conversas não faria sentido.

## Mídia: chave previsível, regenerada a cada boot

🔴 **O LocalStack Community perde o bucket a cada restart** (`armazenamento.md`), e o Postgres continua apontando para as chaves. Era o "toda vez somem as imagens e vídeos" do pedido. A resposta não é persistir o LocalStack (não existe sem plano pago) e sim **regenerar o que sumiu**:

- Toda chave do seed carrega o marcador `seed-` no nome (`users/{id}/avatar/seed-avatar.webp`, `users/{id}/photos/seed-show-1.webp`, `users/{id}/videos/seed-ensaio-lamina-fria/master.m3u8`). É a exceção registrada à regra "nome imprevisível" de `armazenamento.md`: em dev, a URL precisa ser **reproduzível** para a mesma linha do banco voltar a funcionar depois do restart. O que o desenvolvedor sobe pela tela continua com GUID e **não** é tocado pelo seed.
- A cada boot, `DevSeed` percorre avatar, capa, banner de evento, foto do Ensaio e `MediaUpload` das contas do seed e das quatro bandas, faz um `HEAD` por chave e só sobe o que falta (`DevSeedMedia.EnsureImageAsync`/`EnsureVideoAsync`). Com tudo no lugar, custa uns 50 `HEAD` no LocalStack.
- **A mídia vem de uma pasta no PC do desenvolvedor**, fora do repositório: `%LOCALAPPDATA%\NoiseVault\seed-media` (ou `Seed:MediaPath`), com `avatars\*.jpg` (retratos), `photos\*.jpg` (shows, instrumentos, pista) e `videos\*.mp4`. Decisão do usuário em 15/10/2026: foto real da internet, não imagem inventada; o primeiro corte era procedural (gradiente com grão) e o site ficava com cara de placeholder. Binário não entra no Git; a pasta é por máquina.
- **`api/scripts/baixar-midia-seed.ps1` enche a pasta a partir do Wikimedia Commons**: 52 fotos de banda ao vivo (Metallica, Slayer, Kreator, Sepultura, Megadeth, Exodus, Testament, Overkill, Anthrax, Korzus, Krisiun, Angra, Ozzy em 1980 e 1984, Halford nos anos 70, mais pista e mosh pit), 16 retratos de ícones para avatar (Hetfield, Hammett, Ulrich, Araya, King, Mustaine, Cavalera, Kisser, Petrozza, Chuck Billy, Halford, Lemmy, Ozzy, Anselmo, Dickinson, Gary Holt) e 4 vídeos de show (KK's Priest, Malevolent Creation, wall of death, mosh). Pedido do usuário em 15/10/2026, para a apresentação: "thrash metal old school", avatares de ícones do metal e post e mural só com foto de banda, nada genérico. Cada item do script carrega título e licença (CC BY, CC BY-SA, CC0 ou domínio público), e o script grava `CREDITOS.txt` na pasta. Unsplash e Pexels ficaram de fora: a busca dos dois exige chave de API e a página bloqueia robô; a API do Commons é aberta (com `User-Agent` identificado e pausa entre chamadas, senão responde 429).
- **Vídeo é cortado para 75 s e convertido para MP4 pelo ffmpeg no próprio script** (`-DuracaoVideoSegundos` muda o corte; o original `.webm`/`.ogv` fica em `videos\originais`). Sem o corte, o boot transcodificaria 4 min de show por vídeo. O script pula o que já existe e aceita `-Destino` para outra pasta. **Quem clonar o projeto roda o script uma vez** (ou pede ao agente: "rode `api/scripts/baixar-midia-seed.ps1`"). Para trocar uma foto, troque a URL na lista, apague o arquivo e rode de novo. O PowerShell 5.1 só lê o script com acento se ele tiver BOM UTF-8; o arquivo tem, e precisa continuar tendo.
- **Arquivo com o nome da chave ganha; o resto é por hash**: a chave `users/1/photos/seed-show-1.webp` usa `photos\show-1.jpg` se existir (é como o Korzus cai no registro de 2003, o Kreator no post com marcações e o Ozzy de 1980 no registro dele). Sem arquivo com o nome, a chave escolhe pelo hash (`SHA-256` da chave módulo o tamanho da pasta, em ordem de nome), então avatar, capa e banner não trocam de cara entre restarts; os arquivos `pool-*` existem para esse sorteio. **O sorteio só considera arquivo que cabe no preset** (`FitsPreset`: capa ≥ 800 de largura, flyer ≥ 300×300, avatar ≥ 128×128), lido pelo cabeçalho com `Image.Identify`; antes disso, a capa sorteava uma foto de 554 px e o `IImageProcessor` recusava com `TooSmall` (visto em 15/10/2026). O script não baixa de novo um nome que já existe: ao trocar a URL de um nome, apague o arquivo (ou a pasta inteira) antes de rodar; o que saiu da lista o script apaga sozinho. Avatar de pessoa usa `avatars`; todo o resto (capa, banner de evento, avatar de banda, foto de post e do mural, flyer do Ensaio) usa `photos`, com o preset que o caminho da chave indica (`/avatar/`, `/banner/`, `/musician/`, `seed-archive-` para o mural, o resto `Post`), passando pelo mesmo `IImageProcessor` do upload real.
- **Sem a pasta o seed não para**: cai na imagem procedural (gradiente com círculos, listras e grão, cores do hash da chave) e no vídeo gerado pelo ffmpeg (`testsrc2` com tom de áudio, 6 s), e avisa uma vez por pasta com `DEV_SEED_MEDIA_POOL_MISSING`. É reserva para rodar offline, não o caminho normal.
- **Vídeo** é convertido pelo mesmo `IVideoTranscoder` do job real, então o player do front recebe o HLS idêntico ao de um upload de verdade, com `poster.webp`. Sem ffmpeg o vídeo falha com `DEV_SEED_MEDIA_FAILED` e o resto segue; instale o ffmpeg e reinicie.
- Largura, altura, duração e tamanho do `MediaUpload` são atualizados quando o arquivo é regenerado, para o DTO continuar coerente com o que está no bucket. Para trocar o bucket inteiro pela mídia da pasta (por exemplo, depois de a pasta nascer), `docker compose restart localstack` e reinicie a API: o seed só sobe o que não existe.

## Eventos de log (contexto `Api`)

| Evento | Nível | Quando |
|---|---|---|
| `DEV_SEED_APPLIED` | Info | fim do seed, com `ContentCreated` (conteúdo criado neste boot), `MediaRestored` (arquivos reenviados ao bucket) e `NewsCreated` (manchetes de exemplo, só com a tabela vazia) |
| `DEV_SEED_SKIPPED` | Warning | o admin de dev não existe (o `SeedData` não rodou) |
| `DEV_SEED_MEDIA_POOL_MISSING` | Warning | a pasta `avatars`, `photos` ou `videos` está vazia; a mídia sai procedural (contexto `Storage`). Rode `api/scripts/baixar-midia-seed.ps1` |
| `DEV_SEED_MEDIA_FAILED` | Error | uma mídia não pôde ser regenerada (chave e erro no evento); as outras seguem e o boot continua |
| `DEV_SEED_FAILED` | Error | qualquer outra exceção; a API continua subindo |
| `SEED_TEMP_CLEANUP_FAILED` | Warning | pasta temporária do vídeo ficou para trás (contexto `Storage`) |

## Roteiro de verificação

1. `api/scripts/baixar-midia-seed.ps1` → 77 arquivos em `%LOCALAPPDATA%\NoiseVault\seed-media` (16 em `avatars`, 52 em `photos`, 4 `.mp4` em `videos` mais os 4 originais em `videos\originais`, e `CREDITOS.txt`), uns 180 MB.
2. `docker compose up -d`, banco vazio, API no ar → log `DEV_SEED_APPLIED` com `ContentCreated=true`, `MediaRestored` por volta de 55, sem `DEV_SEED_MEDIA_POOL_MISSING`; `SELECT count(*) FROM "Bands"` = 4; `SELECT count(*) FROM "Posts"` = 23; `SELECT count(*) FROM "Events"` = 6.
3. Login `lucia.ferraz@noisevault.dev` / `12341234` → `GET /api/posts` traz o feed com fotos reais de show (`url` em `http://localhost:4566/noisevault-media/users/…/seed-…webp`) e o vídeo da Ferrugem com `posterUrl`; abrir a URL do avatar no navegador mostra um retrato.
4. `docker compose restart localstack`, reiniciar a API → `DEV_SEED_APPLIED` com `ContentCreated=false` e `MediaRestored` igual ao do passo 2; as mesmas URLs voltam a abrir.
5. Reiniciar a API de novo sem mexer no LocalStack → `MediaRestored=0`.
6. Login como admin → `GET /api/notifications` com itens não lidos; `GET /api/admin/support/tickets` com 3 tickets; `GET /api/admin/archive/submissions` com o envio pendente da `paty.lins`.
7. `Seed__DevData=false` num banco vazio → nada além do `SeedData`; `Seed__DevData=true` num banco já semeado → nenhum registro duplicado.
