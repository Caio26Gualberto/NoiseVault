# Ideias — próximos módulos do Noise Vault

Backlog de produto levantado em 14/10/2026, para trazer a comunidade do grupo do Facebook (pouco mais de 4 mil membros, muita gente das antigas compartilhando vídeos e fotos antigas, e gente nova) e para movimentar as bandas locais. **Nada daqui é decisão ainda.** Cada ideia é lapidada antes de virar módulo e, quando virar, ganha a sua rule em `api/.claude/rules/` e `frontend/.claude/rules/`, e uma linha na ordem de `api/.claude/rules/dominio.md`. Aí a ideia sai deste arquivo.

O que já virou módulo saiu daqui em 20/10/2026: prévia ao compartilhar (1) e no link copiado da barra (1.b, o front em S3 + CloudFront, `api/.claude/rules/hospedagem.md`), envio da comunidade para o mural (2), marcações (3), login social (6), PWA parte 1 (7), Ensaio (8a), sessão por cookie, discografia (9), seguir banda (10) e SEO (16). As rules desses módulos citam os números antigos, por isso a numeração de quem ficou não mudou.

## A tese

As pessoas não vão sair do grupo do Facebook por uma lista de recursos. Elas vêm quando o Noise Vault faz bem algo que o grupo faz mal, e quando cada coisa compartilhada no grupo leva de volta para cá.

O Facebook é ótimo para **postar** e péssimo para **guardar**: em uma semana a foto de 1988 some no feed, ninguém acha de novo, e quem aparece nela nunca fica sabendo. O Noise Vault é o arquivo organizado (por ano, show, banda, casa) e o lugar onde a cena se encontra de novo.

## A ponte com o grupo do Facebook

### 3.b Pedir remoção da foto inteira

- Hoje a pessoa remove a própria marcação (`marcacoes.md`), mas não a foto em que aparece.
- Um "pedir remoção" no registro do mural e no post, que vai para a curadoria com o motivo. É a resposta ao direito de imagem ("Fora do código" abaixo).

## Fazer voltar

### 4. "Neste dia"

- No topo do feed: "Há 35 anos, neste dia, rolava este show no ABC", com os registros do mural da mesma data (dia e mês) em anos anteriores.
- Só os registros com dia e mês preenchidos entram; os que têm só o ano ficam de fora.

### 5. Resumo semanal por e-mail

- Os shows do fim de semana, os registros novos no mural e as bandas novas.
- Gente mais velha lê e-mail: é o lembrete que faz voltar.
- **Apoio**: o envio de e-mail já existe (`email.md`). Faltam a preferência em Minha conta (como a do convite de banda) e um job semanal no Hangfire.

### 7b. Notificação no celular (Web Push)

- O sino avisa em tempo real só com o site aberto. Com o app instalado, dá para mandar a notificação do sistema ("Fulano marcou você numa foto de 1991") mesmo com ele fechado.
- **Android** funciona bem; **iPhone** só a partir do iOS 16.4 e só com o app instalado na tela inicial.
- **Precisa de backend**: chaves VAPID, a inscrição de cada aparelho guardada por pessoa, o envio por job do Hangfire ao lado do SignalR e uma preferência em Minha conta ("receber no celular").
- **Pontos a decidir**: quais tipos de notificação vão para o celular (todos ou só menção, marcação e convite), e se agrupa como o sino.

## Para as bandas locais

### 8a.2 O que ficou do Ensaio

- Vídeo na ficha de músico.
- "Seguir vaga": avisar quando abrir vaga do meu instrumento.
- O mapa de quem toca perto.

### 8b. Mercado (venda e troca)

- Em **`mercado.noisevault.net`**, como o Ensaio: moldura própria, mesma marca, mesmo build.
- Preço, estado, fotos obrigatórias, reservado e vendido, validade de 30 dias, denúncia, dicas contra golpe, sem pagamento pelo site.
- Talvez serviços (aula, estúdio, gravação, foto) entrem como um tipo.
- **Ponto a decidir**: o contato da venda pede uma decisão sobre mensagem (link externo, conversa por anúncio ou mensagem direta geral).

### 9. Página da banda como press kit

- Contato para show.
- "Chamar para tocar": mensagem para quem é responsável pela banda.
- A casa de show acha a banda, e a banda acha show.

### 10. Lançamentos

- Um tipo de post "lançamento" (EP, clipe, single) que aparece em destaque e notifica quem segue a banda (`BandFollower` já existe).

### 11. Casas de show como página própria

- Hoje o local do evento e do mural é texto livre (`Venue`).
- Como entidade: cada casa tem a agenda dela, o histórico no mural (inclusive as casas que fecharam) e quem tocou lá.
- Liga eventos e mural de um jeito que nenhum grupo de Facebook consegue.
- **Pontos a decidir**: quem cadastra a casa, como juntar os textos livres antigos ("Porão", "Porão do Rock") numa casa só.

### 12. Banda da semana

- Destaque fixo no feed, escolhido pela curadoria.
- Quando o podcast vier, vira a pauta do episódio.

## Para centralizar a cena

### 13. Fitas demo e áudio no mural

- O mural aceita foto e vídeo, mas fita demo é **áudio**. Um player de áudio para demos digitalizadas é bem a cara do projeto.
- **Apoio**: o pipeline de mídia (`midia-e-video.md`) já converte vídeo; o áudio seria um terceiro tipo, com o seu próprio limite e conversão.

### 14. Histórias

- Um registro do mural que é só texto (um depoimento, "como era o rolê no Porão em 89"), com fotos opcionais.
- Os comentários do mural já contam histórias; isso traz elas para a frente.

### 15. Podcast ligado ao resto

- A aba de podcast já existe como página estática. Quando o podcast começar, cada episódio aponta para as bandas, eventos e registros citados, e a página da banda mostra "apareceu no podcast".
- Assim o podcast alimenta o site, em vez de ficar isolado numa aba.

## Ser encontrado

**SEO** é aparecer no Google quando alguém pesquisa "show de metal em Santo André" ou o nome de uma banda do ABC. **GEO** (Generative Engine Optimization) é ser lido e citado pelas IAs (ChatGPT, Gemini, a "Visão geral" do Google, Perplexity). O item 16 (SEO) virou módulo em 20/10/2026: banda, agenda, evento e mural abertos sem login, HTML com JSON-LD para buscadores e IAs, sitemap, `robots.txt` e `llms.txt` (`api/.claude/rules/seo.md`, `frontend/.claude/rules/visitante.md`). A base do GEO veio junto (robôs de IA liberados, dados estruturados, `llms.txt`, Bing), e a página da banda abre com uma frase corrida. O que sobrou:

### 16.b Lista de bandas

- Não existe `/bandas`: a banda só é achada pelo line-up, pelo mural ou pelo perfil. Uma lista paginada, aberta e com filtro de cidade e estilo, é uma página a mais para o buscador e o começo das páginas por cidade (17).

### 17. GEO

- **Frase corrida no evento e no registro do mural**: a banda já abre com uma frase que a IA cita (`api/.claude/rules/seo.md`); o evento e o registro ainda abrem com a linha de dados separada por " · ". Fazer se o Search Console mostrar busca por shows e memórias.
- **Páginas por cidade e por estilo**: "Shows em São Bernardo", "Bandas de hardcore do ABC". Ajudam o SEO local e o GEO ao mesmo tempo. Dependem do 16.b para as bandas.
- **Ser citado fora**: casas de show, bandas e páginas da cena linkando para o Noise Vault. Pesa para o Google e para as IAs.

## Fora do código, mas pesa tanto quanto

- **Não lançar vazio.** Antes de chamar o grupo, publicar 30 a 50 registros fortes no mural, escolhidos entre o que já circula no grupo, **sempre com permissão de quem postou**. Quem chega precisa encontrar algo.
- **Conversar com os admins do grupo.** Apresentar como "o arquivo do grupo", não como concorrente. Postar no grupo um registro do mural com a prévia bonita, uma vez por semana, é a melhor divulgação possível.
- **Direito de imagem.** Foto antiga tem gente que não pediu para estar ali. O "pedir remoção" (3.b) evita problema e mostra cuidado.
- **Os primeiros 100 importam mais que os 4 mil.** Escolher 10 ou 15 pessoas ativas do grupo e as bandas que mais postam, chamar cada uma pessoalmente, e dar o papel de curador do mural para quem quiser. As permissões `Archive.*` já permitem isso sem código.

## Ordem sugerida

1. "Neste dia" (4) e resumo semanal (5).
2. Search Console e Bing (`DEPLOY.md`, passo 15), e depois a lista de bandas (16.b).
3. GEO (17), quando o Search Console mostrar o que as pessoas pesquisam.
4. Pedir remoção da foto (3.b), antes de divulgar forte no grupo.
5. Bandas: lançamentos (10), press kit (9), o que ficou do Ensaio (8a.2), mercado (8b).
6. Casas de show (11), áudio (13), histórias (14), banda da semana (12), Web Push (7b) e podcast (15), conforme o uso pedir.
