import { delay, mockPage } from '@/api/mock-delay'
import type { PagedRequest, PagedResult } from '@/api/types'
import { AbcCity } from '@/pages/perfil/cities'
import { MusicGenre } from '@/pages/perfil/genres'
import { bandSummaries, people, picture } from '@/api/mock-fixtures'

import { EventStatus } from '../event-status'
import type { EventCommentDto, EventDto, EventInputDto, EventListQuery } from './eventos-service'

function at(daysFromNow: number, hour: number): string {
  const date = new Date()
  date.setDate(date.getDate() + daysFromNow)
  date.setHours(hour, 0, 0, 0)
  return date.toISOString()
}

let nextId = 100

const events: EventDto[] = [
  {
    id: 1,
    title: 'Noite do Thrash no Porão',
    description:
      'Três bandas, uma noite, zero descanso. Fúria de Aço fecha a noite com o set novo. Abertura com Via Anchieta e convidados de Diadema.\n\nEntrada com 1kg de alimento até 21h.',
    startsAt: at(3, 21),
    endsAt: at(4, 2),
    venue: 'Porão do Rock',
    city: AbcCity.santoAndre,
    bannerUrl: picture('event-thrash', 1600, 900),
    bannerPosition: 50,
    genres: [MusicGenre.thrash, MusicGenre.heavy],
    ticketsUrl: 'https://example.com/ingressos',
    price: 25,
    capacity: 180,
    status: EventStatus.published,
    organizer: people[0],
    bands: [bandSummaries[0], bandSummaries[3]],
    interestedCount: 84,
    interested: false,
  },
  {
    id: 2,
    title: 'Doom Sunday: tarde lenta',
    description: 'Serra Negra toca o disco inteiro. Som baixo, luz baixa, cerveja gelada. Entrada franca.',
    startsAt: at(9, 16),
    endsAt: null,
    venue: 'Casa Amarela',
    city: AbcCity.saoBernardo,
    bannerUrl: picture('event-doom', 1600, 900),
    bannerPosition: 60,
    genres: [MusicGenre.doom, MusicGenre.stoner],
    ticketsUrl: null,
    price: null,
    capacity: 80,
    status: EventStatus.published,
    organizer: people[1],
    bands: [bandSummaries[1]],
    interestedCount: 31,
    interested: true,
  },
  {
    id: 3,
    title: 'Grind no Galpão',
    description: 'Tamanduateí Blast + bandas de Mauá e Ribeirão. Sets de 20 minutos, intervalo de 5.',
    startsAt: at(15, 20),
    endsAt: at(16, 1),
    venue: 'Galpão 17',
    city: AbcCity.maua,
    bannerUrl: picture('event-grind', 1600, 900),
    bannerPosition: 40,
    genres: [MusicGenre.grindcore, MusicGenre.death],
    ticketsUrl: 'https://example.com/ingressos',
    price: 20,
    capacity: 250,
    status: EventStatus.published,
    organizer: people[2],
    bands: [bandSummaries[2]],
    interestedCount: 120,
    interested: false,
  },
  {
    id: 4,
    title: 'Festival Serra do Mar',
    description: 'Um dia inteiro de metal na vila de Paranapiacaba, com food trucks e feira de vinil.',
    startsAt: at(32, 14),
    endsAt: at(32, 23),
    venue: 'Vila de Paranapiacaba',
    city: AbcCity.santoAndre,
    bannerUrl: picture('event-festival', 1600, 900),
    bannerPosition: 50,
    genres: [MusicGenre.black, MusicGenre.folk, MusicGenre.heavy],
    ticketsUrl: 'https://example.com/ingressos',
    price: 60,
    capacity: 1200,
    status: EventStatus.published,
    organizer: people[2],
    bands: [bandSummaries[4], bandSummaries[0], bandSummaries[2]],
    interestedCount: 410,
    interested: false,
  },
  {
    id: 5,
    title: 'Ensaio aberto: Via Anchieta',
    description: 'Ensaio aberto no estúdio, 30 pessoas, hard rock dos anos 80 com cover e inédita.',
    startsAt: at(-5, 19),
    endsAt: null,
    venue: 'Estúdio Garagem 3',
    city: AbcCity.saoCaetano,
    bannerUrl: picture('event-ensaio', 1600, 900),
    bannerPosition: 50,
    genres: [MusicGenre.hardRock],
    ticketsUrl: null,
    price: null,
    capacity: 30,
    status: EventStatus.published,
    organizer: people[3],
    bands: [bandSummaries[3]],
    interestedCount: 18,
    interested: false,
  },
]

const comments: Record<number, EventCommentDto[]> = {
  1: [
    { id: 1, author: people[3], text: 'Vai ter venda de camiseta na porta?', createdAt: at(-1, 10) },
    { id: 2, author: people[0], text: 'Vai sim, tamanhos P ao GG.', createdAt: at(-1, 12) },
  ],
}

export async function getEvents(query: EventListQuery): Promise<PagedResult<EventDto>> {
  await delay()
  const search = query.search.trim().toLowerCase()
  const filtered = events
    .filter((event) => event.status === EventStatus.published)
    .filter((event) => (query.genre === null ? true : event.genres.includes(query.genre)))
    .filter((event) => (query.city === null ? true : event.city === query.city))
    .filter((event) => (search ? `${event.title} ${event.venue}`.toLowerCase().includes(search) : true))
    .sort((a, b) => new Date(a.startsAt).getTime() - new Date(b.startsAt).getTime())
  return mockPage(filtered, query)
}

export async function getEvent(id: number): Promise<EventDto | null> {
  await delay()
  return events.find((event) => event.id === id) ?? null
}

export async function getUpcomingEvents(limit = 3): Promise<EventDto[]> {
  await delay(200)
  const now = Date.now()
  return events
    .filter((event) => event.status === EventStatus.published && new Date(event.startsAt).getTime() >= now)
    .sort((a, b) => new Date(a.startsAt).getTime() - new Date(b.startsAt).getTime())
    .slice(0, limit)
}

export async function toggleInterest(id: number): Promise<void> {
  await delay(150)
  const event = events.find((e) => e.id === id)
  if (!event) return
  event.interested = !event.interested
  event.interestedCount += event.interested ? 1 : -1
}

export async function createEvent(input: EventInputDto): Promise<EventDto> {
  await delay()
  const event: EventDto = {
    id: nextId++,
    ...input,
    bannerUrl: picture(`event-${nextId}`, 1600, 900),
    bannerPosition: 50,
    status: EventStatus.published,
    organizer: people[0],
    bands: [],
    interestedCount: 0,
    interested: false,
  }
  events.unshift(event)
  return event
}

export async function updateEvent(id: number, input: EventInputDto): Promise<EventDto | null> {
  await delay()
  const event = events.find((e) => e.id === id)
  if (!event) return null
  Object.assign(event, input)
  return event
}

export async function getEventComments(id: number, query: PagedRequest): Promise<PagedResult<EventCommentDto>> {
  await delay(200)
  return mockPage(comments[id] ?? [], query)
}

export async function addEventComment(id: number, text: string): Promise<void> {
  await delay(150)
  const list = (comments[id] ??= [])
  list.push({ id: Date.now(), author: people[0], text, createdAt: new Date().toISOString() })
}
