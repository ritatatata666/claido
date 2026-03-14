import { defineStore } from 'pinia'

const API = import.meta.env.VITE_API_BASE_URL

export const useGameStore = defineStore('game', {
  state: () => ({
    sessionId: null,
    sessionState: null,
    currentRoom: null,
    discoveredClues: [],        // [{ id, room, text, timestamp }]
    conversationHistories: {},  // npcId -> [{ role, content }]
    completedRooms: [],         // ['shell', 'mail', ...]
    gameStartTime: null,
    clueNotification: null,     // { text, room } — cleared after showing
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
      return data
    },

    addClue(id, room, text) {
      if (this.discoveredClues.find(c => c.id === id)) return
      const clue = { id, room, text, timestamp: Date.now() }
      this.discoveredClues.push(clue)
      this.clueNotification = { text, room }
      setTimeout(() => { this.clueNotification = null }, 3500)
    },

    clearNotification() {
      this.clueNotification = null
    },

    markRoomComplete(room) {
      if (!this.completedRooms.includes(room)) {
        this.completedRooms.push(room)
      }
    },

    addNpcMessage(npcId, role, content) {
      if (!this.conversationHistories[npcId]) {
        this.conversationHistories[npcId] = []
      }
      this.conversationHistories[npcId].push({ role, content })
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
      const res = await fetch(`${API}/api/session/${this.sessionId}/room/${roomName}/enter`, {
        method: 'POST',
      })
      if (!res.ok) throw new Error(`Failed to enter room: ${roomName}`)
      return res.json()
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
