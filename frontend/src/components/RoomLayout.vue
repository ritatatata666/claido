<template>
  <div :class="['room-layout', 'challenge-page', currentRoomClass]">
    <!-- Room cleared overlay -->
    <Transition name="cleared-fade">
      <div v-if="showClearedOverlay" class="cleared-overlay">
        <div class="cleared-box">
          <div class="cleared-icon">✓</div>
          <div class="cleared-text">Room Cleared!</div>
          <div class="cleared-sub">Returning to Hub...</div>
        </div>
      </div>
    </Transition>

    <!-- Top bar -->
    <header class="topbar">
      <div class="topbar-left">
        <button class="hub-btn" @click="router.push('/hub')">← Hub</button>
      </div>
      <div class="topbar-center">
        <span class="room-name">{{ roomLabel }}</span>
      </div>
      <div class="topbar-right">
        <span class="timer">{{ formattedTime }}</span>
      </div>
    </header>

    <!-- Main content area -->
    <div class="room-body">
      <main class="room-main">
        <slot />
      </main>

      <!-- Right sidebar: clues -->
      <aside :class="['clue-sidebar', { collapsed: sidebarCollapsed }]">
        <button class="sidebar-toggle" @click="sidebarCollapsed = !sidebarCollapsed">
          {{ sidebarCollapsed ? '◀' : '▶' }}
        </button>
        <div v-if="!sidebarCollapsed" class="sidebar-content">
          <h3 class="sidebar-title">Evidence</h3>
          <div v-if="store.discoveredClues.length === 0" class="no-clues">
            No clues found yet.
          </div>
          <div
            v-for="clue in store.discoveredClues"
            :key="clue.id"
            class="clue-item"
          >
            <span class="clue-room">{{ clue.room }}</span>
            <p class="clue-text">{{ clue.text }}</p>
          </div>
        </div>
      </aside>
    </div>

    <!-- Progress dots (read-only status indicators) -->
    <footer class="progress-bar">
      <div
        v-for="room in rooms"
        :key="room.id"
        :class="['progress-dot', {
          active: currentRoomId === room.id,
          done: store.isRoomComplete(room.id)
        }]"
        :title="room.label"
      >
        <span class="dot-label">{{ room.label }}</span>
      </div>
    </footer>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted, watch } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useGameStore } from '../stores/gameStore.js'

const store = useGameStore()
const route = useRoute()
const router = useRouter()
const sidebarCollapsed = ref(false)
const showClearedOverlay = ref(false)

const rooms = [
  { id: 'shell', label: 'NovaShell' },
  { id: 'database', label: 'NovaCrime DB' },
  { id: 'mail', label: 'NovaMail' },
  { id: 'wiki', label: 'NovaWiki' },
  { id: 'search', label: 'NovaSearch' },
  { id: 'onion', label: 'The Onion' },
  { id: 'vault', label: 'Vault' },
]

const roomLabels = {
  shell: 'NovaShell',
  database: 'NovaCrime DB',
  mail: 'NovaMail',
  wiki: 'NovaWiki',
  search: 'NovaSearch',
  onion: 'The Onion',
  vault: 'Vault',
}

const currentRoomId = computed(() => route.path.replace('/', ''))
const currentRoomClass = computed(() => `room--${currentRoomId.value}`)
const roomLabel = computed(() => roomLabels[currentRoomId.value] || currentRoomId.value)

// Watch for room completion to trigger overlay then navigate to hub
watch(
  () => store.completedRooms,
  (newVal) => {
    const room = currentRoomId.value
    if (room && room !== 'vault' && newVal.includes(room) && !showClearedOverlay.value) {
      showClearedOverlay.value = true
      setTimeout(() => {
        showClearedOverlay.value = false
        router.push('/hub')
      }, 2000)
    }
  },
  { deep: true }
)

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

onUnmounted(() => {
  clearInterval(timerInterval)
})

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
.room-layout {
  display: grid;
  grid-template-rows: 48px 1fr 36px;
  height: 100vh;
  background: var(--bg-primary);
  overflow: hidden;
}

.challenge-page {
  background: #121212;
  color: #f5f5f5;
}

.challenge-page .room-main {
  background: rgba(18, 18, 18, 0.85);
  border-radius: 0;
  box-shadow: none;
}

.challenge-page button {
  background: #c9a24a;
  color: #050505;
  font-weight: 700;
  letter-spacing: 0.5px;
  border: none;
  box-shadow: none;
}

.challenge-page .hub-btn {
  background: rgba(255, 255, 255, 0.08);
  border: 1px solid rgba(255, 255, 255, 0.18);
  color: #f9f8f6;
}

.challenge-page .clue-sidebar {
  background: #1e1e1e;
  border-left-color: rgba(255, 255, 255, 0.08);
}

.challenge-page .clue-item {
  background: #212121;
  border: 1px solid rgba(255, 255, 255, 0.07);
}

.topbar {
  display: grid;
  grid-template-columns: 1fr auto 1fr;
  align-items: center;
  padding: 0 16px;
  background: var(--bg-secondary);
  border-bottom: 1px solid var(--border-color);
  z-index: 10;
}

.topbar-left { display: flex; align-items: center; }
.topbar-center { display: flex; justify-content: center; }
.topbar-right { display: flex; align-items: center; justify-content: flex-end; gap: 16px; }

.hub-btn {
  background: transparent;
  border: 1px solid var(--border-color);
  color: var(--text-secondary);
  font-size: 12px;
  font-weight: 600;
  padding: 4px 12px;
  border-radius: var(--radius);
  letter-spacing: 0.5px;
  cursor: pointer;
  font-family: var(--font-mono);
  transition: border-color var(--transition), color var(--transition);
}

.hub-btn:hover {
  border-color: var(--accent-blue);
  color: var(--accent-blue);
}

/* Room cleared overlay */
.cleared-overlay {
  position: fixed;
  inset: 0;
  z-index: 9999;
  display: flex;
  align-items: center;
  justify-content: center;
  background: rgba(0, 0, 0, 0.75);
  backdrop-filter: blur(4px);
}

.cleared-box {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 12px;
  padding: 40px 64px;
  background: #0d1a0d;
  border: 2px solid #3fb950;
  border-radius: 6px;
  box-shadow: 0 0 40px rgba(63, 185, 80, 0.4);
}

.cleared-icon {
  font-size: 48px;
  color: #3fb950;
  line-height: 1;
}

.cleared-text {
  font-size: 24px;
  font-weight: 700;
  color: #3fb950;
  letter-spacing: 3px;
  text-transform: uppercase;
  font-family: 'Courier New', monospace;
}

.cleared-sub {
  font-size: 13px;
  color: #2a6a2a;
  letter-spacing: 1px;
  font-family: 'Courier New', monospace;
}

.cleared-fade-enter-active,
.cleared-fade-leave-active { transition: opacity 0.3s ease; }
.cleared-fade-enter-from,
.cleared-fade-leave-to { opacity: 0; }

.room-name {
  font-size: 13px;
  font-weight: 600;
  color: var(--text-secondary);
  text-transform: uppercase;
  letter-spacing: 1px;
}

.timer {
  font-family: var(--font-mono);
  font-size: 13px;
  color: var(--accent-orange);
}

.room-body {
  display: flex;
  overflow: hidden;
  min-height: 0;
}

.room-main {
  flex: 1;
  overflow: hidden;
  min-width: 0;
}

/* Sidebar */
.clue-sidebar {
  width: 220px;
  background: var(--bg-secondary);
  border-left: 1px solid var(--border-color);
  display: flex;
  flex-direction: column;
  position: relative;
  transition: width var(--transition);
}

.clue-sidebar.collapsed { width: 24px; }

.sidebar-toggle {
  position: absolute;
  left: -12px;
  top: 50%;
  transform: translateY(-50%);
  width: 24px;
  height: 24px;
  background: var(--bg-surface);
  border: 1px solid var(--border-color);
  border-radius: 50%;
  color: var(--text-secondary);
  font-size: 10px;
  padding: 0;
  display: flex;
  align-items: center;
  justify-content: center;
  z-index: 5;
  cursor: pointer;
}

.sidebar-content {
  padding: 12px;
  overflow-y: auto;
  flex: 1;
}

.sidebar-title {
  font-size: 11px;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 1px;
  color: var(--text-muted);
  margin-bottom: 12px;
}

.no-clues {
  color: var(--text-muted);
  font-size: 12px;
  font-style: italic;
}

.clue-item {
  margin-bottom: 12px;
  padding: 8px;
  background: var(--bg-surface);
  border: 1px solid var(--border-color);
  border-radius: var(--radius);
}

.clue-room {
  display: inline-block;
  font-size: 10px;
  font-weight: 700;
  text-transform: uppercase;
  color: var(--accent-purple);
  letter-spacing: 0.5px;
  margin-bottom: 4px;
}

.clue-text {
  font-size: 12px;
  color: var(--text-secondary);
  line-height: 1.4;
}

/* Progress bar */
.progress-bar {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 24px;
  background: var(--bg-secondary);
  border-top: 1px solid var(--border-color);
  padding: 0 16px;
}

.progress-dot {
  display: flex;
  flex-direction: column;
  align-items: center;
  cursor: default;
}

.progress-dot::before {
  content: '';
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: var(--text-muted);
  display: block;
  transition: background var(--transition);
}

.progress-dot.active::before { background: var(--accent-blue); box-shadow: 0 0 6px var(--accent-blue); }
.progress-dot.done::before { background: var(--accent-green); }

.dot-label {
  font-size: 9px;
  color: var(--text-muted);
  text-transform: uppercase;
  letter-spacing: 0.5px;
  margin-top: 2px;
}

.progress-dot.active .dot-label { color: var(--text-secondary); }
.progress-dot.done .dot-label { color: var(--accent-green); }

/* Per-room theming: apply subtle horror / escape-room vibes */
.room-layout.room--wiki { background: radial-gradient(ellipse at 20% 10%, rgba(40,30,20,0.35), rgba(5,5,10,0.95)); }
.room-layout.room--shell { background: linear-gradient(180deg,#07080a,#0f0f12); }
.room-layout.room--mail { background: linear-gradient(180deg,#11120f,#0b0b0b); }
.room-layout.room--database { background: linear-gradient(180deg,#071018,#0b1518); }
.room-layout.room--search { background: linear-gradient(180deg,#0b1210,#071018); }
.room-layout.room--onion { background: linear-gradient(180deg,#050306,#08040a); color:#cfcbd1; }

.room-layout.room--wiki .topbar { box-shadow: inset 0 -6px 40px rgba(0,0,0,0.6); }
.room-layout.room--wiki .room-name { color: #e8dccf; text-shadow: 0 2px 8px rgba(0,0,0,0.6); }

.room-layout.room--shell .topbar { background: rgba(5,5,8,0.9); }
.room-layout.room--shell .hub-btn { border-color: rgba(80,160,220,0.12); }

/* Slight film grain overlay for horror effect */
.room-layout::after {
  content: ''; pointer-events: none; position: absolute; inset:0; background-image: linear-gradient(transparent 50%, rgba(0,0,0,0.02) 50%); mix-blend-mode: overlay; opacity:0.6; z-index:2;
}

</style>
