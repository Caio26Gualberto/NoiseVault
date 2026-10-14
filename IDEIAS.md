# Ideias — próximos módulos do Noise Vault

Backlog de produto levantado em 14/10/2026, para trazer a comunidade do grupo do Facebook (pouco mais de 4 mil membros, muita gente das antigas compartilhando vídeos e fotos antigas, e gente nova) e para movimentar as bandas locais. **Nada daqui é decisão ainda.** Cada ideia é lapidada antes de virar módulo e, quando virar, ganha a sua rule em `api/.claude/rules/` e `frontend/.claude/rules/`, e uma linha na ordem de `api/.claude/rules/dominio.md`. Aí a ideia sai deste arquivo ou fica ~~tachada~~ com o link para a rule.

## A tese

As pessoas não vão sair do grupo do Facebook por uma lista de recursos. Elas vêm quando o Noise Vault faz bem algo que o grupo faz mal, e quando cada coisa compartilhada no grupo leva de volta para cá.

O Facebook é ótimo para **postar** e péssimo para **guardar**: em uma semana a foto de 1988 some no feed, ninguém acha de novo, e quem aparece nela nunca fica sabendo. O Noise Vault é o arquivo organizado (por ano, show, banda, casa) e o lugar onde a cena se encontra de novo.

## Prioridade 1 — a ponte com o grupo do Facebook

As três primeiras atacam direto o motivo de o pessoal estar no grupo hoje (fotos e vídeos antigos) e transformam cada post no Facebook num convite para cá.

### 1. Prévia bonita ao compartilhar o link

- **Problema**: o front é SPA; colar um link do Noise Vault no Facebook ou no WhatsApp não mostra foto nem título, porque o robô não executa o JavaScript.
- **Ideia**: tags Open Graph (`og:title`, `og:description`, `og:image`) por página. A API responde aos robôs (pelo `User-Agent` ou por uma rota de compartilhamento) com uma página leve só com as tags e o redirecionamento.
- **Onde vale**: registro do mural, evento, banda, perfil, post.
- **Depende da hospedagem**: com o front no Amplify, só a rota de compartilhamento (`/s/...`) funciona; com o front em S3 + CloudFront, uma CloudFront Function cobre também o link copiado da barra. Ver "Front e a prévia de links" em `api/.claude/rules/hospedagem.md`.
- **Por que primeiro**: maior retorno pelo menor custo. Cada post no grupo vira um cartão com foto e título que traz gente de volta.

### ~~2. A comunidade envia para o mural, a curadoria aprova~~

Virou módulo em 14/10/2026: ver "Envio da comunidade" em `api/.claude/rules/mural.md` e em `frontend/.claude/rules/mural.md`. Decidido: a curadoria edita e publica ou recusa com motivo; o registro sai em nome de quem enviou ("Acervo de"); os limites de mídia são os do arquivo; até 5 envios pendentes por pessoa; permissão `Archive.Review`.

### 3. Marcar pessoas nas fotos antigas ("quem é esse?")

- **Ideia**: em cada foto do mural, marcar quem aparece, com conta (notifica) ou só com o nome escrito. A pessoa marcada confirma ou remove a marcação.
- **Por que**: é o que mais traz gente das antigas de volta. "Você foi marcado numa foto de 1991" reencontra amigos.
- **Apoio**: o módulo de notificações já está pronto; falta a entidade da marcação e um `ENotificationKind` novo.
- **Pontos a decidir**: marcar em posição na foto ou só lista de nomes, quem pode marcar, moderação de marcação errada ou ofensiva.

## Prioridade 2 — fazer voltar

### 4. "Neste dia"

- No topo do feed: "Há 35 anos, neste dia, rolava este show no ABC", com os registros do mural da mesma data (dia e mês) em anos anteriores.
- Só os registros com dia e mês preenchidos entram; os que têm só o ano ficam de fora.

### 5. Resumo semanal por e-mail

- Os shows do fim de semana, os registros novos no mural e as bandas novas.
- Gente mais velha lê e-mail: é o lembrete que faz voltar.
- **Apoio**: o envio de e-mail já existe (`email.md`). Faltam a preferência em Minha conta (como a do convite de banda) e um job semanal no Hangfire.

### 6. Entrar com Google ou Facebook

- Ficou **fora do escopo** no início (`CLAUDE.md`), mas para esse público pode decidir se a pessoa entra ou desiste no cadastro. Senha nova e confirmação de e-mail afastam.
- Entrar com Facebook especialmente, já que a comunidade vem de lá.
- Reavaliar antes da divulgação no grupo. Quando entrar, entra no `AuthAppService` e no `IAuthenticateService`, não num serviço paralelo (`api/CLAUDE.md`).

### 7. Instalável no celular (PWA)

- "Adicionar à tela inicial", com ícone e abertura em tela cheia.
- Não é o app nativo que ficou de fora; custa pouco e ajuda quem não tem costume de abrir site.

## Para as bandas locais

### 8. Classificados de músico

- "Procuro baterista", "toco baixo e procuro banda", "vendo amplificador".
- É o que mais faz músico entrar todo dia. O perfil já tem instrumento e cidade.
- **Pontos a decidir**: tipos de anúncio, validade (anúncio vence sozinho), contato (pela plataforma ou link externo), se venda de equipamento entra (a loja ficou fora do escopo; classificado entre pessoas é outra coisa).

### 9. Página da banda como press kit

- Player do Spotify, Bandcamp ou YouTube embutido, discografia, contato para show.
- "Chamar para tocar": mensagem para quem é responsável pela banda.
- A casa de show acha a banda, e a banda acha show.

### 10. Lançamentos e "seguir banda"

- Um tipo de post "lançamento" (EP, clipe, single) que aparece em destaque e notifica quem segue a banda.
- Pede o **seguir banda**, que hoje não existe e ajudaria o feed inteiro (hoje ele é único e cronológico, sem recorte por quem você segue).

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

## Fora do código, mas pesa tanto quanto

- **Não lançar vazio.** Antes de chamar o grupo, publicar 30 a 50 registros fortes no mural, escolhidos entre o que já circula no grupo, **sempre com permissão de quem postou**. Quem chega precisa encontrar algo.
- **Conversar com os admins do grupo.** Apresentar como "o arquivo do grupo", não como concorrente. Postar no grupo um registro do mural com a prévia bonita, uma vez por semana, é a melhor divulgação possível.
- **Direito de imagem.** Foto antiga tem gente que não pediu para estar ali. Um "pedir remoção" no registro do mural evita problema e mostra cuidado (pode nascer junto com o módulo 3).
- **Os primeiros 100 importam mais que os 4 mil.** Escolher 10 ou 15 pessoas ativas do grupo e as bandas que mais postam, chamar cada uma pessoalmente, e dar o papel de curador do mural para quem quiser. As permissões `Archive.*` já permitem isso sem código.

## Ordem sugerida

1. Prévia ao compartilhar (1).
2. ~~Envio para o mural com aprovação (2)~~ (feito em 14/10/2026).
3. Marcar pessoas nas fotos (3), com o "pedir remoção".
4. "Neste dia" (4) e resumo semanal (5).
5. Entrar com Google ou Facebook (6), antes de divulgar no grupo.
6. Bandas: seguir banda e lançamentos (10), classificados (8), press kit (9).
7. Casas de show (11), áudio (13), histórias (14), banda da semana (12), PWA (7) e podcast (15), conforme o uso pedir.
