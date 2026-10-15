# CLAUDE.md — Monorepo Noise Vault

## Sobre este projeto

**Noise Vault** — ponto de encontro único da cena do ABC Paulista: gente das antigas de qualquer tribo (metal, punk, hardcore e afins), bandas consagradas e bandas começando divulgam shows, projetos e músicas no mesmo lugar. O objetivo é uma comunidade forte que movimente a cena; por isso nada na marca, no vocabulário ou na UI favorece uma tribo só. Feed de posts com fotos e vídeos, mural das antigas (arquivo de fotos e fitas da cena, publicado pela curadoria), agenda de eventos, perfis de pessoas e de bandas com integrantes, convites de banda e notificações. Público: fãs e músicos da região. Administração (moderação, equipe) é a mesma API com permissão, dentro do mesmo frontend.

É a reescrita do zero do projeto legado "NewLevel" (`C:\Projetos\Pessoal\Alvo\newlevel`). O legado serve só como **inventário de funcionalidades**; nenhuma linha de código, entidade ou tela dele é referência — as decisões de modelagem que o substituem estão em `api/.claude/rules/dominio.md`.

**Estado da entrega (outubro/2026):** backend com os módulos **Auth** (registro com confirmação de e-mail, login, refresh, logout, esqueci/redefinir senha, troca de senha), **Perfil e mídia** (avatar e capa no S3, perfil público, busca de pessoas), **Bandas e convites** (criar e editar banda, integrantes, convites) e **Feed** (posts de pessoa e de banda com fotos e vídeo, curtidas, comentários; @menção de pessoas e bandas no texto e nos comentários, e marcação de pessoas nas fotos, também no mural; o vídeo sobe direto ao S3 em partes e é convertido por ffmpeg num job), **Eventos** (agenda com line-up, "Vou", comentários, cancelamento e post automático no feed) , **Mural das antigas** (registros com fotos e vídeos de arquivo, linha do tempo, "Eu estava lá"; quem tem `Archive.Create` publica direto, e a comunidade envia para a curadoria aprovar ou recusar com motivo, `Archive.Review`), **Ensaio** (ficha de músico com o interruptor "Estou procurando banda", vitrine, vagas da banda com validade e o "Tenho interesse" que vira convite de banda; o front mostra num site à parte em `ensaio.noisevault.net`, pelo mesmo build), **Ensaio** (ficha de músico com o interruptor "Estou procurando banda", vitrine, vagas da banda com validade e o "Tenho interesse" que vira convite de banda; o front mostra num site à parte em `ensaio.noisevault.net`, pelo mesmo build), **Notificações** (agrupadas, em tempo real por SignalR, e-mail só no convite de banda) e **Suporte** (relato vira ticket com tags, páginas, prints e vídeo de até 2 min; caixa de entrada com permissão `Support.*`, conversa com quem reportou; sem GitHub). A tela de boas-vindas do primeiro acesso é controlada só pelo front, via `localStorage`. O front é instalável no celular (PWA: ícone, tela cheia, página sem conexão), sem notificação push por enquanto. O botão "Compartilhar" gera um link `/s/...` com prévia (foto e título) no WhatsApp e no Facebook. Os sete módulos de `api/.claude/rules/dominio.md` estão entregues e não há mais tela mockada (`frontend/.claude/rules/mocks.md` virou histórico). Fora de escopo: loja, podcasts, app nativo, login social.

## Mapa

```
NoiseVault/
├── CLAUDE.md            ← este arquivo: o que atravessa api e frontend
├── README.md            ← pré-requisitos e os comandos para subir tudo
├── IDEIAS.md            ← backlog de produto para os próximos módulos (nada ali é decisão até virar rule)
├── docker-compose.yml   ← Postgres 17 na porta 5436, Mailpit (SMTP 1025, caixa em http://localhost:8025) e LocalStack S3 (http://localhost:4566)
├── infra/localstack/    ← init hook do bucket noisevault-media (leitura pública, CORS do PUT pré-assinado, lifecycle de multipart)
├── .github/workflows/   ← CI (build da API, lint + typecheck + build do frontend)
├── api/                 ← .NET 10 · EF Core (Postgres) · Identity + JWT · Hangfire · MailKit — ver api/CLAUDE.md
└── frontend/            ← Vite · React 19 · TypeScript · Tailwind v4 · shadcn/ui — ver frontend/CLAUDE.md
```

Cada pasta tem o seu `CLAUDE.md` (leia primeiro) e `.claude/rules/`. Os dois nasceram do boilerplate em `C:\Users\Administrador\Desktop\Boilerplate` e seguem a filosofia dele: `CLAUDE.md` é convenção, rule é decisão com o porquê, verificação é por execução real, doc e código vão no mesmo commit, sem comentário no código. **A rule é a spec**: decisão não óbvia entra na rule antes ou junto do código, nunca depois.

## O que atravessa os dois

- **Envelope** `{ isSuccess, code, message, data }` em camelCase. O `code` é contrato de i18n: a API emite SCREAMING_SNAKE estável e específico; o frontend traduz em `src/i18n/pt-BR/api.json` via `src/api/api-codes.ts`. Code novo na API → espelho no frontend, mesmo commit.
- **Ordem de trabalho: api → frontend.** O frontend espelha codes e contratos da API; a API não sabe que ele existe. Enquanto um módulo é mock, o DTO do mock é a proposta de contrato — quando o backend nasce, o backend manda e o mock morre.
- **Fronteira HTTP única** em cada lado: controllers que só delegam na API; `src/api/` no frontend. Erro é interpretado uma vez na fronteira.
- **Toda lista nasce paginada no servidor.** `PagedRequestDto`/`PagedResultDto` ↔ `PagedResult<T>`. Mock pagina a fixture do mesmo jeito.
- **Identidade:** `User` é sempre uma pessoa; banda nunca tem login. `nickname` é o handle público (`/perfil/:nickname`); banda tem `slug` (`/bandas/:slug`).
- **Segredo nunca em Git.** `appsettings.Development.json` e `.env` apontam para o Docker local e não carregam segredo real; produção vem de variável de ambiente.
- **Dado pessoal nunca em URL nem em log.** A única exceção registrada é o `email` no link de redefinição de senha (`api/.claude/rules/email.md`).
- Idioma: UI, docs e rules em PT-BR; código, codes e mensagens de API em inglês. Sem comentário no código.

## Como subir

1. `docker compose up -d` (Postgres em `localhost:5436`, banco `noisevault`, `postgres`/`postgres`; Mailpit em `http://localhost:8025`; LocalStack S3 em `http://localhost:4566`, bucket `noisevault-media` criado no boot). Produção é **AWS**, ainda sem deploy: domínio `noisevault.net`, Lightsail em São Paulo (API + Postgres), S3 + CloudFront, MediaConvert, SES e o front no Amplify — ver `api/.claude/rules/hospedagem.md`. O LocalStack emula só o S3 (`armazenamento.md`).
2. ffmpeg instalado (`winget install Gyan.FFmpeg`) para a conversão de vídeo. `cd api && dotnet run --project src/NoiseVault.Api` → `http://localhost:5041` (`/health`, `/swagger`, `/hangfire`). Migration e seed rodam no boot; admin de dev `caiogualbertodev@outlook.com` / `12341234`. Em Development o `DevSeed` também cria o conjunto de exemplo (pessoas `@noisevault.dev` com senha `12341234`, bandas, feed, eventos, mural, Ensaio, suporte) e reenvia ao LocalStack a mídia que sumiu no restart (`api/.claude/rules/seed-de-desenvolvimento.md`).
3. `cd frontend && npm install && npm run dev` → `http://localhost:5173`.

Verificação de cada lado: roteiros nas rules (`api/.claude/rules/auth-e-contas.md`, `frontend/.claude/rules/auth.md` e `shell-e-navegacao.md`).

## Commits

Em português, prefixados pelo lado: `API: …`, `Front: …`, `Repo: …` (raiz, CI, compose). Doc e código no mesmo commit. Branch única `main` enquanto o projeto é de uma pessoa.

## Hierarquia de regras

Rule do domínio > `CLAUDE.md` da pasta > este arquivo > convenção do framework. Uma rule que contradiz o `CLAUDE.md` diz por quê; se não diz, está errada.
