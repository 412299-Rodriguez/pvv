/// <reference types="vite/client" />

interface ImportMetaEnv {
  readonly VITE_BFF_BASE_URL: string
  readonly VITE_TURNSTILE_SITE_KEY: string
  readonly VITE_COMPANY_TOKEN: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}
