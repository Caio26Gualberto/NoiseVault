import type { TFunction } from 'i18next'

export type ReportKind = 'bug' | 'suggestion'

const kindByParam: Record<string, ReportKind> = { bug: 'bug', sugestao: 'suggestion' }

export const reportKindParam: Record<ReportKind, string> = { bug: 'bug', suggestion: 'sugestao' }

export function reportKindFromParam(value: string | null): ReportKind | null {
  return value ? (kindByParam[value] ?? null) : null
}

export const reportAnswerMinLength = 10
export const bugAnswerMaxLength = 1500
export const ideaMaxLength = 3000
export const reasonMaxLength = 1500

export function composeDescription(sections: [heading: string, text: string][]) {
  return sections
    .map(([heading, text]) => [heading, text.trim()] as const)
    .filter(([, text]) => text.length > 0)
    .map(([heading, text]) => `## ${heading}\n${text}`)
    .join('\n\n')
}

export function composeBugDescription(t: TFunction, values: { doing: string; expected: string; happened: string }) {
  return composeDescription([
    [t('compose.doing'), values.doing],
    [t('compose.expected'), values.expected],
    [t('compose.happened'), values.happened],
  ])
}

export function composeSuggestionDescription(t: TFunction, values: { idea: string; reason: string }) {
  return composeDescription([
    [t('compose.idea'), values.idea],
    [t('compose.reason'), values.reason],
  ])
}
