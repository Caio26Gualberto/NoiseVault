import type { ComponentProps } from 'react'
import { Link } from 'react-router-dom'

import { ensaioUrl, isExternalUrl } from './frame'

type EnsaioLinkProps = Omit<ComponentProps<'a'>, 'href'> & {
  to: string
}

export function EnsaioLink({ to, ...props }: EnsaioLinkProps) {
  const href = ensaioUrl(to)

  if (isExternalUrl(href)) return <a href={href} {...props} />

  return <Link to={href} {...props} />
}
