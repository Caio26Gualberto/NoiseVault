import axios from 'axios'

export type PresignedPart = {
  partNumber: number
  url: string
}

export type UploadedPart = {
  partNumber: number
  eTag: string
}

export type UploadProgress = (loaded: number, total: number) => void

type UploadPartsOptions = {
  partSize: number
  onProgress: UploadProgress
  signal: AbortSignal
}

const concurrency = 4
const attemptsPerPart = 3

const storageClient = axios.create()

function wait(ms: number, signal: AbortSignal): Promise<void> {
  return new Promise((resolve, reject) => {
    const timer = setTimeout(resolve, ms)
    signal.addEventListener(
      'abort',
      () => {
        clearTimeout(timer)
        reject(signal.reason)
      },
      { once: true },
    )
  })
}

export async function uploadParts(file: File, parts: PresignedPart[], options: UploadPartsOptions): Promise<UploadedPart[]> {
  const loaded = new Map<number, number>()
  const uploaded: UploadedPart[] = []
  const queue = [...parts]

  const report = () => {
    let sum = 0
    for (const value of loaded.values()) sum += value
    options.onProgress(Math.min(sum, file.size), file.size)
  }

  const uploadPart = async (part: PresignedPart): Promise<UploadedPart> => {
    const start = (part.partNumber - 1) * options.partSize
    const blob = file.slice(start, Math.min(start + options.partSize, file.size))

    for (let attempt = 1; ; attempt++) {
      try {
        const response = await storageClient.put(part.url, blob, {
          signal: options.signal,
          headers: { 'Content-Type': 'application/octet-stream' },
          onUploadProgress: (event) => {
            loaded.set(part.partNumber, event.loaded)
            report()
          },
        })
        const eTag = response.headers.etag as string | undefined
        if (!eTag) throw new Error('missing-etag')
        loaded.set(part.partNumber, blob.size)
        report()
        return { partNumber: part.partNumber, eTag }
      } catch (error) {
        loaded.set(part.partNumber, 0)
        report()
        if (options.signal.aborted || attempt >= attemptsPerPart) throw error
        await wait(1000 * attempt, options.signal)
      }
    }
  }

  const worker = async () => {
    for (let part = queue.shift(); part; part = queue.shift()) {
      uploaded.push(await uploadPart(part))
    }
  }

  await Promise.all(Array.from({ length: Math.min(concurrency, parts.length) }, worker))

  return uploaded.sort((a, b) => a.partNumber - b.partNumber)
}
