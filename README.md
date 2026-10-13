# Headbanger

Comunidade online de metal e rock do ABC Paulista. Monorepo com `api/` (.NET 10) e `frontend/` (React 19 + Vite).

## Pré-requisitos

- .NET SDK 10 e `dotnet-ef` (`dotnet tool install -g dotnet-ef`)
- Node 24+
- Docker com Compose v2

## Subindo em desenvolvimento

```bash
docker compose up -d                                  # Postgres (5436) + Mailpit (http://localhost:8025)
cd api && dotnet run --project src/Headbanger.Api     # http://localhost:5041 — /swagger, /health, /hangfire
cd frontend && npm install && npm run dev             # http://localhost:5173
```

Migration e seed rodam no boot da API em Development. Admin de dev: `admin@headbanger.local` / `12341234`. E-mails de confirmação e de redefinição de senha caem no Mailpit.

## Documentação

Comece por `CLAUDE.md` na raiz, depois `api/CLAUDE.md` e `frontend/CLAUDE.md`. As decisões por domínio estão em `*/.claude/rules/`.
