import { defineConfig } from 'vite'
import react from '@vitejs/plugin-react'
import { fileURLToPath, URL } from 'node:url'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react()],
  resolve: {
    alias: {
      // FSD-friendly absolute imports: `@/shared/...`, `@/entities/...`, etc.
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
  server: {
    port: 5173,
    strictPort: true,
    // Mercado Pago refuses to store a back_url on localhost, so returning from a
    // real checkout requires the portal to be reachable at a public address. In
    // development that is a cloudflared quick tunnel, whose hostname is random on
    // every run — hence the wildcard rather than a fixed host. Dev server only.
    allowedHosts: ['.trycloudflare.com'],
  },
})
