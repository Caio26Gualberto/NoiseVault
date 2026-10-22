---
paths:
  - "src/pages/noticias/**"
  - "src/i18n/pt-BR/noticias.json"
---

# Decisões — Notícias

Pedido do usuário em 21/10/2026: uma página com as notícias do dia de rock e metal, tiradas do Whiplash.Net, mostrando só a foto, o título, a data e o autor. O clique leva à matéria no site deles. A coleta, o modelo e por que só metadados estão em `../api/.claude/rules/noticias.md`.

## A página `/noticias`

- **Pública** (`public: true` no `nav-config`, `visitante.md`): é leitura de conteúdo de fora, e o visitante vê o mesmo que quem tem conta.
- **No topo, o carrossel "Hoje"** (`components/news-carousel.tsx`) com `GET /api/news?scope=today` (até 50, uma página só). É uma faixa horizontal com CSS `scroll-snap`, sem biblioteca de carrossel: o projeto não tinha nenhuma, e o que a página precisa (deslizar no celular e passar com as setas no computador) a faixa faz sozinha. As setas são `AppIconButton` e só aparecem a partir de `md`, porque no celular quem passa é o dedo. Cada slide mostra a foto grande com o título e o "autor · hora" por cima, sobre `scrim` (`design-system.md`). O slide ocupa 85% da largura no celular, para o próximo aparecer na borda e a pessoa entender que dá para deslizar. Sem notícia de hoje, o carrossel não aparece.
- **Embaixo, a lista por dia**: `GET /api/news?scope=earlier`, com rolagem infinita (`AppPagination mode="infinite"`) somando as páginas sem repetir id, como no feed (`feed.md`). As notícias são agrupadas pelo dia local da publicação, com o título do grupo vindo de `formatWeekday` ("terça-feira, 20 de outubro"), em grade de 1, 2 ou 3 colunas (`components/news-card.tsx`).
- **O card inteiro é um link** para a matéria, com `target="_blank"` e `rel="noopener noreferrer"`. O `aria-label` avisa que abre o Whiplash.Net em outra aba. Não existe detalhe de notícia dentro do Noise Vault: a matéria é deles.
- **A foto é a URL deles** (`imageUrl`), pelo `AppCoverImage` com `poster={false}`. A foto de matéria é sempre deitada, então não vale a detecção de cartaz. Sem foto, o card mostra o ícone de jornal sobre `bg-muted`.
- **Atribuição fixa no rodapé**: "Fonte: Whiplash.Net", com link, e a frase dizendo que aqui só aparecem o título, a foto, o autor e o link. A atribuição é parte do acordo de mostrar manchete de outro site, então não sai.
- O título da aba é "Notícias · Noise Vault" (`useDocumentTitle`).

## No menu

- Item "Notícias" na seção Comunidade, logo depois de Perfil, com o ícone `RssIcon` (o `NewspaperIcon` já é do Feed).
- Leva `mobile: true`, e a posição na lista é o que decide a bottom-nav (`shell-e-navegacao.md`). Com sessão, os quatro primeiros `mobile` são Feed, Mural, Eventos e Perfil, e Notícias vai para o "Mais". O visitante não tem o "Mais" (no lugar dele fica o "Entrar") nem o Perfil, então a barra dele vira Mural, Eventos, Notícias e Entrar. Sem o `mobile`, o visitante no celular não teria como chegar à página.

## Roteiro de verificação

Com a API no ar (em dev, o `DevSeed` grava seis manchetes de exemplo):

1. Sem login, `http://localhost:5173/noticias` a 390px: a bottom-nav mostra Mural, Eventos, Notícias (ativo) e Entrar. O carrossel desliza com o dedo e para no começo de cada slide, e o próximo slide aparece na borda.
2. A 1440px: as setas do carrossel aparecem, ficam desligadas nas pontas e passam quase uma tela por clique. Embaixo, os grupos por dia em 3 colunas.
3. Clicar num card ou num slide abre a matéria do Whiplash.Net em outra aba.
4. Com a tabela vazia (ou a API fora), aparece o estado vazio, sem quebrar a página, e o rodapé da fonte continua.
5. Logado, a bottom-nav continua Feed, Mural, Eventos, Perfil e Mais, com Notícias dentro do "Mais".
