import { useIsVisitor } from '../auth/use-require-login'
import { NotificationsBell } from './notifications-bell'
import { PeopleSearch } from './people-search'
import { UserMenu } from './user-menu'
import { VisitorActions } from './visitor-actions'
import { Wordmark } from './wordmark'

export function TopBar() {
  const visitor = useIsVisitor()

  return (
    <header className="sticky top-0 z-30 flex h-[calc(3.5rem+env(safe-area-inset-top))] items-center pt-[env(safe-area-inset-top)] gap-3 border-b bg-background/95 px-4 backdrop-blur md:px-6">
      <div className="md:hidden">
        <Wordmark size="sm" />
      </div>

      {visitor ? (
        <div className="ml-auto">
          <VisitorActions />
        </div>
      ) : (
        <>
          <PeopleSearch />

          <div className="ml-auto flex items-center gap-1 md:ml-0">
            <NotificationsBell />
            <div className="md:hidden">
              <UserMenu compact />
            </div>
          </div>
        </>
      )}
    </header>
  )
}
