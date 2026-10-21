type Pending = {
  task: () => Promise<unknown>
  resolve: (value: unknown) => void
  reject: (reason: unknown) => void
}

export function createLimiter(max: number) {
  let active = 0
  const queue: Pending[] = []

  const next = () => {
    if (active >= max) return
    const pending = queue.shift()
    if (!pending) return
    active += 1
    pending
      .task()
      .then(pending.resolve, pending.reject)
      .finally(() => {
        active -= 1
        next()
      })
  }

  return <T>(task: () => Promise<T>) =>
    new Promise<T>((resolve, reject) => {
      queue.push({ task, resolve: resolve as (value: unknown) => void, reject })
      next()
    })
}
