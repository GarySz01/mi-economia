import tailwindcss from '@tailwindcss/vite'
import react from '@vitejs/plugin-react'
import { defineConfig } from 'vite'

// https://vite.dev/config/
export default defineConfig({
  plugins: [react(), tailwindcss()],
  server: {
    // En desarrollo, /api se reenvía a la API .NET: el navegador ve un solo origen y no hace falta CORS.
    proxy: {
      '/api': 'http://localhost:5170',
    },
  },
})
