import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import tailwindcss from '@tailwindcss/vite'
import { resolve } from 'node:path'

// https://vite.dev/config/
export default defineConfig({
  plugins: [vue(), tailwindcss()],
  resolve: {
    alias: {
      '~/': `${resolve(import.meta.dirname, 'src')}/`,
    },
  },
  server: {
    cors: false,
    proxy: {
      '/sounds': 'http://localhost:8740',
      '/api': {
        target: 'http://localhost:8740',
        ws: true,
      },
    }
  }
})