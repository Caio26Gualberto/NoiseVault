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
  - `ReachKm` (até onde a pessoa topa ir, de 1 a 500 km, ou nulo para "onde precisar"; ver "Distância");
  - `HasGear`, `HasTransport`, `HasRehearsalSpace`;
  - `Influences` (até 300).
  - `PhotoKey?`: a **foto do flyer**, uma foto da pessoa tocando ou de show, separada do avatar. Sem ela, o front usa o avatar do perfil.
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

## Foto do flyer

- `POST /api/musicians/me/photo` (multipart, campo `file`) e `DELETE /api/musicians/me/photo` devolvem a ficha, com os codes `MUSICIAN_PHOTO_UPDATED` e `MUSICIAN_PHOTO_REMOVED`.
- Exige a ficha já salva (`MUSICIAN_PROFILE_REQUIRED`): a foto é da ficha, não do perfil.
- O preset é o `Flyer` (`armazenamento.md`): até 10 MB, no mínimo 300×300, saída WebP com até 1200 no lado maior, sem corte. O flyer recorta com `object-cover` na tela.
- A chave é `users/{userId}/musician/{guid}.webp`. A ordem é a de sempre (`ImageReplacement`): sobe, grava, apaga a antiga.
- Rate limit `profile-upload` (20/h, compartilhado com avatar e capa) e `RequestSizeLimit` de 11 MB.
- `MusicianProfileDto.photoUrl` e `MusicianCardDto.photoUrl` trazem a URL pública. Log `MUSICIAN_PHOTO_UPDATED`.

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
- **Artista solo não abre vaga** (`SOLO_ARTIST_NO_MEMBERS`, `bandas.md`): a vaga termina em convite de integrante, e o solo não tem integrantes. Banda com vaga aberta não vira solo (`BAND_HAS_OPEN_OPENINGS`).
- **Quem mexe**: os Owners da banda, conferidos no `WHERE` (`Band.Members.Any(Owner == eu)`). Quando a consulta volta vazia, uma segunda consulta separa 404 `OPENING_NOT_FOUND` de 403 `BAND_FORBIDDEN`.
- **Editar, fechar e renovar** só valem com a vaga `Open` (`OPENING_NOT_OPEN`). Fechar escolhe entre "preenchida" (`filled: true`) e "encerrada".
- **Leitura**:
  - a busca traz só as abertas e não vencidas, de bandas não apagadas;
  - a lista da banda traz tudo para os Owners e só as abertas para os outros;
  - o `GET /api/openings/{id}` mostra a vaga em qualquer status, para o link do post e da notificação não quebrar.
- **Filtros** (os mesmos na vitrine e nas vagas, paginados):
  - `instrument`;
  - `maxKm` e `nearCityId`, a distância (abaixo). Substituíram o `cityId` e o `abc=true` da primeira versão, por decisão do usuário em 15/10/2026 ("igual o Tinder, por quilometragem");
  - `genre`. É o da ficha na vitrine e o da banda na vaga;
  - `withSamples`, só na vitrine.

## Distância, com a localização protegida

Pedido do usuário em 15/10/2026: filtro por quilômetros, com a localização do navegador como opção, "com toda segurança possível".

- **Pontos**:
  - a pessoa é a localização aproximada dela (`User.ApproxLatitude`/`ApproxLongitude`) ou, sem ela, o centro da cidade do perfil;
  - a banda é sempre o centro da cidade dela;
  - quem não tem cidade brasileira nem localização fica fora quando há raio, e aparece em "qualquer distância", sem distância no card.
- **Coordenadas das cidades**: `City.Latitude`/`Longitude`, de um snapshot próprio (`cidades.md`).
- **Ponto de partida** (`IMusicianAppService.ResolveOrigin`): o centro de `nearCityId` quando vem; senão o ponto de quem está vendo. **Nunca um ponto arbitrário vindo do cliente**: aceitar "calcule a partir daqui" com coordenadas livres permitiria a trilateração (medir a mesma pessoa a partir de três pontos e achar onde ela está). O `nearCityId` só aceita centros de município.
- **Filtro e ordem no banco**:
  - o raio é uma conta equiretangular (`dx² + dy² <= maxKm²`, com o cosseno da latitude de partida calculado antes), que o EF traduz para SQL sem função trigonométrica por linha;
  - com raio, a lista vem do mais perto para o mais longe; sem raio, a ordem é a de sempre;
  - `maxKm` vai de 1 a 500 (`GeoRules.MaxSearchKm`).
- **Mão dupla (só na vitrine)**: com raio, só aparece quem topa ir até o ponto de partida. A conta é a mesma, contra o `ReachKm` da ficha; quem tem `ReachKm` nulo vai a qualquer lugar. A própria ficha não é filtrada pela mão dupla.
- **Distância exibida em faixas** (`GeoRules.Bucket`): 2, 5, 10, 15, 20, 25, 30, 40, 50, 75 e 100 km, e acima disso arredondada para os 50 mais próximos. O `distanceKm` do DTO já sai na faixa; a distância exata nunca sai da API. A conta é a haversine, em memória, só para a página.
- **`ReachKm` substituiu o `Reach`** (minha cidade, ABC, Grande SP, onde precisar). A migration `AddDistance` converteu 0 → 10 km, 1 → 30, 2 → 60 e 3 → sem limite. Vale de 1 a 500 ou nulo (`INVALID_MUSICIAN_PREFERENCES`), e o padrão da ficha nova é 25.

### A localização aproximada

- `PUT /api/profile/location { latitude, longitude }` e `DELETE /api/profile/location`, no `ProfileAppService` (dono do `User`), devolvem o `ProfileDto` com `hasApproxLocation`. Codes: `APPROX_LOCATION_SAVED`, `APPROX_LOCATION_REMOVED` e `INVALID_LOCATION`.
- **A coordenada exata nunca é gravada**: o servidor arredonda para a grade de 0,01° (`GeoRules.Snap`, cerca de 1,1 km). O front já manda arredondada, então a exata nem sai do aparelho; o arredondamento do servidor é a segunda trava.
- **Nenhuma resposta devolve coordenada**, nem para o dono: só o `hasApproxLocation`. O perfil público não sabe que ela existe.
- **Trocar de lugar tem limite**: `location-update`, 10 por dia por conta, contra quem tenta se mover para trilaterar.
- **Log**: `APPROX_LOCATION_SAVED` só com o `user=`, nunca com a coordenada (`observabilidade.md`).
- **Apagar** zera os três campos na hora, e a distância volta a contar pela cidade. A localização é dado pessoal (LGPD): é opt-in, explicada na tela e removível a qualquer momento.

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
| `GET /api/musicians` (vitrine, paginada, `instrument`, `maxKm`, `nearCityId`, `genre`, `withSamples`) | `MUSICIANS_FOUND` |
| `GET /api/musicians/me` | `MUSICIAN_PROFILE_FOUND` (com `exists`) |
| `PUT /api/musicians/me` | `MUSICIAN_PROFILE_SAVED` |
| `POST /api/musicians/me/looking { isLookingForBand }` | `MUSICIAN_LOOKING_CHANGED` |
| `POST` / `DELETE /api/musicians/me/photo` | `MUSICIAN_PHOTO_UPDATED` / `MUSICIAN_PHOTO_REMOVED` |
| `PUT` / `DELETE /api/profile/location` | `APPROX_LOCATION_SAVED` / `APPROX_LOCATION_REMOVED` |
| `GET /api/musicians/{nickname}` | `MUSICIAN_PROFILE_FOUND` |
| `GET /api/openings` (paginada, `instrument`, `maxKm`, `nearCityId`, `genre`) | `OPENINGS_FOUND` |
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
| `location-update` | 10/dia por conta | `PUT /api/profile/location` |
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
   - Ligado: C aparece filtrando por baixo, gênero e com gravação, mas não por bateria; o perfil traz `isLookingForBand`.
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
6. **Distância.** O script `distance-api.mjs` (mesmo scratchpad) passou nas 24 checagens em 15/10/2026, com três contas: quem vê em Santo André, uma em São Bernardo e uma em Campinas.
   - O alcance aceita de 1 a 500.
   - Até 25 km traz São Bernardo e não traz Campinas; até 200 km traz Campinas com `distanceKm: 100`, do mais perto para o mais longe.
   - Com alcance de 1 km, São Bernardo some do raio de 25 km (mão dupla), mas aparece em "qualquer distância". Perto de Campinas até 25 km traz Campinas e não São Bernardo.
   - Localização: latitude 120 → `INVALID_LOCATION`. Salvar não devolve coordenada, e o perfil público não a expõe. Apagar volta a contar pela cidade.
   - A vaga de Campinas fica fora de 25 km e dentro de 200 km, com a distância também no detalhe.
