export type FeedbackSink = {
  error(code: string, backendMessage: string): void
  success(code: string, backendMessage: string): void
}

const noopFeedback: FeedbackSink = {
  error() {},
  success() {},
}

let currentSink: FeedbackSink = noopFeedback

export function setFeedbackSink(sink: FeedbackSink) {
  currentSink = sink
}

export const feedback: FeedbackSink = {
  error: (code, backendMessage) => currentSink.error(code, backendMessage),
  success: (code, backendMessage) => currentSink.success(code, backendMessage),
}
