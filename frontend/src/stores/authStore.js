import { defineStore } from 'pinia'

const API = (import.meta.env.VITE_API_BASE_URL || '').replace(/\/$/, '')
const STORAGE_KEY = 'claido_auth'

async function apiFetch(path, options = {}) {
  try {
    return await fetch(`${API}${path}`, {
      ...options,
      credentials: 'include',
    })
  } catch {
    throw new Error(`Cannot reach backend (${API || 'via /api proxy'}). Ensure backend is running.`)
  }
}

function isUnauthorizedResponse(res) {
  return res?.status === 401
}

function loadPersistedUser() {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    if (!raw) return null
    const parsed = JSON.parse(raw)
    return parsed?.user ?? null
  } catch {
    return null
  }
}

function persistUser(user) {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify({ user }))
  } catch {}
}

export const useAuthStore = defineStore('auth', {
  state: () => ({
    user: loadPersistedUser(),
    hasCheckedAuth: false,
  }),

  getters: {
    isAuthenticated: (state) => !!state.user,
  },

  actions: {
    async register(username, password) {
      const res = await apiFetch('/api/auth/register', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ username, password }),
      })
      const payload = await res.json().catch(() => ({}))
      if (!res.ok) throw new Error(payload?.error || 'Failed to register.')
      this.user = payload
      this.hasCheckedAuth = true
      persistUser(this.user)
      return payload
    },

    async login(username, password) {
      const res = await apiFetch('/api/auth/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ username, password }),
      })
      const payload = await res.json().catch(() => ({}))
      if (!res.ok) throw new Error(payload?.error || 'Failed to login.')
      this.user = payload
      this.hasCheckedAuth = true
      persistUser(this.user)
      return payload
    },

    async fetchMe() {
      const res = await apiFetch('/api/auth/me', {
        method: 'GET',
      })
      if (!res.ok) {
        this.user = null
        this.hasCheckedAuth = true
        persistUser(null)
        return null
      }
      const payload = await res.json()
      this.user = payload
      this.hasCheckedAuth = true
      persistUser(this.user)
      return payload
    },

    async fetchHistory() {
      const res = await apiFetch('/api/auth/history', {
        method: 'GET',
      })
      if (isUnauthorizedResponse(res)) {
        this.user = null
        this.hasCheckedAuth = true
        persistUser(null)
        throw new Error('Session expired. Please sign in again.')
      }
      if (!res.ok) {
        const payload = await res.json().catch(() => ({}))
        throw new Error(payload?.error || 'Failed to fetch history.')
      }
      return res.json()
    },

    async fetchHistoryCase(sessionId) {
      const res = await apiFetch(`/api/auth/history/${sessionId}`, {
        method: 'GET',
      })
      if (isUnauthorizedResponse(res)) {
        this.user = null
        this.hasCheckedAuth = true
        persistUser(null)
        throw new Error('Session expired. Please sign in again.')
      }
      if (!res.ok) {
        const payload = await res.json().catch(() => ({}))
        throw new Error(payload?.error || 'Failed to fetch case history.')
      }
      return res.json()
    },

    async logout() {
      await apiFetch('/api/auth/logout', {
        method: 'POST',
      }).catch(() => {})
      this.user = null
      this.hasCheckedAuth = true
      persistUser(null)
    },
  },
})
