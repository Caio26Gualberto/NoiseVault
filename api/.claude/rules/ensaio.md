---
paths:
  - "**/Musicians/**"
  - "**/BandOpenings/**"
  - "**/Entities/Musician*.cs"
  - "**/Entities/BandOpening*.cs"
  - "**/Enums/Ensaio/**"
  - "**/Controllers/MusiciansController.cs"
  - "**/Controllers/BandOpeningsController.cs"
---

# Decisões — Ensaio (músicos e bandas)

Módulo 10 da ordem em `dominio.md`, o item 8a do `IDEIAS.md`. Músico procura banda, banda abre vaga, e o "Tenho interesse" leva a pessoa até a banda, que convida pelo **convite de banda que já existe** (`bandas.md`). O front mostra tudo no subdomínio `ensaio.noisevault.net` (`frontend/.claude/rules/ensaio.md`). A API não sabe disso: são endpoints comuns em `Controllers/`.

Escopo decidido pelo usuário em 14/10/2026: a ficha, a vitrine, as vagas e o interesse com recado, tudo na primeira entrega. Também entram o selo no perfil, as vagas na página da banda e o post automático no feed.

## A ficha é parte da pessoa, não um segundo perfil

- **`MusicianProfile`** (`EntityBase`) é 1:1 com `User`, com índice único filtrado em `UserId`. `User` continua sendo a pessoa (`dominio.md`); a ficha só acrescenta o que interessa para tocar junto. Campos:
  - `IsLookingForBand` e `LookingSince`;
  - `Goal` (autoral, cover, tanto faz) e `Commitment` (só ensaiar, tocar ao vivo, gravar);
  - `Reach` (minha cidade, ABC, Grande SP, qualquer lugar);
  - `HasGear`, `HasTransport`, `HasRehearsalSpace`;
  - `Influences` (até 300).
- **Tabelas filhas** com FK `Cascade` para a ficha: são parte dela, como a `PhotoTag` é parte da mídia. A ficha não tem exclusão; quem não quer aparecer desliga o interruptor.
  - `MusicianInstrument`: de 1 a 5, com nível; a ordem é a `Position`, e o primeiro é o principal;
  - `MusicianGenre`: até 10. São os sons que a pessoa **quer tocar**, que podem ser diferentes dos do perfil. A ficha nova vem preenchida com os gêneros do perfil;
  - `MusicianAvailability`: dia e turno, com chave composta;
  - `MusicianSampleLink`: até 4 links `https` de YouTube, Spotify, Bandcamp ou SoundCloud (`MusicianRules.IsValidSampleLink`).
- **Instrumento é lista fechada** (`EInstrument`: vocal, guitarra, baixo, bateria, teclado, percussão, sopro, cordas, eletrônico e outro, este com nome livre de até 60). Sem lista fechada, a vitrine e a vaga não conseguem filtrar.
  - O `User.MainInstrument` do perfil continua texto livre e não é tocado pela ficha.
  - O `BandMember.Instrument` também continua texto: o convite feito a partir da vaga leva o rótulo que o front manda.
- **Salvar substitui tudo** (`PUT /api/musicians/me`, como o perfil). Os filhos com `Id` são limpos e recriados; os de chave composta (gêneros e disponibilidade) entram por diferença, como o `UserGenre`, para o EF não acusar duas instâncias com a mesma chave.
- **`GET /api/musicians/me` nunca dá 404**: sem ficha, devolve `exists: false` com os padrões e os gêneros do perfil, para o formulário abrir preenchido. O desfecho é o bool do DTO, não um code (`CLAUDE.md`).

## Vitrine e visibilidade

- A vitrine (`GET /api/musicians`) mostra só quem tem `IsLookingForBand` e conta ativa, do mais recente para o mais antigo (`LookingSince` desc, desempate por `Id`).
- **Ficha com o interruptor desligado**: só o dono a vê. Para os outros é 404 `MUSICIAN_PROFILE_NOT_FOUND`, e o perfil não mostra o selo.
- Ligar grava `LookingSince = agora`; desligar zera. Religar sobe a pessoa para o topo da vitrine de novo, de propósito.
- `UserProfileDto.isLookingForBand` liga o selo "Procurando banda" no perfil. O `PublicProfileAppService` injeta o `IMusicianAppService` (que não depende dele), e não há ciclo.

## Vaga da banda

- **`BandOpening`** (`EntityBase`):
  - `BandId`, `Instrument`, `OtherName?`;
  - `Description` (1 a 500), `Goal?`;
  - `Status` (aberta, preenchida, encerrada), `ExpiresAt`, `ClosedAt?`.
- **Validade de 60 dias**, renovável. Como no convite, **vencida não é status**: é `Open` com `ExpiresAt` no passado, e a busca filtra na leitura. Não precisa de job. O Owner renova (`renew`) e a vaga ganha mais 60 dias.
- **Até 5 vagas abertas por banda** (`OPENING_LIMIT_REACHED`), contando só as abertas e não vencidas.
- **Quem mexe**: os Owners da banda, conferidos no `WHERE` (`Band.Members.Any(Owner == eu)`). Quando a consulta volta vazia, uma segunda consulta separa 404 `OPENING_NOT_FOUND` de 403 `BAND_FORBIDDEN`.
- **Editar, fechar e renovar** só valem com a vaga `Open` (`OPENING_NOT_OPEN`). Fechar escolhe entre "preenchida" (`filled: true`) e "encerrada".
- **Leitura**:
  - a busca traz só as abertas e não vencidas, de bandas não apagadas;
  - a lista da banda traz tudo para os Owners e só as abertas para os outros;
  - o `GET /api/openings/{id}` mostra a vaga em qualquer status, para o link do post e da notificação não quebrar.
- **Filtros** (os mesmos na vitrine e nas vagas, paginados):
  - `instrument`;
  - `cityId`, ou `abc=true` pelo `CityRules.AbcCityIds`. A cidade é a da pessoa na ficha e a da banda na vaga;
  - `genre`. É o da ficha na vitrine e o da banda na vaga;
  - `withSamples`, só na vitrine.

## Post automático

Abrir a vaga cria, na mesma transação, um `Post` da banda com `BandOpeningId` (único, `PostAppService.CreateForOpening`). É o desenho do post de evento (`feed.md`):
- o texto é vazio;
- o `PostDto.opening` (`BandOpeningSummaryDto`) é lido ao vivo e em lote. O `PostAppService` lê o `IRepository<BandOpening>` direto, porque injetar o `IBandOpeningAppService` fecharia um ciclo de DI (ele depende do `IPostAppService`);
- fechar ou vencer **não** apaga o post: o card mostra "preenchida" ou "encerrada" pelo status;
- apagar a vaga apaga o post.

## Interesse e convite

- **`BandOpeningInterest`** (`EntityBase`):
  - `BandOpeningId`, `UserId`, `Message?` (até 300);
  - `Status` (pendente, convidado, dispensado), `BandInvitationId?`.
  - Índice único filtrado em `(BandOpeningId, UserId)`.
- **"Tenho interesse"** (`POST /api/openings/{id}/interest { message? }`) recusa nesta ordem:
  1. recado com mais de 300 → `INVALID_INTEREST_MESSAGE`;
  2. vaga que não existe → `OPENING_NOT_FOUND`;
  3. fechada ou vencida → `OPENING_NOT_OPEN`;
  4. sem ficha → `MUSICIAN_PROFILE_REQUIRED`. Não exige o interruptor ligado: quem já tem banda também pode querer uma vaga;
  5. já é integrante → `ALREADY_BAND_MEMBER`;
  6. já mandou → `OPENING_INTEREST_DUPLICATE`.
- O interesse notifica os Owners com o kind 28 `BandOpeningInterest` (`notificacoes.md`).
- **Retirar** é soft delete, e só vale enquanto o interesse está pendente.
- **Os Owners** listam os interessados (pendentes primeiro, depois os mais novos) com a ficha resumida (`MusicianCardDto`) e o recado. Para cada um, duas ações:
  - **Convidar** chama o `IBandInvitationAppService.Invite` existente com o apelido da pessoa e o instrumento que o front manda. Os erros dele passam direto (`BAND_INVITATION_ALREADY_PENDING`, …). No sucesso, o interesse vira `Invited` com o `BandInvitationId`. Aceitar e recusar são o convite de sempre, e quem é convidado recebe a `BandInvite` (11), sem notificação nova;
  - **Dispensar** marca `Dismissed`, sem avisar a pessoa.
- **"Chamar pra banda"** a partir da vitrine é o mesmo convite (`POST /api/bands/{slug}/invitations`). O front escolhe entre as bandas de que a pessoa é Owner (`GET /api/bands/owned`).
- Um `IBandOpeningAppService` só cuida da vaga e do interesse: o interesse não existe sem a vaga.

## Endpoints

Todos autenticados, em `Controllers/`.

| Método e rota | Code de sucesso |
|---|---|
| `GET /api/musicians` (vitrine, paginada, `instrument`, `cityId`, `abc`, `genre`, `withSamples`) | `MUSICIANS_FOUND` |
| `GET /api/musicians/me` | `MUSICIAN_PROFILE_FOUND` (com `exists`) |
| `PUT /api/musicians/me` | `MUSICIAN_PROFILE_SAVED` |
| `POST /api/musicians/me/looking { isLookingForBand }` | `MUSICIAN_LOOKING_CHANGED` |
| `GET /api/musicians/{nickname}` | `MUSICIAN_PROFILE_FOUND` |
| `GET /api/openings` (paginada, `instrument`, `cityId`, `abc`, `genre`) | `OPENINGS_FOUND` |
| `GET /api/bands/{slug}/openings` | `OPENINGS_FOUND` |
| `POST /api/bands/{slug}/openings { instrument, otherName?, description, goal? }` | `OPENING_CREATED` |
| `GET /api/openings/{id}` | `OPENING_FOUND` |
| `PUT /api/openings/{id}` | `OPENING_UPDATED` |
| `POST /api/openings/{id}/close { filled }` | `OPENING_CLOSED` |
| `POST /api/openings/{id}/renew` | `OPENING_RENEWED` |
| `DELETE /api/openings/{id}` | `OPENING_DELETED` |
| `POST` / `DELETE /api/openings/{id}/interest` | `OPENING_INTEREST_SENT` / `OPENING_INTEREST_WITHDRAWN` |
| `GET /api/openings/{id}/interests` (Owners, paginada) | `OPENING_INTERESTS_FOUND` |
| `POST /api/openings/{id}/interests/{interestId}/invite { instrument }` | `OPENING_INTEREST_INVITED` |
| `POST /api/openings/{id}/interests/{interestId}/dismiss` | `OPENING_INTEREST_DISMISSED` |
| `GET /api/bands/owned` | `BANDS_FOUND` |

O `BandOpeningDto` traz:
- `canManage`;
- `myInterest` (o status e o recado de quem vê);
- `interestCount` (pendentes, só para os Owners);
- `isExpired`.

**Erros:**
- da ficha: `INVALID_MUSICIAN_INSTRUMENTS`, `INVALID_MUSICIAN_PREFERENCES`, `INVALID_GENRE`, `INVALID_AVAILABILITY`, `INVALID_SAMPLE_LINK`, `INVALID_INFLUENCES`, `MUSICIAN_PROFILE_REQUIRED` e `MUSICIAN_PROFILE_NOT_FOUND`;
- da vaga: `INVALID_OPENING_INSTRUMENT`, `INVALID_OPENING_DESCRIPTION`, `OPENING_LIMIT_REACHED`, `OPENING_NOT_FOUND` e `OPENING_NOT_OPEN`;
- do interesse: `INVALID_INTEREST_MESSAGE`, `OPENING_INTEREST_DUPLICATE` e `OPENING_INTEREST_NOT_FOUND`;
- e os de banda: `BAND_NOT_FOUND`, `BAND_FORBIDDEN` e `ALREADY_BAND_MEMBER`.

## Limites

| Política | Limite | Onde |
|---|---|---|
| `opening-create` | 10/h por conta | `POST /api/bands/{slug}/openings` |
| `opening-interest` | 20/h por conta | `POST /api/openings/{id}/interest` |
| `band-invite` | 30/h, compartilhado com o convite | convidar a partir do interesse |
| `user-search` | 60/min | a vitrine |

## Eventos de log (contexto `Ensaio`)

| Evento | Nível | Dados |
|---|---|---|
| `MUSICIAN_PROFILE_CREATED` | Info | `ProfileId`, `IsLookingForBand` |
| `OPENING_CREATED` | Info | `OpeningId`, `BandId`, `Instrument` |
| `OPENING_CLOSED` | Info | `OpeningId`, `Filled` |
| `OPENING_DELETED` | Info | `OpeningId`, `BandId` |
| `OPENING_INTEREST_SENT` | Info | `OpeningId`, `BandId`, `InterestId` |

O convite feito a partir do interesse loga `BAND_INVITATION_SENT` no contexto `Bands`.

## Roteiro de verificação

O script `ensaio-api.mjs` (scratchpad da sessão que criou o módulo, fora do repositório) cobre os passos abaixo. Em 15/10/2026 passaram as 47 checagens. Contas: A (admin, Owner da banda nova em Santo André) e C e D, novas e confirmadas pelo Mailpit; C mora em Santo André.

1. **Ficha.**
   - A ficha vazia vem com `exists: false` e os gêneros do perfil.
   - Erros: link de outro site → `INVALID_SAMPLE_LINK`; 6 instrumentos ou "outro" sem nome → `INVALID_MUSICIAN_INSTRUMENTS`.
   - Salvar → `MUSICIAN_PROFILE_SAVED`. Salvar de novo troca a disponibilidade e os gêneros.
2. **Interruptor.**
   - Desligado: D toma 404 na ficha, e C fica fora da vitrine.
   - Ligado: C aparece filtrando por baixo, `abc=true`, gênero e com gravação, mas não por bateria; o perfil traz `isLookingForBand`.
3. **Vaga.**
   - Erros: C abrindo → `BAND_FORBIDDEN`; descrição vazia → `INVALID_OPENING_DESCRIPTION`.
   - A abre → `OPENING_CREATED` com `canManage`, e o feed traz o post com `opening`.
   - C editando → `BAND_FORBIDDEN`; a 6ª vaga → `OPENING_LIMIT_REACHED`; a busca por baixo no ABC traz a vaga.
4. **Interesse.**
   - Erros: D sem ficha → `MUSICIAN_PROFILE_REQUIRED`; recado de 301 → `INVALID_INTEREST_MESSAGE`.
   - C manda → `OPENING_INTEREST_SENT`; repetir → `OPENING_INTEREST_DUPLICATE`.
   - A recebe o kind 28 com a vaga e a banda, e lista os interessados com o recado; C listando → `BAND_FORBIDDEN`.
   - A convida → `OPENING_INTEREST_INVITED`. C recebe a `BandInvite`, aceita e vira integrante.
5. **Fim.**
   - Fechar como preenchida → some da busca, e o post mostra o status.
   - Interesse ou renovação depois disso → `OPENING_NOT_OPEN`.
   - Apagar → a vaga, o post e a notificação somem.
