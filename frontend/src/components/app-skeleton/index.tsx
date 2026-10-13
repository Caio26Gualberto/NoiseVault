import { Skeleton } from '@/components/ui/skeleton'
import { cn } from '@/lib/utils'

type Preset = 'page' | 'card' | 'list' | 'profile' | 'lines'

type AppSkeletonProps = {
  preset?: Preset
  count?: number
  className?: string
}

function CardSkeleton() {
  return (
    <div className="flex flex-col gap-3 rounded-xl border bg-card p-4">
      <div className="flex items-center gap-3">
        <Skeleton className="size-10 rounded-full" />
        <div className="flex flex-1 flex-col gap-2">
          <Skeleton className="h-3.5 w-1/3" />
          <Skeleton className="h-3 w-1/5" />
        </div>
      </div>
      <Skeleton className="h-3.5 w-full" />
      <Skeleton className="h-3.5 w-5/6" />
      <Skeleton className="aspect-video w-full rounded-lg" />
    </div>
  )
}

function ListSkeleton() {
  return (
    <div className="flex items-center gap-3 py-3">
      <Skeleton className="size-9 rounded-full" />
      <div className="flex flex-1 flex-col gap-2">
        <Skeleton className="h-3.5 w-2/5" />
        <Skeleton className="h-3 w-3/5" />
      </div>
    </div>
  )
}

function ProfileSkeleton() {
  return (
    <div className="flex flex-col gap-4">
      <Skeleton className="aspect-[3/1] w-full rounded-xl" />
      <div className="flex items-end gap-4 px-4">
        <Skeleton className="-mt-12 size-24 rounded-full ring-4 ring-background" />
        <div className="flex flex-1 flex-col gap-2 pb-2">
          <Skeleton className="h-5 w-1/3" />
          <Skeleton className="h-3.5 w-1/5" />
        </div>
      </div>
    </div>
  )
}

export function AppSkeleton({ preset = 'lines', count = 3, className }: AppSkeletonProps) {
  if (preset === 'page') {
    return (
      <div className={cn('flex min-h-dvh items-center justify-center', className)} role="status">
        <div className="flex items-center gap-3">
          <Skeleton className="size-3 rounded-full" />
          <Skeleton className="size-3 rounded-full [animation-delay:120ms]" />
          <Skeleton className="size-3 rounded-full [animation-delay:240ms]" />
        </div>
      </div>
    )
  }

  if (preset === 'profile') {
    return (
      <div className={className} role="status">
        <ProfileSkeleton />
      </div>
    )
  }

  const items = Array.from({ length: count }, (_, index) => index)

  return (
    <div className={cn('flex flex-col', preset === 'card' ? 'gap-4' : 'gap-1', className)} role="status">
      {items.map((index) =>
        preset === 'card' ? (
          <CardSkeleton key={index} />
        ) : preset === 'list' ? (
          <ListSkeleton key={index} />
        ) : (
          <Skeleton key={index} className={cn('h-3.5', index % 3 === 2 ? 'w-2/3' : 'w-full')} />
        ),
      )}
    </div>
  )
}
