import { createRouter, createWebHistory } from 'vue-router'
import LandingView from '../views/LandingView.vue'
import HubView from '../views/HubView.vue'
import ShellView from '../views/ShellView.vue'
import DatabaseView from '../views/DatabaseView.vue'
import MailView from '../views/MailView.vue'
import WikiView from '../views/WikiView.vue'
import SearchView from '../views/SearchView.vue'
import OnionView from '../views/OnionView.vue'
import VaultView from '../views/VaultView.vue'
import ReportView from '../views/ReportView.vue'

const routes = [
  { path: '/', component: LandingView },
  { path: '/hub', component: HubView },
  { path: '/report', component: ReportView },
  { path: '/shell', component: ShellView },
  { path: '/database', component: DatabaseView },
  { path: '/mail', component: MailView },
  { path: '/wiki', component: WikiView },
  { path: '/search', component: SearchView },
  { path: '/onion', component: OnionView },
  { path: '/vault', component: VaultView },
  { path: '/challenge/kitchen', component: MailView },
  { path: '/challenge/ballroom', component: ShellView },
  { path: '/challenge/conservatory', component: SearchView },
  { path: '/challenge/dining-room', component: DatabaseView },
  { path: '/challenge/billiard-room', component: DatabaseView },
  { path: '/challenge/library', component: WikiView },
  { path: '/challenge/lounge', component: MailView },
  { path: '/challenge/hall', component: HubView },
  { path: '/challenge/study', component: OnionView },
]

export default createRouter({
  history: createWebHistory(),
  routes,
})
