---
paths:
  - "**/NoiseVault.Application/Profiles/**"
  - "**/NoiseVault.Application/PublicProfiles/**"
  - "**/NoiseVault.Domain/Entities/User.cs"
  - "**/NoiseVault.Domain/Entities/UserGenre.cs"
  - "**/NoiseVault.Domain/Enums/Profiles/**"
  - "**/NoiseVault.Api/Controllers/ProfileController.cs"
  - "**/NoiseVault.Api/Controllers/UsersController.cs"
---

# Decisões — Perfil e mídia

Módulo 1 da ordem em `dominio.md`. O que uma pessoa mostra sobre si: avatar, capa, bio, cidade, instrumento principal, gêneros favoritos e links sociais; o perfil público em `/perfil/:nickname`; e a busca de pessoas da topbar. O armazenamento das imagens está em `armazenamento.md`; aqui fica a regra do perfil.

## Os campos moram na própria `User`

`Bio`, `City`, `MainInstrument`, `InstagramUrl`, `YoutubeUrl`, `SpotifyUrl`, `AvatarKey`, `BannerKey`, `BannerPosition` são colunas de `Users`; gêneros em `UserGenres (UserId, Genre)` com chave composta. Alternativa descartada: `UserProfile` 1:1. Avatar e nome são lidos em toda lista (autor de post, comentário, integrante de banda); uma tabela à parte custaria um join em cada uma sem ganho — nenhum campo daqui é sensível nem pesado.

O e-mail **nunca** sai no perfil público nem na busca. O legado buscava por e-mail e devolvia quem batia.

## Edição: um `PUT` com o perfil inteiro

`PUT /api/profile` recebe `{ name, bio, city, mainInstrument, genres, socialLinks: { instagram, youtube, spotify } }` e substitui tudo (campo ausente ou vazio = limpa). É o formulário de `/minha-conta`. Substitui o antigo `PATCH /api/profile/name` — dois endpoints para o mesmo formulário só davam chance de um sobrescrever o outro. Imagens têm endpoints próprios porque são multipart e têm custo de processamento.

Validação (imperativa no AppService, mensagens específicas):

| Campo | Regra | Code |
|---|---|---|
| `name` | `UserNameRules` (2–100) | `INVALID_NAME` |
| `bio` | até 500 caracteres depois do trim | `INVALID_BIO` |
| `city` | valor de `EAbcCity` ou nulo | `INVALID_CITY` |
| `mainInstrument` | até 60 caracteres | `INVALID_INSTRUMENT` |
| `genres` | até 10, valores de `EMusicGenre`, sem repetição | `INVALID_GENRE` |
| `socialLinks.*` | `https://` (ou `http://`), até 300, **host do próprio serviço** (`instagram.com`; `youtube.com`, `youtu.be`; `open.spotify.com`, `spotify.com`, com ou sem `www.`/`m.`) | `INVALID_SOCIAL_LINK` |

O host é checado para o link "Instagram" não virar atalho para qualquer site com ícone confiável ao lado.

## Avatar e capa

- `POST /api/profile/avatar` (multipart `file`) → `AVATAR_UPDATED`; `DELETE /api/profile/avatar` → `AVATAR_REMOVED`.
- `POST /api/profile/banner` (multipart `file` + `position` 0–100, padrão 50) → `BANNER_UPDATED`; `PATCH /api/profile/banner-position { position }` → `BANNER_POSITION_UPDATED`; `DELETE /api/profile/banner` → `BANNER_REMOVED`.
- Todos devolvem o `ProfileDto` atualizado (o front troca a sessão sem refetch).
- Erros de arquivo: `IMAGE_REQUIRED` (sem arquivo), `IMAGE_TOO_LARGE` (acima de 5/10 MB, antes de decodificar), `IMAGE_UNSUPPORTED` (não decodifica como JPEG/PNG/WebP), `IMAGE_TOO_SMALL` (abaixo do mínimo do preset), `STORAGE_UNAVAILABLE` (S3 falhou), `INVALID_BANNER_POSITION`.
- Ordem sobe-grava-apaga de `armazenamento.md`. `PATCH banner-position` sem capa → `BANNER_NOT_FOUND`.
- 🔴 **A posição muda sem reenviar a imagem.** No legado reposicionar exigia novo upload e o modal ainda ignorava a posição salva.

## Perfil público e busca

- `GET /api/users/{nickname}` → `UserProfileDto { id, name, nickname, avatarUrl, bannerUrl, bannerPosition, bio, city, mainInstrument, genres, socialLinks, memberSince }`, code `USER_PROFILE_FOUND`. Nickname comparado já normalizado (minúsculo). Inativo, revogado ou inexistente → 404 `PROFILE_NOT_FOUND` (mesma resposta, para não revelar conta desativada).
- `GET /api/users/search?search=&page=&pageSize=` → `PagedResultDto<ProfileSummaryDto { id, name, nickname, avatarUrl }>`, code `USERS_FOUND`. Termo com menos de 2 caracteres devolve página vazia (não erro: a topbar dispara enquanto a pessoa digita). comparação em minúsculas (`Name.ToLower().Contains`, `Nickname.Contains` — o nickname já é gravado minúsculo) em vez de `ILIKE`, que só existe no Postgres e quebraria o `DatabaseProvider`; nunca em `Email` (colação não determinística, não aceita `LIKE`, ver `CLAUDE.md`), ordem por `Nickname` + desempate por `Id`, só ativos.
- `stats` (posts, fotos, vídeos) e `bands` **não existem** neste módulo: entram com feed/galeria (3, 5) e bandas (2). O DTO não traz campo vazio "para o futuro"; o front esconde os blocos.
- Pessoa **não** tem selo de verificado — só banda (módulo 2). `ProfileSummaryDto` não tem `isVerified`.
- Todas as rotas exigem login (o site inteiro é para membros). Se um dia o perfil ficar público para visitantes, é `[AllowAnonymous]` nestas duas actions e nada mais.

## Rate limit

| Política | Limite | Onde |
|---|---|---|
| `profile-upload` | 20/h por conta | `POST avatar`, `POST banner` (processar imagem custa CPU) |
| `user-search` | 60/min por conta | `GET /api/users/search` (debounce de 350 ms no front já segura bem abaixo disso) |

## Eventos de log (contexto `Profiles`)

| Evento | Nível | Quando |
|---|---|---|
| `AVATAR_UPDATED` / `AVATAR_REMOVED` | Info | `UserId`, `Key` |
| `BANNER_UPDATED` / `BANNER_REMOVED` | Info | `UserId`, `Key` |

Atualização de texto do perfil não loga: a auditoria global (`auditoria.md`) já registra quem mudou o quê quando o ator é da equipe, e a pessoa editando o próprio perfil não é evento operacional.

## Roteiro de verificação

Com `docker compose up -d` e a API no ar, logado como uma conta confirmada:

1. `PUT /api/profile` com nome, bio, `city: 0`, `genres: [1, 4]`, `instrument: "Guitarra"` e Instagram `https://instagram.com/x` → 200 `PROFILE_UPDATED` com os campos; `GET /api/profile` devolve o mesmo.
2. Instagram `https://evil.com/x` → 400 `INVALID_SOCIAL_LINK`; `genres: [99]` → `INVALID_GENRE`; bio com 501 caracteres → `INVALID_BIO`; `city: 42` → `INVALID_CITY`.
3. `POST /api/profile/avatar` com um JPEG de celular (com GPS) → `AVATAR_UPDATED`; `avatarUrl` abre no navegador, é WebP 512×512 e não tem EXIF. Arquivo `.txt` renomeado `.png` → `IMAGE_UNSUPPORTED`; PNG de 64×64 → `IMAGE_TOO_SMALL`; arquivo de 6 MB → `IMAGE_TOO_LARGE`.
4. Trocar o avatar → a URL antiga passa a responder 404 no LocalStack. `DELETE /api/profile/avatar` → `avatarUrl: null` e o objeto some.
5. `POST /api/profile/banner` com uma foto larga e `position=30` → `BANNER_UPDATED`, `bannerPosition: 30`; `PATCH /api/profile/banner-position {position: 70}` → mesma `bannerUrl`, posição 70; `{position: 120}` → `INVALID_BANNER_POSITION`.
6. `GET /api/users/{nickname}` → dados públicos, sem `email`; nickname inexistente → 404 `PROFILE_NOT_FOUND`.
7. `GET /api/users/search?search=c` → página vazia; `?search=cai` → acha por nome e por nickname; buscar pelo e-mail completo de alguém → nada.
8. 21 uploads de avatar na mesma hora → o 21º toma 429 `TOO_MANY_REQUESTS`.
