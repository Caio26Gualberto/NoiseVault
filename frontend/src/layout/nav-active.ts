import { matchPath } from 'react-router-dom'

import { navSections } from './nav-config'

export function resolveActivePath(pathname: string): string | null {
  const leaves = navSections.flatMap((section) => section.items)

  const exact = leaves.find((leaf) => matchPath({ path: leaf.path, end: true }, pathname))
  if (exact) return exact.path

  const owner = leaves.find((leaf) =>
    leaf.subRoutes?.some((pattern) => matchPath({ path: pattern, end: true }, pathname)),
  )

  return owner?.path ?? null
}
