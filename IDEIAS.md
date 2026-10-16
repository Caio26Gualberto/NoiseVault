# Ideias — próximos módulos do Noise Vault

Backlog de produto levantado em 14/10/2026, para trazer a comunidade do grupo do Facebook (pouco mais de 4 mil membros, muita gente das antigas compartilhando vídeos e fotos antigas, e gente nova) e para movimentar as bandas locais. **Nada daqui é decisão ainda.** Cada ideia é lapidada antes de virar módulo e, quando virar, ganha a sua rule em `api/.claude/rules/` e `frontend/.claude/rules/`, e uma linha na ordem de `api/.claude/rules/dominio.md`. Aí a ideia sai deste arquivo ou fica ~~tachada~~ com o link para a rule.

## A tese

As pessoas não vão sair do grupo do Facebook por uma lista de recursos. Elas vêm quando o Noise Vault faz bem algo que o grupo faz mal, e quando cada coisa compartilhada no grupo leva de volta para cá.

O Facebook é ótimo para **postar** e péssimo para **guardar**: em uma semana a foto de 1988 some no feed, ninguém acha de novo, e quem aparece nela nunca fica sabendo. O Noise Vault é o arquivo organizado (por ano, show, banda, casa) e o lugar onde a cena se encontra de novo.

## Prioridade 1 — a ponte com o grupo do Facebook

As três primeiras atacam direto o motivo de o pessoal estar no grupo hoje (fotos e vídeos antigos) e transformam cada post no Facebook num convite para cá.

### ~~1. Prévia bonita ao compartilhar o link~~

Feita em 14/10/2026 pela rota de compartilhamento: o botão "Compartilhar" do post, do evento, do registro do mural, da banda e do perfil gera `/s/...`, que devolve as tags Open Graph para o robô e redireciona a pessoa. Ver `api/.claude/rules/compartilhamento.md` e `frontend/.claude/rules/compartilhamento.md`.

### 1.b Prévia também no link copiado da barra do navegador

- **Problema**: quase todo mundo copia o endereço da barra (`/mural/12`), e não usa o botão. Esse link continua sem foto e sem título.
- **Ideia**: quem serve o front olha o `User-Agent` e manda os robôs (`facebookexternalhit`, `WhatsApp`, `Twitterbot`, `TelegramBot`, `Discordbot`) para a mesma rota `/s/...` da API; a pessoa recebe a SPA de sempre.
- **Depende da hospedagem**: o Amplify não deixa pôr função no CloudFront dele. Com o front em S3 + o mesmo CloudFront da mídia, uma CloudFront Function (`cf.updateRequestOrigin`) resolve, sem custo extra no plano. Ver "Front e a prévia de links" em `api/.claude/rules/hospedagem.md`.
- **Apoio**: a montagem das tags já existe (`SharePreviewAppService`); falta só desviar o robô.

### ~~2. A comunidade envia para o mural, a curadoria aprova~~

Virou módulo em 14/10/2026: ver "Envio da comunidade" em `api/.claude/rules/mural.md` e em `frontend/.claude/rules/mural.md`. Decidido: a curadoria edita e publica ou recusa com motivo; o registro sai em nome de quem enviou ("Acervo de"); os limites de mídia são os do arquivo; até 5 envios pendentes por pessoa; permissão `Archive.Review`.

### ~~3. Marcar pessoas nas fotos antigas ("quem é esse?")~~

Virou módulo em 14/10/2026, ampliado pelo usuário para ficar "igual Facebook": @menção de pessoas e bandas em posts e comentários, e marcação de pessoas no ponto da foto, no feed e no mural. Qualquer pessoa marca, com conta ou só com o nome; quem foi marcado, quem marcou, o autor e a curadoria removem. Ver `api/.claude/rules/marcacoes.md` e `frontend/.claude/rules/marcacoes.md`.

- **Ficou pendente**: o "pedir remoção" da foto inteira (direito de imagem, "Fora do código" abaixo). Hoje a pessoa remove a própria marcação, mas não a foto.

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
- **Adiado em 14/10/2026** pelo usuário: ainda não há domínio nem site no ar, e os dois painéis exigem isso para abrir o login a todo mundo.
- **O que já ficou combinado**:
  - **sem Firebase**: falar direto com o Google (Google Identity Services, ID token) e com o Facebook (SDK do JavaScript, access token). O front manda o token para `POST /api/auth/external`; a API confere com o provedor, acha ou cria a conta (a tabela `UserLogins` do Identity) e devolve o nosso JWT;
  - **Facebook primeiro**: quem vem do grupo abre o link no navegador interno do app do Facebook, onde o login do Google é bloqueado (`disallowed_useragent`) e o do Facebook funciona com um toque;
  - **apelido**: o provedor não tem, então o primeiro login social passa por "Escolha seu apelido" (sugerido pelo nome) antes da boas-vindas;
  - **segurança**: só vincular por e-mail quando o provedor diz que ele é verificado; senão, pedir a senha da conta existente. Em Minha conta, ver e desvincular os logins e criar senha para quem só entrou pelo Facebook;
  - **e-mail ausente**: há conta de Facebook sem e-mail (cadastro por celular). Decidir se aceita a conta sem e-mail ou pede o e-mail na hora.
- **Pré-requisitos**:
  - ~~domínio~~ (`noisevault.net`, comprado em 14/10/2026) e HTTPS;
  - duas páginas públicas: **Política de privacidade** e **Como apagar seus dados** (o Facebook exige a segunda para o app ir ao ar);
  - um app no Google Cloud Console (tela de consentimento externa, escopos básicos, ID do cliente "Aplicativo da Web" com as origens do site) e outro no Meta for Developers (caso de uso "Login do Facebook", `public_profile` + `email`, domínios do SDK).
  - O Client ID e o App ID são públicos (`.env` do front e configuração da API); a chave secreta do Facebook fica só na API, por variável de ambiente.

### ~~7. Instalável no celular (PWA)~~

Parte 1 feita em 14/10/2026: ícone na tela inicial, tela cheia, página sem conexão, aviso de versão nova e "Instalar o app" (com o passo a passo do iPhone). Ver `frontend/.claude/rules/pwa.md`.

### 7b. Notificação no celular (Web Push)

- O sino avisa em tempo real só com o site aberto. Com o app instalado, dá para mandar a notificação do sistema ("Fulano marcou você numa foto de 1991") mesmo com ele fechado.
- **Android** funciona bem; **iPhone** só a partir do iOS 16.4 e só com o app instalado na tela inicial.
- **Precisa de backend**: chaves VAPID, a inscrição de cada aparelho guardada por pessoa, o envio por job do Hangfire ao lado do SignalR e uma preferência em Minha conta ("receber no celular").
- **Pontos a decidir**: quais tipos de notificação vão para o celular (todos ou só menção, marcação e convite), e se agrupa como o sino.

## Para as bandas locais

### 8. Classificados de músico

- "Procuro baterista", "toco baixo e procuro banda", "vendo amplificador".
- É o que mais faz músico entrar todo dia. O perfil já tem instrumento e cidade.
- **Pontos a decidir**: tipos de anúncio, validade (anúncio vence sozinho), contato (pela plataforma ou link externo), se venda de equipamento entra (a loja ficou fora do escopo; classificado entre pessoas é outra coisa).
- **Conversa de 14/10/2026: virou dois módulos.**
  - ~~**8a. Músicos e bandas (vem primeiro).**~~ **Feito em 15/10/2026** como o módulo **Ensaio** (`api/.claude/rules/ensaio.md`, `frontend/.claude/rules/ensaio.md`), com o visual "D · Flyer limpo". Ficaram para depois: vídeo na ficha, "seguir vaga" (avisar quando abrir vaga do meu instrumento) e o mapa de quem toca perto.
    - Do lado da pessoa, uma **ficha de músico** opcional, parte do mesmo perfil e não um segundo perfil (`User` continua sendo uma pessoa só):
      - instrumentos com nível, gêneros que quer tocar, influências, objetivo, região e até onde topa ir;
      - disponibilidade, estrutura (equipamento, transporte, lugar de ensaio) e mostra do som;
      - um interruptor "Estou procurando banda", que tira ou põe a pessoa na vitrine.
    - Do lado da banda, **"Vagas abertas"** na página dela.
    - A banda busca músicos e usa o **convite de banda que já existe**. O músico vê as vagas e clica em "Tenho interesse", e a banda recebe a ficha por notificação.
  - **Experiência própria, como um site à parte com a mesma marca**: moldura e navegação só dele, no subdomínio **`ensaio.noisevault.net`** (o nome ainda é proposta; "Line-up" e "Procura-se" também foram sugeridos).
    - Linguagem visual proposta: **o mural de recados da loja de discos**, com anúncio como flyer xerocado preso com fita e a tirinha de destacar como "Tenho interesse".
    - Antes do código, um **protótipo visual** para aprovação.
  - **8b. Mercado (venda e troca, depois)**, em **`mercado.noisevault.net`**: preço, estado, fotos obrigatórias, reservado e vendido, validade de 30 dias, denúncia, dicas contra golpe, sem pagamento pelo site. Talvez serviços (aula, estúdio, gravação, foto) entrem como um tipo. O contato da venda pede uma decisão sobre mensagem (externa, conversa por anúncio ou mensagem direta geral).
  - ~~**Pré-requisito dos subdomínios: sessão por cookie.**~~ **Feito em 14/10/2026** (`api/.claude/rules/auth-e-contas.md`, `frontend/.claude/rules/auth.md`). Antes, os tokens ficavam no `localStorage`, que é preso a cada endereço, e o login não atravessaria os subdomínios. O refresh token passa a um cookie `HttpOnly` em `.noisevault.net`, criado pela API, e o token de acesso fica só na memória. O CORS passa a aceitar credenciais com a lista exata de origens, e o refresh ganha proteção contra requisição forjada (CSRF). Deixa o site principal mais seguro também. Vem antes do 8a.

### 9. Página da banda como press kit

- ~~Player do Spotify, Bandcamp ou YouTube embutido, discografia~~ (feito em 16/10/2026: discografia importada do Spotify ou manual, com player embutido, `api/.claude/rules/discografia.md`), contato para show.
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

1. ~~Prévia ao compartilhar (1)~~ (feita pelo botão; o link da barra é o 1.b, quando a hospedagem do front for decidida).
2. ~~Envio para o mural com aprovação (2)~~ (feito em 14/10/2026).
3. ~~Marcar pessoas nas fotos (3)~~ (feito em 14/10/2026, com @menções); falta o "pedir remoção" da foto.
4. "Neste dia" (4) e resumo semanal (5).
5. Entrar com Google ou Facebook (6), antes de divulgar no grupo.
6. Bandas: seguir banda e lançamentos (10), classificados (8), press kit (9).
7. Casas de show (11), áudio (13), histórias (14), banda da semana (12), ~~PWA (7)~~ (parte 1 feita; falta o Web Push, 7b) e podcast (15), conforme o uso pedir.
