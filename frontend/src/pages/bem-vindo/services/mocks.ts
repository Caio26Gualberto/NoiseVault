import { delay } from '@/api/mock-delay'

import type { AvatarOptionDto } from './onboarding-service'

const seeds = ['riff', 'blast', 'doom', 'thrash', 'solo', 'pit', 'amp', 'stage']

const fixtures: AvatarOptionDto[] = seeds.map((seed) => ({
  id: seed,
  url: `https://api.dicebear.com/9.x/thumbs/svg?seed=${seed}&backgroundType=gradientLinear`,
}))

export async function getAvatarOptions(): Promise<AvatarOptionDto[]> {
  await delay()
  return fixtures
}
