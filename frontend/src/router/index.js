import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '../stores/authStore.js'
import LandingView from '../views/LandingView.vue'
import LoginView from '../views/LoginView.vue'
import RegisterView from '../views/RegisterView.vue'
import HistoryView from '../views/HistoryView.vue'
import HistoryCaseView from '../views/HistoryCaseView.vue'
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
  { path: '/login', component: LoginView, meta: { guestOnly: true } },
  { path: '/register', component: RegisterView, meta: { guestOnly: true } },
  { path: '/', component: LandingView, meta: { requiresAuth: true } },
  { path: '/history', component: HistoryView, meta: { requiresAuth: true } },
  { path: '/history/:sessionId', component: HistoryCaseView, meta: { requiresAuth: true } },
  { path: '/hub', component: HubView, meta: { requiresAuth: true } },
  { path: '/report', component: ReportView },
  { path: '/shell', component: ShellView, meta: { requiresAuth: true } },
  { path: '/database', component: DatabaseView, meta: { requiresAuth: true } },
  { path: '/mail', component: MailView, meta: { requiresAuth: true } },
  { path: '/wiki', component: WikiView, meta: { requiresAuth: true } },
  { path: '/search', component: SearchView, meta: { requiresAuth: true } },
  { path: '/onion', component: OnionView, meta: { requiresAuth: true } },
  { path: '/vault', component: VaultView, meta: { requiresAuth: true } },
  { path: '/challenge/kitchen', component: MailView, meta: { requiresAuth: true } },
  { path: '/challenge/ballroom', component: ShellView, meta: { requiresAuth: true } },
  { path: '/challenge/conservatory', component: SearchView, meta: { requiresAuth: true } },
  { path: '/challenge/dining-room', component: DatabaseView, meta: { requiresAuth: true } },
  { path: '/challenge/billiard-room', component: DatabaseView, meta: { requiresAuth: true } },
  { path: '/challenge/library', component: WikiView, meta: { requiresAuth: true } },
  { path: '/challenge/lounge', component: MailView, meta: { requiresAuth: true } },
  { path: '/challenge/hall', component: HubView, meta: { requiresAuth: true } },
  { path: '/challenge/study', component: OnionView, meta: { requiresAuth: true } },
]

export default function createAppRouter(pinia) {
  const router = createRouter({
    history: createWebHistory(),
    routes,
  })

  router.beforeEach(async (to) => {
    const auth = useAuthStore(pinia)
    if (!auth.hasCheckedAuth) {
      await auth.fetchMe().catch(() => null)
    }

    if (to.meta.requiresAuth && !auth.isAuthenticated) {
      return '/login'
    }

    if (to.meta.guestOnly && auth.isAuthenticated) {
      return '/'
    }

    return true
  })

  return router
}
