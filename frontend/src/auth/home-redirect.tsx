import { Navigate } from 'react-router-dom'

import { homePath, visitorHomePath } from './routes'
import { RequireAuth } from './require-auth'
import { useIsVisitor } from './use-require-login'

export function HomeRedirect() {
  const visitor = useIsVisitor()

  return (
    <RequireAuth allowVisitor>
      <Navigate to={visitor ? visitorHomePath : homePath} replace />
    </RequireAuth>
  )
}
