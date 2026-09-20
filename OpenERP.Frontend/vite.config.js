import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

export default defineConfig({
  plugins: [vue()],
  base: '/vue-login/',
  build: {
    outDir: '../OpenERP.Web/wwwroot/vue-login',
    emptyOutDir: true,
    rollupOptions: {
      output: {
        entryFileNames: 'assets/login.js',
        chunkFileNames: 'assets/[name].js',
        assetFileNames: 'assets/[name][extname]',
      },
    },
  },
})
