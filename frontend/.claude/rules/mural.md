---
paths:
  - "src/pages/mural/**"
  - "src/pages/feed/components/archive-embed.tsx"
  - "src/pages/admin/mural/**"
---

# Decisões — Mural das antigas no frontend

Rule irmã: `../api/.claude/rules/mural.md`. A regra é do backend; a tela só reflete. Codes em `src/api/api-codes.ts` + `i18n/pt-BR/api.json`.

## Telas

| Caminho | O que mostra | Quem |
|---|---|---|
| `/mural` | linha do tempo interativa, busca, filtros de tipo, cidade e ordem, e a grade de registros | todos |
| `/mural/:id` | data na precisão gravada, título, local, bandas, galeria com legendas, "Eu estava lá", curtir, a história e os comentários ("Histórias") | todos |
| `/mural/novo`, `/mural/:id/editar` | formulário com data aproximada, bandas e o gerenciador de mídias | `Archive.Create` (`permission` no `nav-config`) / o `canEdit` da API (curadoria, ou quem enviou com o envio pendente; sem ele, volta ao detalhe) |
| `/mural/enviar` | o mesmo formulário em modo envio (`mode="submission"`), com o texto da curadoria e "Enviar para a curadoria"; leva a `/mural/envios` | qualquer pessoa |
| `/mural/envios` | "Meus envios": capa, status, data e motivo da recusa; editar e retirar o pendente, apagar o recusado | quem enviou |
| `/admin/mural` | fila da curadoria: abas Na fila, Publicados e Recusados com contagem, e busca; cada linha abre `/mural/:id` | `Archive.Review` |
| `/bandas/:slug` | seção "Das antigas" com os registros da banda; some quando não há nenhum | todos |
| `/feed` | `ArchiveEmbed` no post automático ("publicou no mural das antigas") | todos |

- O cabeçalho do mural mostra "Meus envios" para todo mundo. Ao lado, "Novo registro" para quem tem `archiveCreate`, ou **"Enviar para o mural"** para os outros. O mural vazio (sem filtro) ganha a mesma ação.
- O menu "…" do detalhe usa `canEdit`/`canDelete` da API, mais "Retirar envio" para quem enviou o pendente ou o recusado.
- Os caminhos (`submitArchivePath`, `mySubmissionsPath`, `adminArchivePath`) e o status (`ArchiveEntryStatus`, com rótulo e tom em `Record` exaustivo) moram em `pages/mural/archive-status.ts`.
  - Tons: Aguardando `warning`, Publicado `success`, Não publicado `neutral`.
  - Vermelho fica de fora, como no suporte.
- O item "Mural das antigas" (ícone de fita cassete) entrou no lugar de Vídeos, na sidebar e na bottom-nav. Fotos, Vídeos e Moderação saíram, com as pastas, os mocks e as chaves de i18n.

## Linha do tempo (`components/timeline.tsx`)

- Uma barra por ano, do primeiro registro até o último. A altura é proporcional à quantidade, e um ano vazio vira um traço desabilitado. Os anos ficam agrupados por década, com o rótulo "80s".
- **Interação**:
  - passar o mouse mostra "1987 · 12 registros" no cabeçalho, com `aria-live`;
  - clicar seleciona o ano: a barra fica `primary` com brilho, e a grade refaz a busca com a entrada de página;
  - clicar de novo desseleciona;
  - clicar na década seleciona a década inteira (fundo `primary/10`);
  - "Todos" limpa.
- Com o foco na faixa, ←/→ andam entre os anos que têm registro. No celular, a faixa rola na horizontal e o ano escolhido vai para o centro (`scrollIntoView`).
- O brilho do ano escolhido usa `shadow-primary/50`, um token, nunca uma cor solta.

## Grade e card

- Capa 4:3 com `sepia` leve, que sai no hover (é foto antiga, afinal); selo de data ("ago / 1987" ou só "1987"); contadores de fotos e vídeos; título, local e bandas.
- No rodapé ficam "Eu estava lá" e as curtidas, em `primary` quando são minhas.
- A data sai em `archive-date.ts` (`formatArchiveDate`, `archiveDateBadge`), sempre em UTC, porque é um dia do calendário e não um instante.

## Detalhe

- **Galeria**: grade quadrada com a legenda subindo no hover ou no foco. Clicar abre o `AppMediaViewer` naquela posição, e o viewer agora mostra a **legenda** embaixo da mídia. Vídeo toca no `AppVideoPlayer` com as qualidades.
- **Mídia processando**: aparece com spinner. A página consulta de novo a cada 5 s enquanto houver alguma.
- **"Eu estava lá"**:
  - um botão de destaque, mais a fileira de até 5 avatares com "Fulano, Ciclano e mais 12 estavam lá";
  - clicar abre a lista paginada (`AttendeesDialog`).
- **Comentários** se chamam "Histórias". O vazio convida: "Estava lá ou reconhece alguém? Conte aqui."

## Formulário

- **Modo envio** (`/mural/enviar`): mesmos campos, mas a mídia vai para `/api/archive/media/*`.
  - O `MediaManager` recebe `target`: `curation` para quem tem `archiveCreate`, `submission` para os outros (`archiveUploadPaths` no service).
  - Na edição, o formulário usa o `PUT` de envio quando a pessoa é quem enviou, o envio está pendente e ela não tem `archiveEdit`; senão, o da curadoria.
- **Data**: ano (obrigatório, 1950 até hoje), mês ("Não sei" por padrão) e dia (vazio). O zod espelha a API: dia exige mês e tem que existir no mês.
- **Cidade**: `AppCityField` com busca do IBGE, "Fora do Brasil" e "Não sei" para limpar. O filtro da grade é um `AppCitySelect` com "Todas as cidades".
- **Bandas**: o `LineupEditor` dos eventos, com `legend` e `hint` próprios, importado pelo caminho da página dona (`pages/eventos/components`).
- **Mídias** (`components/media-manager.tsx`, com o estado em `managed-media.ts`):
  - "Adicionar fotos" (várias) e "Adicionar vídeos" (vários); o envio começa ao escolher;
  - foto vai para `POST /api/admin/archive/media/photos`; vídeo, para `uploadVideo(..., archiveVideoStartPath)`;
  - os limites locais (`archivePhotoMaxBytes`, `archiveVideoLimits` em `lib/video-file.ts`) recusam cedo.
  - Cada item tem miniatura, progresso, legenda, "Usar como capa", ↑/↓ e remover.
  - Remover uma mídia nova apaga no servidor; remover uma salva só tira da lista, e a API apaga ao salvar.
  - "Publicar" espera os envios e exige pelo menos uma mídia pronta.

## Envio da comunidade e revisão

- **Detalhe do envio** (`/mural/:id` fora de `Published`):
  - faixa "Aguardando a curadoria", com um texto para quem enviou e outro para a curadoria, ou "Este envio não foi publicado" com o motivo;
  - "Eu estava lá", curtir e as histórias somem. O hook nem busca os comentários: a API responderia `ARCHIVE_ENTRY_NOT_FOUND` e sairia um toast;
  - "Voltar" leva a "Meus envios" para quem enviou e à fila para a curadoria.
- **Painel da curadoria** (`components/review-panel.tsx`, com `canReview`):
  - "Publicar no mural", "Editar antes" (com `canEdit`) e "Recusar", que abre um `AppDialog` com o motivo (zod: 3 a 500);
  - a ação troca o registro pelo DTO que volta. Publicado, o detalhe ganha as interações e as histórias.
- **Crédito**: "Acervo de @apelido" quando `isSubmission`; "Publicado por" no resto.
- **Fila** (`pages/admin/mural/`, no molde da caixa de entrada do suporte): refaz a busca e a contagem quando a `version` do `NotificationsProvider` sobe, porque cada envio novo chega como notificação.
- **Notificações**: envio recebido e publicado levam a `/mural/:id`; recusado leva a `/mural/envios`.

## Roteiro de verificação

Com a API, o ffmpeg e o Vite no ar. O script `mural-ui.mjs` vive fora do repositório.

1. **Admin (1440px).** `/mural` vazio mostra o convite. "Novo registro": ano 1987, agosto, "Ginásio de Santo André", banda cadastrada + "Sepultura", 3 fotos com legenda, 1 vídeo e uma capa escolhida.
   - Publicar leva ao detalhe com "agosto de 1987".
   - O feed mostra o card "Das antigas".
2. **Linha do tempo.** Aparece a barra de 1987; clicar filtra a grade; "80s" seleciona a década; "Todos" limpa.
3. **Detalhe.** O hover mostra as legendas, e o viewer mostra a legenda embaixo da foto. O vídeo toca, com a engrenagem de qualidade.
4. **Fã (390px).** Não vê "Novo registro". A bottom-nav mostra "Mural". Marca "Eu estava lá" (os avatares aparecem) e conta uma história.
5. **Editar.** Trocar a capa e a ordem, tirar uma foto e salvar: o detalhe reflete.

### Envio da comunidade

O script `mural-submissions-ui.mjs` (Playwright + Brave) registra uma fã pelo Mailpit.

1. **Fã (390).** O mural mostra "Enviar para o mural" e "Meus envios", sem "Novo registro".
   - O formulário de envio não tem rolagem horizontal e leva a "Meus envios", com "Aguardando a curadoria".
   - No detalhe: a faixa, sem "Eu estava lá", sem histórias e sem o painel.
2. **Curadoria (1440).** "Envios do mural" aparece na Administração, e a aba "Na fila" tem contagem.
   - Recusar sem motivo mostra o erro no campo; com motivo, o detalhe mostra "Motivo da curadoria".
   - Em outro envio, "Publicar no mural" troca a faixa por "Eu estava lá" e pelo crédito "Acervo de".
3. **Fã depois.** "Meus envios" mostra "Publicado", e o recusado com o motivo.
   - As duas notificações chegam, e o publicado aparece no mural.
   - `/admin/mural` leva ao `/feed`.

Resultado em 14/10/2026: as 22 checagens passaram.
