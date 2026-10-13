import { delay } from '@/api/mock-delay'

import type { ReportProblemInputDto, ReportProblemResultDto } from './suporte-service'

export async function reportProblem(input: ReportProblemInputDto): Promise<ReportProblemResultDto> {
  await delay()
  void input
  return { protocol: `HB-${Date.now().toString(36).toUpperCase().slice(-6)}` }
}
