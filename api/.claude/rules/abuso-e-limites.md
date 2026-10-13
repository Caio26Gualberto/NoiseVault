---
paths:
  - "**/Headbanger.Api/RateLimiting/**"
  - "**/Headbanger.Api/Program.cs"
  - "**/Headbanger.Application/Auth/**"
  - "**/Headbanger.Application/Users/**"
---

# Decisões — Abuso: limites de taxa e de volume

Endpoint público sem limite é convite: um único cliente abre milhares de registros, cada chamada a um gateway externo custa dinheiro, e contas vazias são criadas em massa. Nenhuma invasão — só ausência de limite.

## Duas camadas, e as duas são necessárias

**Taxa** (`ApiRateLimiting`) trava o volume por janela de tempo, particionado por `account_id` quando há token e por IP quando não há. **Volume** (regra de negócio no AppService) trava o estado absurdo independentemente do ritmo: um atacante que respeite a janela e distribua IPs ainda não consegue ter mil registros abertos.

Só rate limit não basta porque IP roda barato. Só regra de negócio não basta porque `register` e `login` não têm conta para contar.

## Os números, e por que estes

| Política | Limite | Motivo |
|---|---|---|
| global | 240/min | teto de segurança; uso normal não chega perto |
| `register` | 5/h por IP | cadastro é evento raro por pessoa; 5 cobre família e rede compartilhada |
| `login` | 10/5min por IP | erro de senha acontece; script de força bruta não |
| `refresh-token` | 30/5min por IP | grava/revoga linha a cada chamada |

Endpoint novo que **cria**, **envia** ou **chama fora** nasce com política própria e uma linha nesta tabela. Os números são ponto de partida; o projeto ajusta e registra o porquê.

## O rejeitado responde no envelope, não em texto solto

`OnRejected` escreve `ApiResponse` com `TOO_MANY_REQUESTS` via `EnvelopeJson` (camelCase) e loga `RATE_LIMIT_REJECTED`. Um 429 com corpo vazio ou PascalCase cai no fallback genérico do cliente — a pessoa vê "erro inesperado" e tenta de novo, o contrário do que o limite quer. Ver `observabilidade.md`.
