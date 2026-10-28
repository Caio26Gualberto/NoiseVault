---
paths:
  - "src/auth/**"
  - "src/api/**"
  - "src/pages/entrar/**"
  - "src/pages/criar-conta/**"
  - "src/pages/confirmar-email/**"
  - "src/pages/esqueci-senha/**"
  - "src/pages/redefinir-senha/**"
  - "src/pages/definir-senha/**"
  - "src/pages/bem-vindo/**"
  - "src/pages/regras/**"
  - "src/auth/welcome-storage.ts"
  - "src/pages/minha-conta/**"
---

# Decisões — Auth no frontend

Rule irmã: `api/.claude/rules/auth-e-contas.md` — a regra é do backend, a tela só reflete. Codes espelhados em `src/api/api-codes.ts` + `i18n/pt-BR/api.json`.

## Sessão: a arquitetura do boilerplate, sem o gate de painel

`src/api/` (client, interceptor de refresh single-flight, `base-service`, `token-storage`) veio do boilerplate e mudou num ponto: a sessão por cookie (abaixo). `auth-provider.tsx` mudou em um ponto: **não exige `Admin.Access`** — qualquer `User` entra na comunidade. `fetchMyAccess` continua sendo chamado (para `can()` nas telas de admin), mas falha dele não derruba a sessão: o `access` fica vazio.

A sessão carrega o `ProfileDto` inteiro (dados de acesso e o perfil público: avatar, capa, bio, cidade, gêneros, links — ver `perfil.md`). Ação de perfil devolve o `ProfileDto` novo e a tela chama `replaceUser(dto)`; `refreshUser()` fica para quando não há DTO na mão.

## Sessão por cookie: o refresh no cookie `HttpOnly`, o token de acesso em memória

Decidido em 14/10/2026, antes dos subdomínios (`IDEIAS.md`, item 8). A regra é do backend (`../api/.claude/rules/auth-e-contas.md`); aqui fica o que a tela faz.

- **Nada de token no `localStorage`.** O refresh token chega num cookie `nv_refresh` que o JavaScript não lê e o navegador manda sozinho (`withCredentials: true` no `client.ts` e no cliente sem interceptor). O token de acesso fica numa variável do `token-storage.ts` e some ao recarregar. Um script injetado na página não leva a sessão embora: no máximo usa o token de 15 minutos enquanto a aba está aberta.
- **Boot**: o `auth-provider` chama `restoreSessionToken()`, que faz `POST /api/auth/refresh-token` sem corpo. Deu token → busca o perfil e o acesso; não deu → deslogado, sem toast. O status `unknown` cobre essa ida à API, então o `RequireAuth` não redireciona antes.
- O `token-storage` apaga as chaves da versão anterior (`access_token`, `refresh_token`, `user_email`) ao carregar. Quem estava logado antes da mudança entra de novo uma vez.
- **Várias abas**: cada aba tem o seu token em memória, mas o cookie é um só e roda a cada renovação. As renovações passam em fila pelo **Web Locks** (`navigator.locks`, trava `nv-refresh`): a segunda aba espera a primeira e renova com o cookie novo. Sem Web Locks (navegador antigo), sobra o reenvio de 400 ms depois de um `INVALID_TOKEN`. Abrir três abas juntas sem a fila derrubava uma delas (visto em 14/10/2026).
- `freshToken` (o do hub de notificações) renova quando não há token em memória ou ele vence em menos de 60 s.
- **Sair** chama o `logout`, que apaga o cookie na API; o front só esquece o token.
- `ORIGIN_NOT_ALLOWED` (o refresh vindo de origem fora da lista) tem tradução, mas só aparece com o front num endereço que a API não conhece.

## Telas e caminhos

| Caminho | Tela | Observação |
|---|---|---|
| `/entrar` | login | Facebook e Google em cima (`login-social.md`), e-mail + senha; link para criar conta e para esqueci a senha |
| `/entrar/apelido`, `/entrar/vincular` | segundo passo do login social | só com o ticket no `state` da rota (`login-social.md`) |
| `/criar-conta` | registro | nome, nickname, e-mail, senha, confirmação; sucesso vira o estado "confira seu e-mail" **na mesma tela**, com botão de reenviar |
| `/confirmar-email?userId&token` | confirmação | chama `confirm-email` na montagem; estados: confirmando / confirmado (botão "Entrar") / inválido (campo de e-mail + "Reenviar") |
| `/esqueci-senha` | pedido de reset | só e-mail; sucesso vira "confira seu e-mail" sem dizer se existe |
| `/redefinir-senha?email&token` | nova senha | senha + confirmação; sucesso → `/entrar` com toast |
| `/definir-senha` | senha inicial (convite) | copiada do boilerplate; só com `isNeededChangePassword` |
| `/regras` | regras da comunidade | os combinados (o assunto é música, não é palanque político, respeito, divulgar sim e spam não, avisar pelo suporte) sobre a mesma capa do `/bem-vindo`; "Bora, combinado" marca o `localStorage` e segue; aberta também sem login (`visitante.md`), com "Voltar" para quem já aceitou e "Entrar" para o visitante |
| `/bem-vindo` | boas-vindas do primeiro acesso | o texto original do projeto sobre a capa do *To Mega Therion* (Celtic Frost) em tela cheia; "Começar jornada" marca o `localStorage` e vai para `/feed` |
| `/meu-perfil` | meu perfil | o que os outros veem: avatar, capa, nome, bio, cidade, instrumento, gêneros e links, com a prévia do perfil; detalhes em `perfil.md` |
| `/minha-conta` | conta | só o que é sensível: e-mail e apelido (leitura), senha, notificações por e-mail e localização aproximada |

Caminhos ficam em `auth/routes.ts`; ninguém escreve `'/entrar'` em página.

## Gates do `RequireAuth`, nesta ordem

1. `status === 'unknown'` → `AppSkeleton` de tela cheia (nunca redirecionar antes do boot terminar: redirecionaria quem tem sessão válida).
2. Não autenticado → `/entrar` com `state.from` para voltar depois.
3. `isNeededChangePassword` → `/definir-senha` (exceto nela).
4. Ainda não aceitou as regras (`hasAcceptedRules(user.id)` falso) → `/regras` (exceto nela e em `/minha-conta`).
5. Ainda não viu as boas-vindas (`hasSeenWelcome(user.id)` falso) → `/bem-vindo` (exceto nela, em `/regras` e em `/minha-conta`, para não prender quem quer só sair).
6. `permission` sem capacidade → `/feed`.

`redirect-if-authenticated.tsx` faz o inverso para `/entrar`, `/criar-conta` e `/esqueci-senha`.

## Boas-vindas: `localStorage` por usuário, não estado do servidor

Quem já viu a tela de boas-vindas fica marcado em `localStorage` na chave `app-welcome-seen:{userId}` (`auth/welcome-storage.ts`). Decidido em 13/10/2026: a versão anterior gravava `OnboardingCompletedAt` no banco, e isso era custo sem dono — ver `../api/.claude/rules/auth-e-contas.md`.

- A chave é **por usuário**, para outra conta no mesmo navegador ver a apresentação.
- `tokenStorage.clear()` **não** apaga a chave: sair e entrar de novo não mostra a tela outra vez.
- Trocar de navegador, usar aba anônima ou limpar os dados mostra de novo. Aceito: é uma tela de apresentação, não um passo obrigatório.
- `localStorage` indisponível (modo privado restrito) conta como "já viu" — falha aberta, a pessoa nunca fica presa num redirecionamento.

## Regras da comunidade: antes das boas-vindas, versionadas no `localStorage`

Pedido do usuário em 28/10/2026, antes de abrir o site para a cena: quem entra lê os combinados antes da apresentação. O principal é que o Noise Vault não é palanque político, de lado nenhum: o que a banda canta e como o artista se posiciona fica para o palco, e aqui o assunto é música, lançamento, show e apoio à cena local.

- A chave é `app-rules-accepted:{userId}` (`auth/rules-storage.ts`) e o valor é a `rulesVersion`, não `'true'`. Mudou o texto de um jeito que todo mundo precisa reler → troca a constante, e a tela volta para todas as contas no próximo acesso, sem repetir o `/bem-vindo`.
- Mesmas regras das boas-vindas: por usuário, o `tokenStorage.clear()` não apaga, outro navegador mostra de novo e `localStorage` indisponível conta como aceito (falha aberta). É um aviso de convivência, não um aceite jurídico: quem precisa de prova de aceite é o servidor, e isso não existe.
- Um botão só, "Bora, combinado", sem caixa de "li e concordo" (escolha do usuário: tom de gente da cena, sem cara de contrato).
- A mesma página fica aberta em `/regras` sem login, para a equipe poder apontar a regra a alguém; o link mora no menu da conta e no rodapé das telas de auth. Fica fora do shell, como a `/bem-vindo`, com `<RequireAuth allowVisitor>`.
- O `skipWelcome` do Ensaio pula os dois gates: as regras aparecem quando a pessoa entra no site principal.

## Login vindo do Ensaio

- O `RequireAuth` aceita `skipWelcome` (o shell do Ensaio usa), porque a tela de boas-vindas é do site principal.
- No subdomínio, quem não tem sessão vai para `/entrar?voltar=<endereço do Ensaio>` do site principal. Depois do login, ou se a pessoa já tem sessão, volta para lá.
- Só volta se a origem for a do Ensaio (`safeReturnUrl` em `src/ensaio/frame.ts`); qualquer outro `voltar` é ignorado. Detalhes em `ensaio.md`.

## A única exceção à regra "página não trata erro"

O toast global cobre todo erro de API. **`EMAIL_NOT_CONFIRMED` no login é a exceção**: além do toast, a tela de login mostra uma faixa inline "Seu e-mail ainda não foi confirmado" com o botão "Reenviar e-mail", porque a ação de reenviar é da tela e a pessoa precisa dela ali, não num toast que some. O hook `use-login.ts` captura o `ApiError` por `code` e expõe `pendingConfirmation`. A outra é o `EXTERNAL_TICKET_INVALID` em `/entrar/apelido` e `/entrar/vincular`, que troca a tela por "Essa etapa expirou" (`login-social.md`). Nenhuma outra tela faz isso sem registrar aqui.

## "Apelido", não "nickname"

Decisão do usuário em 14/10/2026: o público inclui muita gente das antigas, que não sabe o que é "nickname". Na tela o campo é **Apelido** em todo lugar (cadastro, Minha conta, mensagens de erro, busca de integrante). O código, o DTO e o code da API continuam `nickname` (`INVALID_NICKNAME`, `NICKNAME_TAKEN`): só a palavra que a pessoa lê mudou.

Os tooltips vieram do cadastro do legado e explicam o campo sem tomar espaço no formulário (`hint` do `AppField`):
- **Apelido**: como te conhecem (na banda, no rolê ou o das antigas), mais o porquê das minúsculas sem espaço (ele vira o endereço do perfil). O texto do legado falava só das antigas; o usuário ampliou em 14/10/2026 porque o site serve à música em geral;
- **E-mail**: é a forma de login;
- **Cidade** (Minha conta, porque o cadastro não pede cidade): "A cidade de onde você é, ou onde costumava andar".

## Validação espelha o Identity

`auth/validators.ts` concentra os schemas zod reutilizados: `passwordSchema` = mínimo 6 e pelo menos um dígito (a política do `AddIdentity`); `nicknameSchema` = `^[a-z0-9](?:[a-z0-9._]{1,28}[a-z0-9])?$` com `transform(lowercase)`; `nameSchema` = 2 a 100; `emailSchema`. ⚠️ Mudou a política no backend, muda aqui no mesmo commit — senão a tela aprova o que o servidor recusa.

## Roteiro de verificação

Com a API e o Mailpit (http://localhost:8025) no ar:

1. `/feed` sem sessão → `/entrar`. Criar conta com nickname `Caio.Teste` → campo normaliza para `caio.teste`; senha `abcdef` → erro inline antes de enviar; válido → estado "confira seu e-mail".
2. Abrir o link do Mailpit → `/confirmar-email` mostra "E-mail confirmado" → "Entrar" → login → `/regras` → "Bora, combinado" → `/bem-vindo` com a capa do Celtic Frost e o texto original → "Começar jornada" → `/feed`. Recarregar `/feed` mantém a sessão; sair e entrar de novo vai direto ao `/feed`; apagar `app-welcome-seen:{id}` do `localStorage` faz a tela voltar; apagar `app-rules-accepted:{id}` faz só a `/regras` voltar. `/regras` sem sessão mostra o texto com "Entrar"; com o aceite feito, "Voltar".
3. Login de conta não confirmada → toast + faixa com "Reenviar"; clicar → toast `CONFIRMATION_EMAIL_SENT` e e-mail novo no Mailpit.
4. `/esqueci-senha` com e-mail inexistente → mesma tela de sucesso; com existente → link no Mailpit → `/redefinir-senha` → nova senha → `/entrar` com toast → entrar com a nova.
5. Depois do login, o `localStorage` não tem token nenhum e o `document.cookie` não mostra o `nv_refresh` (o DevTools, em Application → Cookies, mostra, com HttpOnly marcado). Recarregar mantém a sessão; abrir três abas de uma vez mantém as três logadas. Apagar o cookie no DevTools e recarregar → `/entrar` sem toast; apagar com a página aberta e esperar o token vencer → `/entrar` com toast `SESSION_EXPIRED` na próxima request.
6. `/minha-conta` → trocar senha com atual errada → toast `INVALID_CREDENTIALS`; certa → toast `PASSWORD_CHANGED` e a sessão continua, inclusive depois de recarregar.
7. Sair → o cookie some do DevTools; `/feed` manda para `/entrar`.
