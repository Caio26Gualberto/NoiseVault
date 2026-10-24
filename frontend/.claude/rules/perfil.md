---
paths:
  - "src/pages/perfil/**"
  - "src/pages/minha-conta/**"
  - "src/pages/meu-perfil/**"
  - "src/layout/top-bar.tsx"
  - "src/layout/people-search.tsx"
  - "src/layout/user-menu.tsx"
  - "src/components/app-profile-hero/**"
---

# Decisões — Perfil e mídia no front

Rule irmã: `../api/.claude/rules/perfil-e-midia.md` (campos, validação, codes) e `armazenamento.md` (limites de imagem). A regra é do backend; aqui fica como a tela reflete.

## O que é real e o que ainda é mock

| Tela | Fonte |
|---|---|
| `/perfil/:nickname` | `GET /api/users/{nickname}` |
| `/meu-perfil` | `GET /api/profile` (sessão), `PUT /api/profile`, avatar e capa |
| Busca da topbar | `GET /api/users/search` |
| Bandas no perfil | seção "Bandas e projetos" com os `BandSummaryDto` de `profile.bands` (módulo 2, `bandas.md`), com "Artista solo" embaixo do nome do solo; no próprio perfil aparece "Novo projeto" mesmo sem nenhuma, que abre a escolha entre banda e artista solo (24/10/2026, `bandas.md`) |
| Bandas que sigo | só no próprio perfil, abaixo de "Bandas": `FollowedBands` de `pages/bandas/components/` com `GET /api/bands/following` paginado (`bandas.md`, 16/10/2026) |
| Publicações no perfil | contador "N publicações" no hero (`postCount`) e a seção "Publicações" com `OwnerPosts` do feed (módulo 3, `feed.md`) |
| Selo "Procurando banda" | `isLookingForBand` do `UserProfileDto` acende o selo ao lado da cidade e leva à ficha no Ensaio (`ensaio.md`) |
| Abas de fotos e vídeos de galeria | **não existem**: voltam com o módulo 5 |

~~As fixtures compartilhadas em `src/api/mock-fixtures.ts`~~ morreram com o mock de notificações (módulo 6). Pessoa **não tem selo de verificado** (só banda): `ProfileSummaryDto` não tem `isVerified`.

## Sessão carrega o perfil inteiro

`ProfileDto` da sessão traz `avatarUrl`, `bannerUrl`, `bannerPosition`, `bio`, `city`, `mainInstrument`, `genres` e `socialLinks`. Toda ação do perfil devolve o `ProfileDto` atualizado e a tela chama `replaceUser(dto)` do `AuthProvider` — sem refetch, e o avatar do user-menu troca na hora.

## Meu perfil × Minha conta

Decisão do usuário em 15/10/2026: "perfil é o que as pessoas veem, e minha conta é sensível, hoje tá tudo junto".

- **`/meu-perfil`** (`pages/meu-perfil/`) edita o perfil público: avatar, capa, nome, bio, cidade, instrumento, gêneros e links. A coluna fixa tem a prévia ao vivo do cartão do perfil (capa, avatar, nome, cidade, instrumento, bio e gêneros, lidos do formulário enquanto a pessoa digita) e o "Salvar perfil". O botão "Editar perfil" do perfil e o item do user-menu levam para cá.
- **`/minha-conta`** fica só com o que é da pessoa e não aparece para ninguém: e-mail e apelido (só leitura), troca de senha, notificações por e-mail e a localização aproximada do Ensaio (`ensaio.md`). Tem "Editar perfil" e "Ver perfil" para quem chegou procurando o perfil.
- O service continua um só (`pages/minha-conta/services/conta-service.ts`): "Meu perfil" importa do caminho da conta, como manda a regra de service não promovido. Os validadores do perfil foram para `pages/meu-perfil/profile-validators.ts`, porque são daquela tela; a banda importa de lá.
- O `/bem-vindo` continua liberando só a `/minha-conta`.

## Edição: um formulário, um `PUT`

`/meu-perfil` envia tudo de uma vez (nome, bio, cidade, instrumento, gêneros, links). Os schemas zod em `pages/meu-perfil/profile-validators.ts` espelham `perfil-e-midia.md` (bio 500, instrumento 60, até 10 gêneros, link `http(s)` no host do serviço). ⚠️ Mudou a regra no backend, muda aqui no mesmo commit.

A cidade é o `AppCityField` (busca no `GET /api/cities` por `pages/perfil/services/cidades-service.ts`, mais "Fora do Brasil"), e os gêneros são chips de múltipla escolha de `genres.ts` (numeração igual à do backend). Tipo, conversão para o formulário (`toCityValue`, `toCityInput`), schema zod e `formatCity` moram em `pages/perfil/city.ts`.

## Avatar: escolher, ver, enviar

`AppAvatarUploader` (`components/app-avatar-uploader`, global desde o módulo 2: a banda usa o mesmo componente; recebe `onUpload`/`onRemove` e quem chama troca o DTO): o arquivo escolhido aparece na hora (`URL.createObjectURL`, revogado ao trocar ou desmontar) e só sobe quando a pessoa confirma. Tipo (JPEG/PNG/WebP) e tamanho (5 MB) são checados antes de enviar para a pessoa não esperar o upload para ouvir "não"; o servidor checa de novo de verdade. O recorte final é quadrado e central (feito no servidor), então o preview já é um círculo com `object-cover`.

## Capa: posicionar com a mesma proporção da tela

`AppBannerEditor` (`components/app-banner-editor`, global pelo mesmo motivo; `onUpload`, `onPosition` e `onRemove` devolvem o DTO salvo, que traz a `bannerPosition` de volta). Validação de arquivo em `src/lib/image-file.ts`, textos em `common.json` (`imageUpload.*`):

- O preview usa **a mesma proporção do `AppProfileHero`** (`aspect-[4/1]` a partir de `md`, `3/1` abaixo). No legado o preview tinha outra altura e o corte salvo não batia com o perfil.
- Arrastar **move a imagem junto com o dedo/mouse** (pointer events: mouse, toque e caneta). Arrastar para baixo mostra a parte de cima da imagem — o legado fazia o contrário e parecia invertido.
- Teclado: o controle é focável, ↑/↓ movem 2%, `Home`/`End` vão a 0/100. `aria-valuenow` com a posição.
- Capa nova: escolher → posicionar → "Salvar capa" envia arquivo + posição. Capa existente: só arrastar → "Salvar posição" faz `PATCH banner-position` sem reenviar.
- Tamanho máximo 10 MB, largura mínima 800px (checada pelo `naturalWidth` antes de enviar).

## Perfil público

`/perfil/:nickname` usa `AppProfileHero` com a posição salva (`object-position: center Y%`). Bio, cidade, instrumento, gêneros e links aparecem só se preenchidos. Perfil inexistente (`PROFILE_NOT_FOUND`) mostra `AppEmptyState` em vez de toast — é a exceção de "página não trata erro" registrada aqui: o service chama com `toastError: false` e a tela decide pelo `code`: `PROFILE_NOT_FOUND` vira o estado vazio; qualquer outro erro é repassado a `feedback.error` (o mesmo sink do `base-service`, então o toast continua único) e a tela mostra "Não foi possível abrir o perfil". O próprio dono vê "Editar perfil" levando a `/minha-conta`.

## Busca da topbar

`layout/people-search.tsx`: campo real, `useDebouncedValue` (350 ms), só busca com 2+ caracteres, mostra até 8 resultados (avatar, nome, `@nickname`) num popover; ↑/↓ + Enter navegam, Esc fecha, clique ou Enter vai para `/perfil/:nickname` e limpa o campo. Respostas antigas que chegam depois de uma mais nova são descartadas (contador de requisição). Em mobile a busca fica escondida nesta entrega — entra num `Sheet` quando houver busca de bandas também.

## Roteiro de verificação

Com API, LocalStack e Vite no ar:

1. `/meu-perfil`: preencher bio, cidade, instrumento, 2 gêneros e Instagram → "Salvar perfil" → toast `PROFILE_UPDATED`; recarregar mantém tudo. Instagram `https://evil.com/x` → erro no campo antes de enviar.
2. Escolher um JPEG como avatar → preview circular → "Salvar avatar" → toast `AVATAR_UPDATED`; o avatar do user-menu troca sem recarregar. Arquivo `.txt` → erro sem chamar a API. "Remover" volta às iniciais.
3. Escolher uma capa larga → arrastar para mostrar outra parte → "Salvar capa" → `/perfil/:nickname` mostra o mesmo corte do preview. Arrastar de novo → "Salvar posição" → muda sem reupload (mesma `bannerUrl`).
4. `/perfil/<nickname>` de outra pessoa → sem "Editar perfil"; `/perfil/naoexiste` → estado vazio, sem toast.
5. Topbar: digitar "a" não busca; "adm" lista o admin; Enter abre o perfil e limpa o campo.
6. ~~Telas mockadas abrem com as fixtures movidas~~: não sobrou tela mockada que use fixture de pessoa.
7. 390px: `/meu-perfil`, `/minha-conta` e `/perfil/:nickname` sem rolagem horizontal; arrastar a capa com toque funciona.
