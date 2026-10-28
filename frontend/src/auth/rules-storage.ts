export const rulesVersion = '2026-10-28'

const keyFor = (userId: number) => `app-rules-accepted:${userId}`

export function hasAcceptedRules(userId: number): boolean {
  try {
    return localStorage.getItem(keyFor(userId)) === rulesVersion
  } catch {
    return true
  }
}

export function markRulesAccepted(userId: number) {
  try {
    localStorage.setItem(keyFor(userId), rulesVersion)
  } catch {
    return
  }
}
