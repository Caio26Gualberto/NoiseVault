---
paths:
  - "**/NoiseVault.Api/RateLimiting/**"
  - "**/NoiseVault.Api/Program.cs"
  - "**/NoiseVault.Application/Auth/**"
  - "**/NoiseVault.Application/Users/**"
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
| `confirm-email` | 10/h por IP | cada chamada valida um token do Identity; 10 cobre link clicado duas vezes e reenvio |
| `resend-confirmation` | 3/h por IP | cada chamada pode mandar um e-mail; é o endpoint mais barato de abusar para spam |
| `forgot-password` | 3/h por IP | idem, e o 200 constante não deixa o atacante saber se acertou |
| `reset-password` | 10/h por IP | tentativa de token; o token tem 1 h de vida, então 10 por hora é o teto de chute |
| `profile-upload` | 20/h por conta | processar imagem custa CPU e storage; trocar avatar 20 vezes por hora já é teste, não uso |
| `user-search` | 60/min por conta | a topbar busca enquanto a pessoa digita (debounce de 350 ms); 60 por minuto é folga de sobra |
| `band-create` | 5/h por conta | criar banda é raro; cinco por hora já é spam ou teste |
| `band-invite` | 30/h por conta | montar a formação de uma banda cabe com folga; disparar convite em massa não |
| `media-photo` | 60/h por conta | um post tem até 10 fotos; 60 por hora cobre um show inteiro postado em vários posts |
| `media-video` | 10/h por conta | abrir um multipart reserva storage e um job de conversão; soma-se ao limite de volume de 3 vídeos em andamento por pessoa (`midia-e-video.md`) |
| `post-create` | 30/h por conta | publicar é frequente, mas 30 por hora já é spam |
| `comment-create` | 60/h por conta | conversa rápida cabe; robô de comentário não. Compartilhado entre post e evento |
| `event-create` | 10/h por conta | divulgar a agenda da semana cabe; spam de evento não. Somado ao limite de volume de 20 eventos futuros por pessoa (`eventos.md`) |

Endpoint novo que **cria**, **envia** ou **chama fora** nasce com política própria e uma linha nesta tabela. Os números são ponto de partida; o projeto ajusta e registra o porquê.

## O rejeitado responde no envelope, não em texto solto

`OnRejected` escreve `ApiResponse` com `TOO_MANY_REQUESTS` via `EnvelopeJson` (camelCase) e loga `RATE_LIMIT_REJECTED`. Um 429 com corpo vazio ou PascalCase cai no fallback genérico do cliente — a pessoa vê "erro inesperado" e tenta de novo, o contrário do que o limite quer. Ver `observabilidade.md`.
