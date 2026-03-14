import { defineStore } from 'pinia'

const API = import.meta.env.VITE_API_BASE_URL

const STORAGE_KEY = 'claido_session'

function loadPersistedState() {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    return raw ? JSON.parse(raw) : {}
  } catch { return {} }
}

function persistState(state) {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify({
      sessionId: state.sessionId,
      sessionState: state.sessionState,
      discoveredClues: state.discoveredClues,
      completedRooms: state.completedRooms,
      conversationHistories: state.conversationHistories,
      gameStartTime: state.gameStartTime,
      shellHistory: state.shellHistory,
    }))
  } catch {}
}

const _persisted = loadPersistedState()

export const useGameStore = defineStore('game', {
  state: () => ({
    sessionId: _persisted.sessionId ?? null,
    sessionState: _persisted.sessionState ?? null,
    currentRoom: null,
    discoveredClues: _persisted.discoveredClues ?? [],
    conversationHistories: _persisted.conversationHistories ?? {},
    completedRooms: _persisted.completedRooms ?? [],
    gameStartTime: _persisted.gameStartTime ?? null,
    clueNotification: null,
    roomCache: {},
    shellHistory: _persisted.shellHistory ?? [],
  }),

  getters: {
    isRoomComplete: (state) => (room) => state.completedRooms.includes(room),
    elapsedSeconds: (state) => {
      if (!state.gameStartTime) return 0
      return Math.floor((Date.now() - state.gameStartTime) / 1000)
    },
  },

  actions: {
    async createSession() {
      const res = await fetch(`${API}/api/session/create`, { method: 'POST' })
      if (!res.ok) throw new Error('Failed to create session')
      const data = await res.json()
      this.sessionId = data.sessionId
      this.sessionState = data
      this.gameStartTime = Date.now()
      this.discoveredClues = []
      this.completedRooms = []
      this.conversationHistories = {}
      this.roomCache = {}
      this.shellHistory = []
      persistState(this)
      return data
    },

    addClue(id, room, text) {
      if (this.discoveredClues.find(c => c.id === id)) return
      const clue = { id, room, text, timestamp: Date.now() }
      this.discoveredClues.push(clue)
      this.clueNotification = { text, room }
      setTimeout(() => { this.clueNotification = null }, 3500)
      persistState(this)
    },

    clearNotification() {
      this.clueNotification = null
    },

    markRoomComplete(room) {
      if (!this.completedRooms.includes(room)) {
        this.completedRooms.push(room)
        persistState(this)
      }
    },

    addNpcMessage(npcId, role, content) {
      if (!this.conversationHistories[npcId]) {
        this.conversationHistories[npcId] = []
      }
      this.conversationHistories[npcId].push({ role, content })
      persistState(this)
    },

    getNpcHistory(npcId) {
      return this.conversationHistories[npcId] || []
    },

    async sendNpcMessage(npcId, message) {
      const history = this.getNpcHistory(npcId)
      const res = await fetch(`${API}/api/session/${this.sessionId}/npc/chat`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          npcId,
          message,
          conversationHistory: history,
        }),
      })
      if (!res.ok) throw new Error('NPC chat failed')
      const { reply } = await res.json()
      this.addNpcMessage(npcId, 'user', message)
      this.addNpcMessage(npcId, 'assistant', reply)
      return reply
    },

    async enterRoom(roomName) {
      if (this.roomCache[roomName]) {
        return this.roomCache[roomName]
      }
      const res = await fetch(`${API}/api/session/${this.sessionId}/room/${roomName}/enter`, {
        method: 'POST',
      })
      if (res.status === 404) throw new Error('Session expired — please start a new game.')
      if (!res.ok) throw new Error(`Failed to enter room: ${roomName}`)
      const data = await res.json()
      this.roomCache[roomName] = data
      return data
    },

    setShellHistory(history) {
      this.shellHistory = history
      persistState(this)
    },

    async validateAnswer(roomName, answer) {
      const res = await fetch(`${API}/api/session/${this.sessionId}/room/${roomName}/validate`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ answer }),
      })
      return res.json()
    },
  },
})
