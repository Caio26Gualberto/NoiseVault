---
paths:
  - "src/pages/perfil/**"
  - "src/pages/minha-conta/**"
  - "src/layout/top-bar.tsx"
  - "src/layout/people-search.tsx"
  - "src/layout/user-menu.tsx"
  - "src/components/app-profile-hero/**"
  - "src/api/mock-fixtures.ts"
---

# Decisões — Perfil e mídia no front

Rule irmã: `../api/.claude/rules/perfil-e-midia.md` (campos, validação, codes) e `armazenamento.md` (limites de imagem). A regra é do backend; aqui fica como a tela reflete.

## O que é real e o que ainda é mock

| Tela | Fonte |
|---|---|
| `/perfil/:nickname` | `GET /api/users/{nickname}` |
| `/minha-conta` → seção Perfil | `GET /api/profile` (sessão), `PUT /api/profile`, avatar e capa |
| Busca da topbar | `GET /api/users/search` |
| Contadores, bandas e abas de conteúdo no perfil | **não existem**: voltam com os módulos 2 (bandas), 3 (feed) e 5 (galeria) |

Os mocks dos outros módulos ainda precisam de pessoas e bandas de exemplo: elas moram em `src/api/mock-fixtures.ts` (`people`, `bandSummaries`, `avatar()`, `picture()`), andaime que morre com o último `mocks.ts`. Pessoa **não tem selo de verificado** (só banda): `ProfileSummaryDto` não tem `isVerified`.

## Sessão carrega o perfil inteiro

`ProfileDto` da sessão traz `avatarUrl`, `bannerUrl`, `bannerPosition`, `bio`, `city`, `mainInstrument`, `genres` e `socialLinks`. Toda ação do perfil devolve o `ProfileDto` atualizado e a tela chama `replaceUser(dto)` do `AuthProvider` — sem refetch, e o avatar do user-menu troca na hora.

## Edição: um formulário, um `PUT`

A seção Perfil de `/minha-conta` envia tudo de uma vez (nome, bio, cidade, instrumento, gêneros, links). Os schemas zod em `pages/minha-conta/profile-validators.ts` espelham `perfil-e-midia.md` (bio 500, instrumento 60, até 10 gêneros, link `http(s)` no host do serviço). ⚠️ Mudou a regra no backend, muda aqui no mesmo commit.

Cidade e gêneros são `Select` e chips de múltipla escolha alimentados por `pages/perfil/cities.ts` e `genres.ts` (numeração igual à do backend).

## Avatar: escolher, ver, enviar

`avatar-uploader.tsx`: o arquivo escolhido aparece na hora (`URL.createObjectURL`, revogado ao trocar ou desmontar) e só sobe quando a pessoa confirma. Tipo (JPEG/PNG/WebP) e tamanho (5 MB) são checados antes de enviar para a pessoa não esperar o upload para ouvir "não"; o servidor checa de novo de verdade. O recorte final é quadrado e central (feito no servidor), então o preview já é um círculo com `object-cover`.

## Capa: posicionar com a mesma proporção da tela

`banner-editor.tsx`:

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

1. `/minha-conta`: preencher bio, cidade, instrumento, 2 gêneros e Instagram → "Salvar perfil" → toast `PROFILE_UPDATED`; recarregar mantém tudo. Instagram `https://evil.com/x` → erro no campo antes de enviar.
2. Escolher um JPEG como avatar → preview circular → "Salvar avatar" → toast `AVATAR_UPDATED`; o avatar do user-menu troca sem recarregar. Arquivo `.txt` → erro sem chamar a API. "Remover" volta às iniciais.
3. Escolher uma capa larga → arrastar para mostrar outra parte → "Salvar capa" → `/perfil/:nickname` mostra o mesmo corte do preview. Arrastar de novo → "Salvar posição" → muda sem reupload (mesma `bannerUrl`).
4. `/perfil/<nickname>` de outra pessoa → sem "Editar perfil"; `/perfil/naoexiste` → estado vazio, sem toast.
5. Topbar: digitar "a" não busca; "adm" lista o admin; Enter abre o perfil e limpa o campo.
6. Telas mockadas (feed, eventos, bandas, vídeos, fotos, notificações, moderação) continuam abrindo com as fixtures movidas.
7. 390px: `/minha-conta` e `/perfil/:nickname` sem rolagem horizontal; arrastar a capa com toque funciona.
