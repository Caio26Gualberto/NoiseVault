const keyFor = (userId: number) => `app-welcome-seen:${userId}`

export function hasSeenWelcome(userId: number): boolean {
  try {
    return localStorage.getItem(keyFor(userId)) === 'true'
  } catch {
    return true
  }
}

export function markWelcomeSeen(userId: number) {
  try {
    localStorage.setItem(keyFor(userId), 'true')
  } catch {
    return
  }
}
