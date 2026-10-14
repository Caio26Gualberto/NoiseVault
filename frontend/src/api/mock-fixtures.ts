import type { ProfileSummaryDto } from '@/pages/perfil/services/perfil-service'

export const avatar = (seed: string) => `https://api.dicebear.com/9.x/thumbs/svg?seed=${seed}&backgroundType=gradientLinear`
export const picture = (seed: string, w = 1200, h = 400) => `https://picsum.photos/seed/${seed}/${w}/${h}`

export const people: ProfileSummaryDto[] = [
  { id: 2, name: 'Caio Gualberto', nickname: 'caio', avatarUrl: avatar('caio') },
  { id: 3, name: 'Marina Vitorino', nickname: 'marina.v', avatarUrl: avatar('marina') },
  { id: 4, name: 'Rafael Tenório', nickname: 'rafa_tenorio', avatarUrl: avatar('rafa') },
  { id: 5, name: 'Bia Cordeiro', nickname: 'bia.cordeiro', avatarUrl: avatar('bia') },
  { id: 6, name: 'Dudu Lacerda', nickname: 'dudulacerda', avatarUrl: avatar('dudu') },
  { id: 7, name: 'Lívia Sampaio', nickname: 'livia', avatarUrl: avatar('livia') },
]
