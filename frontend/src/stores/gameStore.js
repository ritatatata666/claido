import { defineStore } from 'pinia'

const API = (import.meta.env.VITE_API_BASE_URL || '').replace(/\/$/, '')
const STORAGE_KEY = 'claido_session'
const DEFAULT_VILLAIN_TOKENS = 3
const DEFAULT_GOOD_TOKENS = 2
const DEFAULT_MAX_WRONG_ATTEMPTS = 5

function normalizeClue(clue = {}) {
  return {
    id: clue.id,
    room: clue.room,
    text: clue.text,
    timestamp: clue.timestamp,
    locked: Boolean(clue.locked),
  }
}

function normalizeRoomName(roomName = '') {
  return String(roomName).trim().toLowerCase()
}

function normalizeWrongAttemptMap(value) {
  if (!value || typeof value !== 'object') return {}
  return Object.entries(value).reduce((acc, [roomName, attemptCount]) => {
    const room = normalizeRoomName(roomName)
    const count = Number(attemptCount)
    if (room) acc[room] = Number.isFinite(count) ? Math.max(0, Math.floor(count)) : 0
    return acc
  }, {})
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
      shellOutputLines: state.shellOutputLines,
      shellCwd: state.shellCwd,
      teamMode: state.teamMode,
      teamRole: state.teamRole,
      villainTokens: state.villainTokens,
      goodTokens: state.goodTokens,
      teamActionLog: state.teamActionLog,
      teamMembers: state.teamMembers,
      joinCode: state.joinCode,
      playerId: state.playerId,
      lockedClueIds: state.lockedClueIds,
      protectedClueIds: state.protectedClueIds,
      investigatorName: state.investigatorName,
      leaderboard: state.leaderboard,
      notes: state.notes,
      roomWrongAttempts: state.roomWrongAttempts,
      penaltySecondsTotal: state.penaltySecondsTotal,
      maxWrongAttemptsPerRoom: state.maxWrongAttemptsPerRoom,
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

function normalizeLeaderboardEntries(entries = []) {
  return entries.map(entry => ({
    displayName: entry.displayName ?? entry.DisplayName ?? 'Player',
    solveSeconds: Number(entry.solveSeconds ?? entry.SolveSeconds ?? 0),
    completedAtUtc: entry.completedAtUtc ?? entry.CompletedAtUtc ?? null,
  }))
}

function resolveGameStartTime(payload, fallback = null) {
  const startedAt = payload?.startedAtUtc ?? payload?.StartedAtUtc
  const parsed = startedAt ? Date.parse(startedAt) : NaN
  if (Number.isFinite(parsed)) return parsed
  if (typeof fallback === 'number') return fallback
  return Date.now()
}

function initializeNewSession(state, payload) {
  state.sessionId = payload.sessionId
  state.sessionState = payload
  state.gameStartTime = resolveGameStartTime(payload)
  state.discoveredClues = []
  state.completedRooms = []
  state.conversationHistories = {}
  state.roomCache = {}
  state.shellHistory = []
  state.shellOutputLines = []
  state.shellCwd = '/home/analyst'
  state.teamMembers = []
  state.teamActionLog = []
  state.lockedClueIds = []
  state.protectedClueIds = []
  state.villainTokens = payload.villainTokens ?? DEFAULT_VILLAIN_TOKENS
  state.goodTokens = payload.goodTokens ?? DEFAULT_GOOD_TOKENS
  state.notes = ''
  state.roomWrongAttempts = {}
  state.penaltySecondsTotal = 0
  state.maxWrongAttemptsPerRoom = DEFAULT_MAX_WRONG_ATTEMPTS
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
    shellOutputLines: _persisted.shellOutputLines ?? [],
    shellCwd: _persisted.shellCwd ?? '/home/analyst',
    teamMode: _persisted.teamMode ?? 'standard',
    teamRole: _persisted.teamRole ?? 'good',
    villainTokens: _persisted.villainTokens ?? DEFAULT_VILLAIN_TOKENS,
    goodTokens: _persisted.goodTokens ?? DEFAULT_GOOD_TOKENS,
    teamActionLog: _persisted.teamActionLog ?? [],
    teamMembers: _persisted.teamMembers ?? [],
    joinCode: _persisted.joinCode ?? '',
    playerId: _persisted.playerId ?? null,
    lockedClueIds: _persisted.lockedClueIds ?? [],
    protectedClueIds: _persisted.protectedClueIds ?? [],
    investigatorName: _persisted.investigatorName ?? '',
    leaderboard: normalizeLeaderboardEntries(_persisted.leaderboard ?? []),
    notes: _persisted.notes ?? '',
    roomWrongAttempts: normalizeWrongAttemptMap(_persisted.roomWrongAttempts),
    penaltySecondsTotal: Number(_persisted.penaltySecondsTotal ?? 0) || 0,
    maxWrongAttemptsPerRoom: Number(_persisted.maxWrongAttemptsPerRoom ?? DEFAULT_MAX_WRONG_ATTEMPTS) || DEFAULT_MAX_WRONG_ATTEMPTS,
  }),

  getters: {
    isRoomComplete: (state) => (room) => state.completedRooms.includes(room),
    currentPlayerName: (state) => {
      if (state.playerId) {
        const me = state.teamMembers.find(member => member.memberId === state.playerId)
        if (me?.displayName) return me.displayName
      }
      return state.investigatorName || 'Player'
    },
    elapsedSeconds: (state) => {
      if (!state.gameStartTime) return 0
      return Math.floor((Date.now() - state.gameStartTime) / 1000) + Math.max(0, Number(state.penaltySecondsTotal) || 0)
    },
  },

  actions: {
    applyAttemptPayload(payload, fallbackRoomName = null) {
      if (!payload || typeof payload !== 'object') return

      const normalizedMax = Number(payload.maxAttempts)
      if (Number.isFinite(normalizedMax) && normalizedMax > 0) {
        this.maxWrongAttemptsPerRoom = Math.floor(normalizedMax)
      }

      const normalizedPenaltyTotal = Number(payload.totalPenaltySeconds)
      if (Number.isFinite(normalizedPenaltyTotal) && normalizedPenaltyTotal >= 0) {
        this.penaltySecondsTotal = Math.floor(normalizedPenaltyTotal)
      }

      const roomName = normalizeRoomName(payload.roomName ?? fallbackRoomName)
      if (!roomName) return
      const wrongAttempts = Number(payload.wrongAttempts)
      if (Number.isFinite(wrongAttempts) && wrongAttempts >= 0) {
        this.roomWrongAttempts = {
          ...this.roomWrongAttempts,
          [roomName]: Math.floor(wrongAttempts),
        }
      }
    },

    getWrongAttempts(roomName) {
      const room = normalizeRoomName(roomName)
      return Math.max(0, Number(this.roomWrongAttempts[room] ?? 0) || 0)
    },

    getAttemptsRemaining(roomName) {
      return Math.max(0, this.maxWrongAttemptsPerRoom - this.getWrongAttempts(roomName))
    },

    isRoomLocked(roomName) {
      return this.getWrongAttempts(roomName) >= this.maxWrongAttemptsPerRoom
    },

    async createSession(displayName = 'Investigator') {
      const res = await fetch(`${API}/api/session/create`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ displayName }),
      })
      if (!res.ok) throw new Error('Failed to create session')
      const data = await res.json()
      initializeNewSession(this, data)
      this.joinCode = data.joinCode ?? data.JoinCode ?? ''
      this.teamMode = data.teamMode ?? this.teamMode
      this.teamRole = 'good'
      this.playerId = null
      this.investigatorName = data.investigatorName ?? displayName
      persistState(this)
      return data
    },

    async createTeamRoom(displayName, preferredRole = 'investigator') {
      if (!String(displayName || '').trim()) throw new Error('Investigator name is required.')
      const res = await apiFetch('/api/session/team/create', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ displayName, preferredRole }),
      })
      if (!res.ok) {
        const err = await res.json().catch(() => ({}))
        throw new Error(err?.error || 'Failed to create team room.')
      }
      const payload = await res.json()
      initializeNewSession(this, payload)
      this.teamMode = 'team'
      this.investigatorName = displayName
      this.applyTeamSnapshot(payload, payload.role, payload.playerId)
      return payload
    },

    async joinTeamSession(joinCode, displayName, preferredRole = 'investigator') {
      const code = joinCode ?? this.joinCode
      if (!code) throw new Error('Join code is required.')
      if (!String(displayName || '').trim()) throw new Error('Investigator name is required.')
      const res = await apiFetch('/api/session/join', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          joinCode: code,
          displayName,
          preferredRole,
        }),
      })
      if (!res.ok) {
        const err = await res.json().catch(() => ({}))
        throw new Error(err?.error || 'Failed to join session.')
      }
      const payload = await res.json()
      this.sessionId = payload.sessionId
      this.investigatorName = displayName
      this.applyTeamSnapshot(payload, payload.role, payload.playerId)
      return payload
    },

    async refreshTeamState() {
      if (!this.sessionId) return null
      const res = await apiFetch(`/api/session/${this.sessionId}/team/state`)
      if (!res.ok) throw new Error('Failed to refresh team state.')
      const payload = await res.json()
      this.applyTeamSnapshot(payload)
      return payload
    },

    async fetchLeaderboard() {
      const res = await fetch(`${API}/api/session/leaderboard`)
      if (!res.ok) throw new Error('Failed to load leaderboard.')
      const payload = await res.json()
      this.leaderboard = normalizeLeaderboardEntries(payload)
      persistState(this)
      return this.leaderboard
    },

    addClue(id, room, text) {
      if (this.discoveredClues.find(c => c.id === id)) return
      const isMaskedForInvestigator = this.teamMode === 'team' && this.teamRole === 'good' && this.lockedClueIds.includes(id)
      const clue = {
        id,
        room,
        text,
        timestamp: Date.now(),
        locked: this.lockedClueIds.includes(id),
      }
      this.discoveredClues.push(clue)
      this.clueNotification = {
        text: isMaskedForInvestigator ? 'Clue hidden by the saboteur — reveal it in Team Mode.' : text,
        room,
      }
      setTimeout(() => { this.clueNotification = null }, 3500)
      if (this.teamMode === 'team' && this.teamRole === 'good') {
        this.reportInvestigatorClue(id, room, text).catch(() => {})
      }
      persistState(this)
    },

    async reportInvestigatorClue(clueId, room = '', snippet = '') {
      if (!this.sessionId || !this.playerId || this.teamMode !== 'team' || this.teamRole !== 'good' || !clueId) return false
      const res = await apiFetch(`/api/session/${this.sessionId}/team/discover`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          memberId: this.playerId,
          clueId,
          room,
          snippet,
        }),
      })
      if (!res.ok) return false
      const payload = await res.json()
      this.applyTeamSnapshot(payload)
      return true
    },

    applyTeamSnapshot(payload, assignedRole = null, playerId = null) {
      if (!payload) return
      this.sessionState = payload
      this.gameStartTime = resolveGameStartTime(payload, this.gameStartTime)
      this.teamMembers = normalizeTeamMembers(payload.teamMembers ?? [])
      this.teamActionLog = normalizeActionLog(payload.teamActionLog ?? [])
      this.lockedClueIds = payload.lockedClues ?? []
      this.protectedClueIds = payload.investigatorFoundClues ?? payload.InvestigatorFoundClues ?? []
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
      if (this.protectedClueIds.includes(clue.id)) throw new Error('This clue was already found by investigators and can no longer be sabotaged.')
      const res = await apiFetch(`/api/session/${this.sessionId}/team/lock`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          memberId: this.playerId,
          clueId: clue.id,
          room: clue.room,
          snippet: clue.text,
        }),
      })
      if (!res.ok) {
        const err = await res.json().catch(() => ({}))
        throw new Error(err?.error || 'Failed to lock clue.')
      }
      const payload = await res.json()
      this.applyTeamSnapshot(payload)
      return true
    },

    async unlockClue(clue) {
      if (!clue || !this.sessionId || !this.playerId || this.teamMode !== 'team') return false
      const res = await apiFetch(`/api/session/${this.sessionId}/team/unlock`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          memberId: this.playerId,
          clueId: clue.id,
          room: clue.room,
          snippet: clue.text,
        }),
      })
      if (!res.ok) {
        const err = await res.json().catch(() => ({}))
        throw new Error(err?.error || 'Failed to unlock clue.')
      }
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
      const res = await apiFetch(`/api/session/${this.sessionId}/npc/chat`, {
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
      const res = await apiFetch(`/api/session/${this.sessionId}/room/${roomName}/enter`, {
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

    setShellOutputLines(lines) {
      this.shellOutputLines = lines
      persistState(this)
    },

    setShellCwd(cwd) {
      this.shellCwd = cwd
      persistState(this)
    },

    async validateAnswer(roomName, answer) {
      const room = normalizeRoomName(roomName)
      const res = await fetch(`${API}/api/session/${this.sessionId}/room/${room}/validate`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          answer, ...extras,
          memberId: this.playerId,
        }),
      })
      const payload = await res.json()
      this.applyAttemptPayload(payload, room)
      if (Array.isArray(payload.leaderboard)) {
        this.leaderboard = normalizeLeaderboardEntries(payload.leaderboard)
      }
      persistState(this)
      return payload
    },

    async registerWrongAttempt(roomName) {
      const room = normalizeRoomName(roomName)
      const res = await fetch(`${API}/api/session/${this.sessionId}/room/${room}/wrong-attempt`, {
        method: 'POST',
      })
      const payload = await res.json()
      if (!res.ok) {
        throw new Error(payload?.error || `Failed to record wrong attempt in ${room}.`)
      }
      this.applyAttemptPayload(payload, room)
      persistState(this)
      return payload
    },
  },
})
