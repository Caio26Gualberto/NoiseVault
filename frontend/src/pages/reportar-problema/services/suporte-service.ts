import * as mocks from './mocks'

export const IssueLabel = { bug: 0, suggestion: 1, content: 2, account: 3 } as const
export type IssueLabel = (typeof IssueLabel)[keyof typeof IssueLabel]

export type ReportProblemInputDto = {
  label: IssueLabel
  title: string
  description: string
  pageUrl: string | null
}

export type ReportProblemResultDto = {
  protocol: string
}

export const reportProblem: (input: ReportProblemInputDto) => Promise<ReportProblemResultDto> = mocks.reportProblem
