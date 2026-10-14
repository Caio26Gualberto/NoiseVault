# CLAUDE.md — Monorepo Noise Vault

## Sobre este projeto

**Noise Vault** — ponto de encontro único da cena do ABC Paulista: gente das antigas de qualquer tribo (metal, punk, hardcore e afins), bandas consagradas e bandas começando divulgam shows, projetos e músicas no mesmo lugar. O objetivo é uma comunidade forte que movimente a cena; por isso nada na marca, no vocabulário ou na UI favorece uma tribo só. Feed de posts com fotos e vídeos, galeria de vídeos (YouTube) e de fotos com moderação, agenda de eventos, perfis de pessoas e de bandas com integrantes, convites de banda e notificações. Público: fãs e músicos da região. Administração (moderação, equipe) é a mesma API com permissão, dentro do mesmo frontend.

É a reescrita do zero do projeto legado "NewLevel" (`C:\Projetos\Pessoal\Alvo\newlevel`). O legado serve só como **inventário de funcionalidades**; nenhuma linha de código, entidade ou tela dele é referência — as decisões de modelagem que o substituem estão em `api/.claude/rules/dominio.md`.

**Estado da entrega (outubro/2026):** backend com os módulos **Auth** (registro com confirmação de e-mail, login, refresh, logout, esqueci/redefinir senha, troca de senha), **Perfil e mídia** (avatar e capa no S3, perfil público, busca de pessoas) **Bandas e convites** (criar e editar banda, integrantes, convites) e **Feed** (posts de pessoa e de banda com fotos e vídeo, curtidas, comentários; o vídeo sobe direto ao S3 em partes e é convertido por ffmpeg num job) e **Eventos** (agenda com line-up, "Vou", comentários, cancelamento e post automático no feed). A tela de boas-vindas do primeiro acesso é controlada só pelo front, via `localStorage`. Todo o resto do frontend é **mock** por página, com a mesma assinatura que o service real vai ter. A ordem dos módulos seguintes está em `api/.claude/rules/dominio.md`; o que cada tela mockada espera do backend está em `frontend/.claude/rules/mocks.md`. Fora de escopo: loja, podcasts, app nativo, login social.

## Mapa

```
NoiseVault/
├── CLAUDE.md            ← este arquivo: o que atravessa api e frontend
├── README.md            ← pré-requisitos e os comandos para subir tudo
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

1. `docker compose up -d` (Postgres em `localhost:5436`, banco `noisevault`, `postgres`/`postgres`; Mailpit em `http://localhost:8025`; LocalStack S3 em `http://localhost:4566`, bucket `noisevault-media` criado no boot). Produção é **AWS** (S3 + CloudFront, SES depois); o LocalStack emula só o que já usamos — ver `api/.claude/rules/armazenamento.md`.
2. ffmpeg instalado (`winget install Gyan.FFmpeg`) para a conversão de vídeo. `cd api && dotnet run --project src/NoiseVault.Api` → `http://localhost:5041` (`/health`, `/swagger`, `/hangfire`). Migration e seed rodam no boot; admin de dev `admin@noisevault.local` / `12341234`.
3. `cd frontend && npm install && npm run dev` → `http://localhost:5173`.

Verificação de cada lado: roteiros nas rules (`api/.claude/rules/auth-e-contas.md`, `frontend/.claude/rules/auth.md` e `shell-e-navegacao.md`).

## Commits

Em português, prefixados pelo lado: `API: …`, `Front: …`, `Repo: …` (raiz, CI, compose). Doc e código no mesmo commit. Branch única `main` enquanto o projeto é de uma pessoa.

## Hierarquia de regras

Rule do domínio > `CLAUDE.md` da pasta > este arquivo > convenção do framework. Uma rule que contradiz o `CLAUDE.md` diz por quê; se não diz, está errada.
