import { createApp } from 'vue'
import { createPinia } from 'pinia'
import './assets/main.css'
import App from './App.vue'
import createAppRouter from './router/index.js'

const app = createApp(App)
const pinia = createPinia()
const router = createAppRouter(pinia)

app.use(pinia)
app.use(router)
app.mount('#app')
