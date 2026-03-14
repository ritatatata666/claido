<template>
  <div class="hub">
    <div class="scanlines"></div>

    <!-- Top bar -->
    <header class="hub-topbar">
      <div class="hub-logo">
        <span class="logo-icon">🔐</span>
        <span class="logo-text">CLAIDO</span>
        <span class="logo-sub">Ops Center</span>
      </div>
      <div class="hub-progress">
        <div class="progress-label">INVESTIGATION PROGRESS</div>
        <div class="progress-bar-wrap">
          <div class="progress-bar-fill" :style="{ width: progressPercent + '%' }"></div>
        </div>
        <div class="progress-count">
          <span class="count-done">{{ clearedCount }}</span>
          <span class="count-sep"> / </span>
          <span class="count-total">6</span>
          <span class="count-label"> rooms cleared</span>
        </div>
      </div>
      <div class="hub-timer">
        <span class="timer-label">ELAPSED</span>
        <span class="timer-val">{{ formattedTime }}</span>
      </div>
    </header>

    <!-- Room grid -->
    <main class="hub-main">
      <div class="section-label">SELECT INVESTIGATION ROOM</div>
      <div class="room-grid">
        <div
          v-for="room in mainRooms"
          :key="room.id"
          :class="['room-panel', `room-panel--${room.color}`, { 'room-panel--cleared': getRoomStatus(room.id) === 'CLEARED' }]"
          @click="enterRoom(room)"
        >
          <div class="panel-left-border"></div>
          <div class="panel-body">
            <div class="panel-top">
              <span class="panel-icon">{{ room.icon }}</span>
              <div class="panel-name-wrap">
                <div class="panel-name">{{ room.name }}</div>
                <div class="panel-desc">{{ room.desc }}</div>
              </div>
              <div :class="['panel-badge', `badge--${getRoomStatus(room.id).toLowerCase().replace(' ', '-')}`]">
                {{ getRoomStatus(room.id) }}
              </div>
            </div>
          </div>
        </div>
      </div>

      <!-- Vault row -->
      <div class="vault-section">
        <div class="section-label">FINAL OBJECTIVE</div>
        <div
          class="room-panel room-panel--vault"
          @click="enterRoom(vaultRoom)"
        >
          <div class="panel-left-border"></div>
          <div class="panel-body">
            <div class="panel-top">
              <span class="panel-icon">{{ vaultRoom.icon }}</span>
              <div class="panel-name-wrap">
                <div class="panel-name">{{ vaultRoom.name }}</div>
                <div class="panel-desc">{{ vaultRoom.desc }}</div>
              </div>
              <div class="vault-clues-hint">
                {{ store.discoveredClues.length }} / 4 clues collected
              </div>
            </div>
          </div>
        </div>
      </div>
    </main>
  </div>
</template>

<script setup>
import { computed, onMounted, onUnmounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useGameStore } from '../stores/gameStore.js'

const router = useRouter()
const store = useGameStore()

const mainRooms = [
  {
    id: 'shell',
    name: 'NovaShell',
    icon: '>_',
    desc: 'Explore the internal filesystem and decode hidden secrets',
    route: '/shell',
    color: 'green',
  },
  {
    id: 'database',
    name: 'NovaCrime DB',
    icon: '⬡',
    desc: 'Query employee records and access logs via SQL',
    route: '/database',
    color: 'blue',
  },
  {
    id: 'mail',
    name: 'NovaMail',
    icon: '✉',
    desc: 'Read intercepted corporate emails for suspicious activity',
    route: '/mail',
    color: 'cyan',
  },
  {
    id: 'wiki',
    name: 'NovaWiki',
    icon: '◈',
    desc: 'Browse classified internal documents and decode redacted sections',
    route: '/wiki',
    color: 'yellow',
  },
  {
    id: 'search',
    name: 'NovaSearch',
    icon: '⊕',
    desc: 'Analyse 50,000 system log entries for anomalies',
    route: '/search',
    color: 'teal',
  },
  {
    id: 'onion',
    name: 'The Onion',
    icon: '⊗',
    desc: 'Browse dark web channels for leaked intelligence',
    route: '/onion',
    color: 'purple',
  },
]

const vaultRoom = {
  id: 'vault',
  name: 'Vault',
  icon: '🔒',
  desc: 'Enter the four-word passphrase to complete the investigation',
  route: '/vault',
}

function getRoomStatus(roomId) {
  if (store.completedRooms.includes(roomId)) return 'CLEARED'
  if (store.roomCache[roomId]) return 'IN PROGRESS'
  return 'ACTIVE'
}

function enterRoom(room) {
  router.push(room.route)
}

const clearedCount = computed(() =>
  mainRooms.filter(r => store.completedRooms.includes(r.id)).length
)

const progressPercent = computed(() => Math.round((clearedCount.value / 6) * 100))

// Timer
const elapsed = ref(0)
let timerInterval = null

onMounted(() => {
  if (!store.sessionId) {
    router.replace('/')
    return
  }
  timerInterval = setInterval(() => {
    if (store.gameStartTime) {
      elapsed.value = Math.floor((Date.now() - store.gameStartTime) / 1000)
    }
  }, 1000)
})

onUnmounted(() => clearInterval(timerInterval))

const formattedTime = computed(() => {
  const s = elapsed.value
  const h = Math.floor(s / 3600)
  const m = Math.floor((s % 3600) / 60)
  const sec = s % 60
  if (h > 0) return `${h}:${String(m).padStart(2, '0')}:${String(sec).padStart(2, '0')}`
  return `${String(m).padStart(2, '0')}:${String(sec).padStart(2, '0')}`
})
</script>

<style scoped>
/* ── Base ─────────────────────────────────────────────── */
.hub {
  min-height: 100vh;
  background: #0a0a0f;
  display: flex;
  flex-direction: column;
  font-family: 'Courier New', Courier, monospace;
  position: relative;
  overflow-y: auto;
}

.scanlines {
  position: fixed;
  inset: 0;
  pointer-events: none;
  z-index: 0;
  background: repeating-linear-gradient(
    to bottom,
    transparent,
    transparent 3px,
    rgba(0, 0, 0, 0.07) 3px,
    rgba(0, 0, 0, 0.07) 4px
  );
}

/* ── Topbar ───────────────────────────────────────────── */
.hub-topbar {
  position: relative;
  z-index: 1;
  display: grid;
  grid-template-columns: 1fr auto 1fr;
  align-items: center;
  padding: 0 28px;
  height: 64px;
  background: #0d0d14;
  border-bottom: 1px solid #1e2030;
  gap: 24px;
}

.hub-logo {
  display: flex;
  align-items: center;
  gap: 10px;
}

.logo-icon {
  font-size: 20px;
}

.logo-text {
  font-size: 18px;
  font-weight: 900;
  letter-spacing: 4px;
  color: #00ff41;
  text-shadow: 0 0 10px rgba(0, 255, 65, 0.5);
}

.logo-sub {
  font-size: 11px;
  color: #3a4a3a;
  letter-spacing: 2px;
  text-transform: uppercase;
}

.hub-progress {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 5px;
  min-width: 260px;
}

.progress-label {
  font-size: 9px;
  letter-spacing: 2px;
  color: #3a4050;
  text-transform: uppercase;
}

.progress-bar-wrap {
  width: 100%;
  height: 4px;
  background: #1a1a28;
  border-radius: 2px;
  overflow: hidden;
}

.progress-bar-fill {
  height: 100%;
  background: linear-gradient(90deg, #00ff41, #00cc33);
  border-radius: 2px;
  transition: width 0.4s ease;
  box-shadow: 0 0 8px rgba(0, 255, 65, 0.6);
}

.progress-count {
  font-size: 12px;
  color: #5a6070;
  letter-spacing: 0.5px;
}

.count-done {
  font-weight: 700;
  color: #00ff41;
  font-size: 14px;
}

.count-sep {
  color: #3a4050;
}

.count-total {
  color: #7a8090;
  font-size: 14px;
}

.count-label {
  font-size: 11px;
  color: #3a4050;
  text-transform: uppercase;
  letter-spacing: 1px;
}

.hub-timer {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  gap: 2px;
}

.timer-label {
  font-size: 9px;
  letter-spacing: 2px;
  color: #3a4050;
  text-transform: uppercase;
}

.timer-val {
  font-size: 20px;
  font-weight: 700;
  color: #e0a020;
  font-family: 'Courier New', monospace;
  letter-spacing: 2px;
  text-shadow: 0 0 8px rgba(224, 160, 32, 0.4);
}

/* ── Main content ─────────────────────────────────────── */
.hub-main {
  position: relative;
  z-index: 1;
  flex: 1;
  padding: 32px 36px 48px;
  display: flex;
  flex-direction: column;
  gap: 20px;
  max-width: 1100px;
  margin: 0 auto;
  width: 100%;
}

.section-label {
  font-size: 10px;
  font-weight: 700;
  letter-spacing: 3px;
  color: #2a3040;
  text-transform: uppercase;
  margin-bottom: 4px;
}

/* ── Room grid ────────────────────────────────────────── */
.room-grid {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 12px;
}

@media (max-width: 700px) {
  .room-grid {
    grid-template-columns: 1fr;
  }
  .hub-topbar {
    grid-template-columns: 1fr;
    height: auto;
    padding: 12px 16px;
    gap: 12px;
  }
  .hub-timer {
    align-items: flex-start;
  }
}

/* ── Room panel ───────────────────────────────────────── */
.room-panel {
  display: flex;
  align-items: stretch;
  background: #0f0f18;
  border: 1px solid #1a1a28;
  border-radius: 4px;
  cursor: pointer;
  transition: border-color 0.18s, background 0.18s, box-shadow 0.18s;
  overflow: hidden;
  position: relative;
}

.room-panel:hover {
  background: #13131e;
  box-shadow: 0 4px 24px rgba(0, 0, 0, 0.5);
}

.panel-left-border {
  width: 4px;
  flex-shrink: 0;
  background: #2a2a40;
  transition: background 0.18s;
}

/* Color variants */
.room-panel--green .panel-left-border   { background: #00cc44; }
.room-panel--blue .panel-left-border    { background: #1f6feb; }
.room-panel--cyan .panel-left-border    { background: #0dbfb8; }
.room-panel--yellow .panel-left-border  { background: #d4a017; }
.room-panel--teal .panel-left-border    { background: #00bfb3; }
.room-panel--purple .panel-left-border  { background: #8b5cf6; }

.room-panel--green:hover  { border-color: #00cc44; box-shadow: 0 4px 20px rgba(0, 204, 68, 0.15); }
.room-panel--blue:hover   { border-color: #1f6feb; box-shadow: 0 4px 20px rgba(31, 111, 235, 0.15); }
.room-panel--cyan:hover   { border-color: #0dbfb8; box-shadow: 0 4px 20px rgba(13, 191, 184, 0.15); }
.room-panel--yellow:hover { border-color: #d4a017; box-shadow: 0 4px 20px rgba(212, 160, 23, 0.15); }
.room-panel--teal:hover   { border-color: #00bfb3; box-shadow: 0 4px 20px rgba(0, 191, 179, 0.15); }
.room-panel--purple:hover { border-color: #8b5cf6; box-shadow: 0 4px 20px rgba(139, 92, 246, 0.15); }

/* Cleared state */
.room-panel--cleared {
  opacity: 0.65;
}

.room-panel--cleared .panel-left-border {
  background: #3fb950 !important;
}

/* Panel body */
.panel-body {
  flex: 1;
  padding: 16px 18px;
}

.panel-top {
  display: flex;
  align-items: center;
  gap: 14px;
}

.panel-icon {
  font-size: 22px;
  flex-shrink: 0;
  width: 32px;
  text-align: center;
  font-family: 'Courier New', monospace;
  color: #7a8090;
}

.room-panel--green .panel-icon   { color: #00cc44; }
.room-panel--blue .panel-icon    { color: #1f6feb; }
.room-panel--cyan .panel-icon    { color: #0dbfb8; }
.room-panel--yellow .panel-icon  { color: #d4a017; }
.room-panel--teal .panel-icon    { color: #00bfb3; }
.room-panel--purple .panel-icon  { color: #8b5cf6; }

.panel-name-wrap {
  flex: 1;
  min-width: 0;
}

.panel-name {
  font-size: 14px;
  font-weight: 700;
  color: #c8d0dc;
  letter-spacing: 1px;
  text-transform: uppercase;
  margin-bottom: 3px;
}

.panel-desc {
  font-size: 11px;
  color: #4a5060;
  line-height: 1.45;
}

/* Status badges */
.panel-badge {
  flex-shrink: 0;
  font-size: 9px;
  font-weight: 700;
  letter-spacing: 1.5px;
  text-transform: uppercase;
  padding: 3px 8px;
  border-radius: 2px;
  border: 1px solid;
}

.badge--active {
  color: #3fb950;
  border-color: #3fb950;
  background: rgba(63, 185, 80, 0.08);
}

.badge--in-progress {
  color: #e0a020;
  border-color: #e0a020;
  background: rgba(224, 160, 32, 0.08);
}

.badge--cleared {
  color: #58b0ff;
  border-color: #58b0ff;
  background: rgba(88, 176, 255, 0.08);
}

/* ── Vault section ────────────────────────────────────── */
.vault-section {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.room-panel--vault {
  background: #100e08;
  border-color: #2a2010;
}

.room-panel--vault .panel-left-border {
  background: linear-gradient(180deg, #d4a017, #a07010);
}

.room-panel--vault:hover {
  background: #141008;
  border-color: #d4a017;
  box-shadow: 0 4px 24px rgba(212, 160, 23, 0.2);
}

.room-panel--vault .panel-icon {
  color: #d4a017;
  font-size: 24px;
}

.room-panel--vault .panel-name {
  color: #e0b840;
}

.vault-clues-hint {
  flex-shrink: 0;
  font-size: 11px;
  color: #8a7030;
  font-weight: 600;
  text-align: right;
  white-space: nowrap;
}
</style>
