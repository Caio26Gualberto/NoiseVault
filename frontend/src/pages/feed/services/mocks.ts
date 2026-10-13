import { delay, mockPage } from '@/api/mock-delay'
import type { PagedRequest, PagedResult } from '@/api/types'
import { bandSummaries, people, picture } from '@/api/mock-fixtures'

import type { CommentDto, FeedQuery, PostDto } from './feed-service'

function ago(hours: number): string {
  return new Date(Date.now() - hours * 60 * 60 * 1000).toISOString()
}

let nextId = 100

const posts: PostDto[] = [
  {
    id: 1,
    author: people[0],
    band: bandSummaries[0],
    content: 'Gravação do disco novo começou hoje. Primeira faixa de bateria fechada em duas tomadas. O Rafa tá possuído.',
    media: [
      { id: 1, kind: 'photo', url: picture('post-1a', 1200, 900) },
      { id: 2, kind: 'photo', url: picture('post-1b', 1200, 900) },
      { id: 3, kind: 'photo', url: picture('post-1c', 1200, 900) },
    ],
    likeCount: 42,
    commentCount: 2,
    likedByMe: false,
    createdAt: ago(2),
  },
  {
    id: 2,
    author: people[1],
    band: bandSummaries[1],
    content: 'Doom Sunday confirmado para o próximo fim de semana na Casa Amarela. Entrada franca, tragam os amigos que não aguentam blast beat.',
    media: [{ id: 4, kind: 'photo', url: picture('post-2', 1600, 900) }],
    likeCount: 18,
    commentCount: 0,
    likedByMe: true,
    createdAt: ago(7),
  },
  {
    id: 3,
    author: people[2],
    band: null,
    content: 'Fotos do Grind no Galpão da semana passada. Quem reconhecer alguém nas fotos me marca que eu mando o arquivo em alta.',
    media: [
      { id: 5, kind: 'photo', url: picture('post-3a', 1200, 900) },
      { id: 6, kind: 'photo', url: picture('post-3b', 1200, 900) },
      { id: 7, kind: 'photo', url: picture('post-3c', 1200, 900) },
      { id: 8, kind: 'photo', url: picture('post-3d', 1200, 900) },
      { id: 9, kind: 'photo', url: picture('post-3e', 1200, 900) },
    ],
    likeCount: 97,
    commentCount: 1,
    likedByMe: false,
    createdAt: ago(26),
  },
  {
    id: 4,
    author: people[3],
    band: null,
    content: 'Procuro baixista para projeto de thrash old school em Diadema. Ensaio quinta à noite. Chama no direct.',
    media: [],
    likeCount: 9,
    commentCount: 0,
    likedByMe: false,
    createdAt: ago(50),
  },
  {
    id: 5,
    author: people[4],
    band: bandSummaries[2],
    content: 'Clipe novo no ar. Gravado inteiro no Tamanduateí com uma câmera emprestada e muita boa vontade.',
    media: [{ id: 10, kind: 'video', url: picture('post-5', 1600, 900) }],
    likeCount: 133,
    commentCount: 0,
    likedByMe: false,
    createdAt: ago(80),
  },
  {
    id: 6,
    author: people[5],
    band: null,
    content: 'Alguém tem o setlist do show da Fúria no Porão do ano passado? Quero montar uma playlist.',
    media: [],
    likeCount: 4,
    commentCount: 0,
    likedByMe: false,
    createdAt: ago(120),
  },
]

const comments: Record<number, CommentDto[]> = {
  1: [
    { id: 1, author: people[2], text: 'Possuído e com dor no braço, mas feliz.', createdAt: ago(1.5) },
    { id: 2, author: people[4], text: 'Quando sai o single?', createdAt: ago(1) },
  ],
  3: [{ id: 3, author: people[0], text: 'A terceira é a melhor foto que já tiraram de mim.', createdAt: ago(20) }],
}

export async function getFeed(query: FeedQuery): Promise<PagedResult<PostDto>> {
  await delay()
  const scoped = query.scope === 'following' ? posts.filter((p) => p.band !== null) : posts
  return mockPage(scoped, query)
}

export async function getPost(id: number): Promise<PostDto | null> {
  await delay()
  return posts.find((p) => p.id === id) ?? null
}

export async function toggleLike(id: number): Promise<void> {
  await delay(120)
  const post = posts.find((p) => p.id === id)
  if (!post) return
  post.likedByMe = !post.likedByMe
  post.likeCount += post.likedByMe ? 1 : -1
}

export async function getComments(id: number, query: PagedRequest): Promise<PagedResult<CommentDto>> {
  await delay(200)
  return mockPage(comments[id] ?? [], query)
}

export async function addComment(id: number, text: string): Promise<void> {
  await delay(150)
  const list = (comments[id] ??= [])
  list.push({ id: nextId++, author: people[0], text, createdAt: new Date().toISOString() })
  const post = posts.find((p) => p.id === id)
  if (post) post.commentCount += 1
}

export async function createPost(content: string): Promise<PostDto> {
  await delay()
  const post: PostDto = {
    id: nextId++,
    author: people[0],
    band: null,
    content,
    media: [],
    likeCount: 0,
    commentCount: 0,
    likedByMe: false,
    createdAt: new Date().toISOString(),
  }
  posts.unshift(post)
  return post
}
