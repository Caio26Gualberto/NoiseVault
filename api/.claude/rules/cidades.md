---
paths:
  - "**/NoiseVault.Application/Cities/**"
  - "**/NoiseVault.Infra.Data/Repositories/Cities/**"
  - "**/NoiseVault.Infra.Data/Context/Seeding/**"
  - "**/NoiseVault.Api/Controllers/CitiesController.cs"
  - "**/NoiseVault.Domain/Entities/City.cs"
---

# Decisões — Cidades

Decisão do usuário em 14/10/2026: o site passou a servir à música em geral, então a lista fechada do ABC (`EAbcCity`: as 7 cidades, São Paulo e "Outra") saiu. **O núcleo e a origem do projeto continuam sendo o ABC**: a marca e os textos não mudaram, só deixou de existir a trava de cidade.

## Modelo

- **`City`** (`Id` = código IBGE do município, `Name`, `Uf`, `SearchName`): os **5.571 municípios** do IBGE. Não herda `EntityBase`: é dado de referência, sem autor, sem soft delete, sem auditoria. O repositório é independente (`ICityRepository`).
- `SearchName` é o nome em minúsculas e sem acento, gravado pelo seed, para a busca achar "sao bernardo" e "São Bernardo".
- **`User`, `Band`, `Event` e `ArchiveEntry`** têm `CityId?` (FK `Restrict`) e `CityName?`:
  - `CityId` é a cidade brasileira;
  - `CityName` (2 a 80) é **só para fora do Brasil**: "Lisboa, Portugal", o festival lá fora. Decisão do usuário: IBGE mais texto livre para o exterior;
  - os dois juntos → `INVALID_CITY`. Nenhum → sem cidade (perfil, banda, mural) ou `CITY_REQUIRED` (evento: todo show acontece em algum lugar).
- A saída leva `city: { id, name, uf } | null` e `cityName`. O front mostra "Santo André, SP" ou o texto livre.

## De onde vêm os dados

- Snapshot da API de localidades do IBGE (`/api/v1/localidades/municipios?view=nivelado`, baixado em 14/10/2026) em `Infra.Data/Context/Seeding/ibge-municipios.json`, como `[id, nome, uf]`, embutido no assembly.
- **Coordenadas** (`City.Latitude`/`Longitude`, o centro do município): snapshot `Infra.Data/Context/Seeding/municipios-coordenadas.json` (`[id, latitude, longitude]`), tirado em 15/10/2026 do conjunto público `kelvins/municipios-brasileiros` (licença MIT), ligado pelo mesmo código do IBGE. Cobre os 5.571 municípios. O seed preenche as coordenadas que estiverem nulas a cada boot. É o que a distância do Ensaio usa (`ensaio.md`).
- **A aplicação não chama o IBGE em tempo de execução**: o seed insere os municípios que faltam a cada boot, em todo ambiente (inclusive produção, porque perfil sem cidade não pode depender de dado de dev). Município novo do IBGE = baixar o snapshot de novo e reiniciar.
- **Migração do enum** (`ReplaceCityEnumWithIbge`): a migration cria a tabela com as 8 cidades que o enum conhecia (os 7 municípios do ABC e São Paulo, pelos códigos 3547809, 3548708, 3548807, 3513801, 3529401, 3543303, 3544103 e 3550308), converte os valores gravados e só então apaga a coluna `City`. O "Outra" (8) vira `CityId` nulo, e o `CityName` digitado fica como texto livre. O seed completa os outros 5.563 no boot seguinte.

## Busca

`GET /api/cities?search=&page=&pageSize=` (qualquer pessoa logada) → `CITIES_FOUND`, paginado.
- O termo é normalizado do mesmo jeito do `SearchName` e precisa de pelo menos 2 letras; menos que isso devolve lista vazia, sem varrer a tabela.
- Ordem: quem **começa** com o termo vem antes de quem só contém; dentro de cada grupo, **as 7 cidades do ABC vêm primeiro** (`CityRules.AbcCityIds`), porque o núcleo do projeto continua sendo o ABC; depois nome e UF, com desempate por `Id`. "sao bern" traz São Bernardo do Campo antes de São Bernardino (SC), e "Santo" traz Santo André antes dos outros Santos.
- **`GET /api/cities/featured`** devolve as 7 cidades do ABC, na ordem de `CityRules.AbcCityIds`, com o code `CITIES_FOUND`. É o que o seletor de cidade mostra antes de a pessoa digitar (pedido do usuário em 15/10/2026). Não tem rate limit próprio: é uma lista fixa, e o front guarda depois da primeira chamada.
- Limite `city-search` 60/min por conta, como a busca de pessoas: o campo busca enquanto a pessoa digita.

## Quem valida

`ICityAppService.Resolve(cityId, cityName, required)` é o único lugar que confere cidade, usado por perfil, banda, evento e mural (dono da entidade `City`, injetado nos outros, `CLAUDE.md`). Devolve o par normalizado ou o erro: `INVALID_CITY` (id inexistente ou os dois preenchidos), `INVALID_CITY_NAME` (texto fora de 2–80), `CITY_REQUIRED` (só o evento pede). Os codes antigos `INVALID_EVENT_CITY_NAME` e `INVALID_ARCHIVE_CITY_NAME` saíram.

## Filtros

Eventos e mural filtram por `cityId`. A busca textual de cada um continua olhando o `CityName` e passa a olhar também o nome da cidade do IBGE.

## Roteiro de verificação

1. Boot em banco vazio → `SELECT count(*) FROM "Cities"` = 5571.
2. `GET /api/cities?search=sao bern` → São Bernardo do Campo (SP) primeiro; `search=s` → lista vazia.
3. `PUT /api/profile` com `cityId: 3547809` → `city: { name: "Santo André", uf: "SP" }`; `cityId: 1` → `INVALID_CITY`; `cityId` e `cityName` juntos → `INVALID_CITY`; `cityName: "Lisboa, Portugal"` → aceito.
4. Evento sem cidade → `CITY_REQUIRED`; com Recife (2611606) → lista por `cityId=2611606` acha; busca "recife" acha.
5. Mural com `cityName` de fora → aceito; `cityId=3547809` filtra.
