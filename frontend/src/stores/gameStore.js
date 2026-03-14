import { defineStore } from 'pinia'

const API = import.meta.env.VITE_API_BASE_URL
const STORAGE_KEY = 'claido_session'
const DEFAULT_VILLAIN_TOKENS = 3
const DEFAULT_GOOD_TOKENS = 2

function normalizeClue(clue = {}) {
  return {
    id: clue.id,
    room: clue.room,
    text: clue.text,
    timestamp: clue.timestamp,
    locked: Boolean(clue.locked),
  }
}

function loadPersistedState() {
  try {
    const raw = localStorage.getItem(STORAGE_KEY)
    if (!raw) return {}
    const parsed = JSON.parse(raw)
    if (Array.isArray(parsed.discoveredClues)) {
      parsed.discoveredClues = parsed.discoveredClues.map(normalizeClue)
    }
    return parsed
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
      teamMode: state.teamMode,
      teamRole: state.teamRole,
      villainTokens: state.villainTokens,
      goodTokens: state.goodTokens,
      teamActionLog: state.teamActionLog,
      teamMembers: state.teamMembers,
      joinCode: state.joinCode,
      playerId: state.playerId,
      lockedClueIds: state.lockedClueIds,
    }))
  } catch {}
}

function normalizeTeamMembers(members = []) {
  return members.map(member => ({
    memberId: member.memberId ?? member.MemberId,
    displayName: member.displayName ?? member.DisplayName ?? 'player',
    role: member.role ?? member.Role ?? 'good',
  }))
}

function normalizeActionLog(entries = []) {
  return entries.map(entry => ({
    actor: entry.actor ?? entry.Actor,
    action: entry.action ?? entry.Action,
    clueId: entry.clueId ?? entry.ClueId,
    room: entry.room ?? entry.Room,
    snippet: entry.snippet ?? entry.Snippet,
    memberId: entry.memberId ?? entry.MemberId,
    displayName: entry.displayName ?? entry.DisplayName,
    timestamp: entry.timestamp ?? entry.Timestamp,
  }))
}

function initializeNewSession(state, payload) {
  state.sessionId = payload.sessionId
  state.sessionState = payload
  state.gameStartTime = Date.now()
  state.discoveredClues = []
  state.completedRooms = []
  state.conversationHistories = {}
  state.roomCache = {}
  state.shellHistory = []
  state.teamMembers = []
  state.teamActionLog = []
  state.lockedClueIds = []
  state.villainTokens = payload.villainTokens ?? DEFAULT_VILLAIN_TOKENS
  state.goodTokens = payload.goodTokens ?? DEFAULT_GOOD_TOKENS
}

function syncClueLockStatus(state) {
  const lockedSet = new Set(state.lockedClueIds ?? [])
  state.discoveredClues = state.discoveredClues.map(clue => ({
    ...clue,
    locked: lockedSet.has(clue.id),
  }))
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
    teamMode: _persisted.teamMode ?? 'standard',
    teamRole: _persisted.teamRole ?? 'good',
    villainTokens: _persisted.villainTokens ?? DEFAULT_VILLAIN_TOKENS,
    goodTokens: _persisted.goodTokens ?? DEFAULT_GOOD_TOKENS,
    teamActionLog: _persisted.teamActionLog ?? [],
    teamMembers: _persisted.teamMembers ?? [],
    joinCode: _persisted.joinCode ?? '',
    playerId: _persisted.playerId ?? null,
    lockedClueIds: _persisted.lockedClueIds ?? [],
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
      initializeNewSession(this, data)
      this.joinCode = data.joinCode ?? data.JoinCode ?? ''
      this.teamMode = data.teamMode ?? this.teamMode
      this.teamRole = 'good'
      this.playerId = null
      persistState(this)
      return data
    },

    async createTeamRoom(displayName = 'Host Investigator') {
      const res = await fetch(`${API}/api/session/team/create`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ displayName }),
      })
      if (!res.ok) {
        const err = await res.json().catch(() => ({}))
        throw new Error(err?.error || 'Failed to create team room.')
      }
      const payload = await res.json()
      initializeNewSession(this, payload)
      this.teamMode = 'team'
      this.applyTeamSnapshot(payload, payload.role, payload.playerId)
      return payload
    },

    async joinTeamSession(joinCode, displayName = 'Investigator') {
      const code = joinCode ?? this.joinCode
      if (!code) throw new Error('Join code is required.')
      const res = await fetch(`${API}/api/session/join`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          joinCode: code,
          displayName,
        }),
      })
      if (!res.ok) {
        const err = await res.json().catch(() => ({}))
        throw new Error(err?.error || 'Failed to join session.')
      }
      const payload = await res.json()
      this.sessionId = payload.sessionId
      this.applyTeamSnapshot(payload, payload.role, payload.playerId)
      return payload
    },

    async refreshTeamState() {
      if (!this.sessionId) return null
      const res = await fetch(`${API}/api/session/${this.sessionId}/team/state`)
      if (!res.ok) throw new Error('Failed to refresh team state.')
      const payload = await res.json()
      this.applyTeamSnapshot(payload)
      return payload
    },

    addClue(id, room, text) {
      if (this.discoveredClues.find(c => c.id === id)) return
      const clue = {
        id,
        room,
        text,
        timestamp: Date.now(),
        locked: this.lockedClueIds.includes(id),
      }
      this.discoveredClues.push(clue)
      this.clueNotification = { text, room }
      setTimeout(() => { this.clueNotification = null }, 3500)
      persistState(this)
    },

    applyTeamSnapshot(payload, assignedRole = null, playerId = null) {
      if (!payload) return
      this.sessionState = payload
      this.teamMembers = normalizeTeamMembers(payload.teamMembers ?? [])
      this.teamActionLog = normalizeActionLog(payload.teamActionLog ?? [])
      this.lockedClueIds = payload.lockedClues ?? []
      this.villainTokens = payload.villainTokens ?? this.villainTokens
      this.goodTokens = payload.goodTokens ?? this.goodTokens
      this.teamMode = payload.teamMode ?? this.teamMode
      this.joinCode = payload.joinCode ?? this.joinCode
      if (playerId) this.playerId = playerId
      if (assignedRole) {
        this.teamRole = assignedRole
      } else if (this.playerId) {
        const me = this.teamMembers.find(m => m.memberId === this.playerId)
        if (me) this.teamRole = me.role
      }
      syncClueLockStatus(this)
      persistState(this)
    },

    configureTeamMode(mode) {
      this.teamMode = mode
      if (mode === 'standard') {
        this.teamRole = 'good'
      }
      persistState(this)
    },

    async lockClue(clue) {
      if (!clue || !this.sessionId || !this.playerId || this.teamMode !== 'team' || this.teamRole !== 'villain') return false
      const res = await fetch(`${API}/api/session/${this.sessionId}/team/lock`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          memberId: this.playerId,
          clueId: clue.id,
          room: clue.room,
          snippet: clue.text,
        }),
      })
      if (!res.ok) throw new Error('Failed to lock clue.')
      const payload = await res.json()
      this.applyTeamSnapshot(payload)
      return true
    },

    async unlockClue(clue) {
      if (!clue || !this.sessionId || !this.playerId || this.teamMode !== 'team') return false
      const res = await fetch(`${API}/api/session/${this.sessionId}/team/unlock`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          memberId: this.playerId,
          clueId: clue.id,
          room: clue.room,
          snippet: clue.text,
        }),
      })
      if (!res.ok) throw new Error('Failed to unlock clue.')
      const payload = await res.json()
      this.applyTeamSnapshot(payload)
      return true
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
