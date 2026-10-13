import i18n from 'i18next'
import { initReactI18next } from 'react-i18next'

import api from './pt-BR/api.json'
import auth from './pt-BR/auth.json'
import common from './pt-BR/common.json'
import conta from './pt-BR/conta.json'
import feed from './pt-BR/feed.json'
import layout from './pt-BR/layout.json'

export const defaultLanguage = 'pt-BR'

const namespaces = {
  common,
  api,
  layout,
  auth,
  conta,
  feed,
}

i18n.use(initReactI18next).init({
  lng: defaultLanguage,
  fallbackLng: defaultLanguage,
  defaultNS: 'common',
  ns: Object.keys(namespaces),
  resources: {
    [defaultLanguage]: namespaces,
  },
  interpolation: { escapeValue: false },
})

export function apiMessage(code: string, backendMessage: string): string {
  if (code && i18n.exists(`api:${code}`)) {
    return i18n.t(`api:${code}`)
  }
  if (backendMessage) return backendMessage
  return i18n.t('api:UNKNOWN_ERROR')
}

export default i18n
