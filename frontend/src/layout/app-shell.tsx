import { Outlet } from 'react-router-dom'

import { BottomNav } from './bottom-nav'
import { NavSidebar } from './nav-sidebar'
import { NotificationsProvider } from './notifications-provider'
import { ShellRailProvider } from './shell-rail'
import { useShellRailNode } from './use-shell-rail'
import { TopBar } from './top-bar'

function ShellBody() {
  const rail = useShellRailNode()

  return (
    <div className="flex min-h-dvh">
      <NavSidebar />
      <div className="flex min-w-0 flex-1 flex-col">
        <TopBar />
        <div className="mx-auto flex w-full max-w-7xl flex-1 gap-8 px-4 pt-6 pb-24 md:px-6 md:pb-10">
          <main className="min-w-0 flex-1">
            <Outlet />
          </main>
          {rail ? <aside className="hidden w-80 shrink-0 xl:block">{rail}</aside> : null}
        </div>
      </div>
      <BottomNav />
    </div>
  )
}

export function AppShell() {
  return (
    <NotificationsProvider>
      <ShellRailProvider>
        <ShellBody />
      </ShellRailProvider>
    </NotificationsProvider>
  )
}
