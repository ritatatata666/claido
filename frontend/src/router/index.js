import { createRouter, createWebHistory } from 'vue-router'
import LandingView from '../views/LandingView.vue'
import ShellView from '../views/ShellView.vue'
import DatabaseView from '../views/DatabaseView.vue'
import MailView from '../views/MailView.vue'
import WikiView from '../views/WikiView.vue'
import SearchView from '../views/SearchView.vue'
import OnionView from '../views/OnionView.vue'
import VaultView from '../views/VaultView.vue'

const routes = [
  { path: '/', component: LandingView },
  { path: '/shell', component: ShellView },
  { path: '/database', component: DatabaseView },
  { path: '/mail', component: MailView },
  { path: '/wiki', component: WikiView },
  { path: '/search', component: SearchView },
  { path: '/onion', component: OnionView },
  { path: '/vault', component: VaultView },
]

export default createRouter({
  history: createWebHistory(),
  routes,
})
