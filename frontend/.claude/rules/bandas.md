---
paths:
  - "src/pages/bandas/**"
  - "src/pages/notificacoes/components/band-invitations.tsx"
---

# Decisões — Bandas e convites no frontend

Rule irmã: `../api/.claude/rules/bandas.md`. A regra é do backend; a tela só reflete. Codes em `src/api/api-codes.ts` + `i18n/pt-BR/api.json`.

## Telas

| Caminho | Tela | Quem vê |
|---|---|---|
| `/bandas/:slug` | página pública da banda: capa, avatar, descrição, cidade, "Desde {ano}", gêneros, links e integrantes; a seção "Vagas abertas" vem do Ensaio (`ensaio.md`) | qualquer pessoa logada |
| `/bandas/nova` | criar banda (`BandForm` com o campo "seu instrumento na banda") | qualquer pessoa logada; entra pelo botão "Criar banda" no próprio perfil |
| `/bandas/:slug/editar` | avatar e capa, dados, integrantes e convites | só Owner (`myRole === owner`); quem não é Owner é redirecionado para `/bandas/:slug` |
| `/admin/bandas` | fila de pedidos de verificação (ver "Verificação") | `Bands.Verifications.View`; decidir exige `Bands.Verify` |

- As três estão em `hiddenRoutes` no `nav-config`, com `/bandas/nova` antes de `/bandas/:slug`.
- Criar banda leva direto a `/editar`, porque o próximo passo natural é subir imagem e convidar a formação.
- **Sem "Seguir" e sem contadores falsos.** O hero mostra integrantes, publicações (`postCount`, módulo 3) e shows marcados (`upcomingEventCount`, módulo 4). A seção "Shows" (`eventos/components/band-events.tsx`) vem antes de Publicações, seguida de "Das antigas" (`mural/components/band-archive.tsx`), que só aparece quando a banda tem registro no mural. Seguidores e eventos voltam quando existirem, não antes.
- A seção "Publicações" vem depois de Integrantes, com `OwnerPosts` do feed (`feed.md`). Integrante publica como banda pelo composer do `/feed`.
- `BAND_NOT_FOUND` é tratado na tela, como `PROFILE_NOT_FOUND` no perfil:
  - `getBandProfile` chama com `toastError: false`;
  - `useBand` (`hooks/use-band.ts`) devolve `notFound`, que vira `AppEmptyState`;
  - qualquer outro erro passa pelo `feedback.error` e cai no estado `failed`.

## Quem faz o quê na tela

- **Na página pública:** Owner vê "Editar banda"; Member vê "Sair da banda" (com `useAppConfirm`; ao sair, vai para o próprio perfil).
- **Na edição, integrante Member:** "Tornar responsável" e "Remover".
  - Não existe "transferir": passar a posse é promover alguém e depois sair, exatamente como a API (a descrição do card diz isso).
- **Na edição, outro Owner:** só o selo. A API recusa remover Owner.
- **Na edição, a própria linha:** "Sair da banda". O último Owner recebe `BAND_LAST_OWNER` pelo toast global.
- Toda ação de banda devolve o `BandProfileDto` novo e a tela chama `replace(dto)`; sair é a exceção, porque a pessoa perde acesso à tela.

## Verificação

Rule irmã: a seção "Verificação" de `../api/.claude/rules/bandas.md` (15/10/2026).

- **Na página da banda**, só o Owner de banda **não verificada** vê a caixa tracejada abaixo do hero, com três estados lidos de `verification` no `BandProfileDto`: sem pedido ("Pedir verificação"), `pending` ("em análise", sem botão) e `rejected` (o motivo da API e "Pedir de novo"). O botão abre `components/verification-request-dialog.tsx`: mensagem obrigatória (20 a 1000) e WhatsApp opcional; ao enviar, `reload()` da banda. A exigência mínima (avatar, 2 integrantes, um link social) é da API: a tela só explica no texto do diálogo e deixa o toast `BAND_VERIFICATION_REQUIREMENTS` dizer o que falta.
- **Fila** em `/admin/bandas` (`pages/admin/bandas/`, item "Verificação de bandas" na seção Administração, permissão `bandsVerificationsView`), no molde de `/admin/mural`: abas por status com o resumo, busca, lista. Clicar num pedido abre `components/verification-review-dialog.tsx` com o avatar e os links sociais da banda (abrem em outra aba, porque é o que o revisor confere), quem pediu, a mensagem e o telefone. "Aprovar" passa por `useAppConfirm` e avisa que o nome fica reservado; "Recusar" pede o motivo (3 a 500) no próprio diálogo; num pedido aprovado de banda ainda verificada aparece "Tirar o selo". Aprovar, recusar e tirar o selo exigem `bandsVerify`; sem ela a pessoa só lê.
- `band-verification-status.ts` espelha `EBandVerificationStatus` (`pending: 0, approved: 1, rejected: 2`), com tom do badge e `adminBandsPath`.
- As notificações 29 a 32 (`notificacoes.md`): o pedido leva a equipe para `/admin/bandas`; aprovado, recusado e selo removido levam os Owners à página da banda.

## Discografia

Rule irmã: `../api/.claude/rules/discografia.md` (16/10/2026).

- **Na página pública**, `components/band-discography.tsx` entra entre Integrantes e Vagas, busca `GET /api/bands/{slug}/discography` sozinho e some quando não há lançamento. No topo, a faixa em destaque: o embed do Spotify quando a faixa é importada, ou um cartão com o link próprio quando é manual. Depois, um carrossel de capas (`aspect-square overflow-hidden`), **4 por vez** (2 no celular), deslizando com animação ao trocar de página, setas redondas sobrepostas às bordas que somem no fim e pontinhos de página clicáveis (pedido do usuário em 16/10/2026, depois de ver 30 discos do Slayer tomando a página); duas colunas no celular, quatro a partir de `md`. Clicar abre `release-dialog.tsx`: capa, link de origem, lista de faixas com número e duração e **um player por vez**, carregado só ao clicar na faixa (`spotify-embed.tsx`); nunca um iframe por faixa, porque cada embed pesa e o Spotify limita. Faixa manual abre o link próprio; faixa sem nada é só texto.
- **Atribuição**: quando há lançamento importado, o rodapé da seção e do diálogo mostra o logo do Spotify (`SpotifyLogo`, o ícone oficial em SVG inline) com "Dados e capas do Spotify" e o link para o artista. Lançamento manual não mostra Spotify em lugar nenhum: a política deles proíbe misturar os dois na mesma faixa.
- **Na edição** (`/bandas/:slug/editar`), o `AppCard` "Discografia" (`components/discography-manager.tsx`) vem depois dos convites: caixa "Importar do Spotify" (desabilitada sem link de artista ou sem o servidor configurado, com a frase explicando; mostra "Atualizado há X"), lista com ↑/↓ (o `move` por `splice` do `lineup-editor`), olho para esconder, "Trocar capa", "Editar" e "Apagar" só no manual, e a lista de faixas expandida com a estrela de destaque. Reordenar guarda a ordem local e mostra "Salvar ordem" / "Desfazer"; só o salvar chama a API. "Adicionar lançamento" abre `release-form-dialog.tsx` (título, tipo em chips, ano, link, faixas com nome, link e ordem), que também edita o manual.
- `release-kind.ts` espelha `EReleaseKind` (`album: 0, ep: 1, single: 2`) e `EReleaseSource` (`spotify: 0, manual: 1`) e monta as URLs do embed.

## Convite

- **Busca:** a pessoa é escolhida pela busca de pessoas real (`searchPeople` do `perfil-service`, debounce de 350 ms, 2+ caracteres, até 6 resultados, integrantes já filtrados). Escolher alguém trava o campo num cartão com "Trocar". Digitar o nickname inteiro e enviar sem escolher também funciona: a API resolve o nickname e devolve `INVITEE_NOT_FOUND` se não houver ninguém.
- **Pendentes:** a lista aparece abaixo do formulário (`GET /api/bands/{slug}/invitations`, uma página de 20, que é o limite de pendentes da API), com "Revogar" e a data de vencimento.
- **No painel do sino** (`notificacoes.md`): a seção "Convites de banda" (`notificacoes/components/band-invitations.tsx`) vem **da API real** (`GET /api/band-invitations/mine`), acima da lista de notificações (real desde o módulo 6, `notificacoes.md`), que mostra o item do convite só com o estado.
  - A seção some quando não há convite.
  - "Aceitar" leva para a página da banda. "Recusar" refaz a lista.
  - Responder é pelo **id do convite**, não da notificação: a notificação vai ser só projeção do convite (`../api/.claude/rules/dominio.md`).
- ~~O sino do topo ainda conta só as notificações mock~~: desde o módulo 6, o convite é uma notificação `BandInvite` e soma no sino. Aceitar ou recusar marca como lida, e a seção refaz a busca quando a contagem muda.

## Reaproveitamento

- `BandSummaryDto` mora em `bandas-service.ts` (a página dona). Perfil, feed, eventos e notificações importam de lá.
- Os uploaders de avatar e capa viraram globais (`AppAvatarUploader`, `AppBannerEditor`), recebendo callbacks: a conta e a banda usam o mesmo componente. Validação de arquivo em `src/lib/image-file.ts`; textos em `common.json` (`imageUpload.*`).
- O formulário (`components/band-form.tsx` + `band-validators.ts`) é o mesmo na criação e na edição. Os campos repetidos do perfil (cidade, gêneros, links, limite de instrumento) reaproveitam `meu-perfil/profile-validators.ts`.
- `band-invitation-status.ts` espelha `EBandInvitationStatus` (`pending: 0, accepted: 1, declined: 2, revoked: 3`).

## Roteiro de verificação

Com a API, o Docker e o Vite no ar, conta A (admin) e conta B confirmada:

1. **Criar.** A abre o próprio perfil, vê a seção "Bandas" com "Criar banda", cria "Serra Negra" com ano 1991 e "Vocal", e cai em `/bandas/serra-negra/editar` com A como Responsável.
2. **Convidar.** A digita parte do nickname de B no convite, escolhe a pessoa na lista, põe "Guitarra" e envia: toast "Convite enviado." e B aparece em "Convites pendentes" com a data de vencimento.
3. **Aceitar no celular.** B, a 390px, abre o balão do sino, vê "Admin convidou você para tocar Guitarra na Serra Negra." no topo, toca "Aceitar" e vai para `/bandas/serra-negra` como integrante, com "Sair da banda".
4. **Ver como Owner.** A, a 1440px, abre a banda: dois integrantes, "Editar banda" e "2 integrantes" no hero, sem "Seguir".
5. **Não encontrada.** `/bandas/nao-existe` mostra o estado vazio "Banda não encontrada"; B abrindo `/bandas/serra-negra/editar` é redirecionado para a página da banda.
6. **Promover e sair.** A promove B, sai da banda e vai para o próprio perfil. Na página da banda, B passa a ver "Editar banda".
7. **Imagens.** Avatar e capa sobem pela edição e aparecem no hero; o perfil de B lista a banda na seção "Bandas".
