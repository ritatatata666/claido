<template>
  <div class="room-layout">
    <!-- Top bar -->
    <header class="topbar">
      <div class="topbar-left">
        <span class="logo">🔐 CLAIDO</span>
      </div>
      <div class="topbar-center">
        <span class="room-name">{{ roomLabel }}</span>
      </div>
      <div class="topbar-right">
        <span class="timer">{{ formattedTime }}</span>
        <button v-if="nextRoom" class="next-room-btn" @click="goToRoom(nextRoom)">
          {{ nextRoom.label }} →
        </button>
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

    <!-- Progress dots -->
    <footer class="progress-bar">
      <div
        v-for="room in rooms"
        :key="room.id"
        :class="['progress-dot', {
          active: currentRoomId === room.id,
          done: store.isRoomComplete(room.id)
        }]"
        :title="room.label"
        @click="goToRoom(room)"
      >
        <span class="dot-label">{{ room.label }}</span>
      </div>
    </footer>
  </div>
</template>

<script setup>
import { ref, computed, onMounted, onUnmounted } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useGameStore } from '../stores/gameStore.js'

const store = useGameStore()
const route = useRoute()
const router = useRouter()
const sidebarCollapsed = ref(false)

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
const roomLabel = computed(() => roomLabels[currentRoomId.value] || currentRoomId.value)

const currentRoomIndex = computed(() => rooms.findIndex(r => r.id === currentRoomId.value))
const nextRoom = computed(() => rooms[currentRoomIndex.value + 1] ?? null)

function goToRoom(room) {
  router.push('/' + room.id)
}

// Timer
const elapsed = ref(0)
let timerInterval = null

onMounted(() => {
  // Redirect to landing if no active session
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

.topbar {
  display: grid;
  grid-template-columns: 1fr auto 1fr;
  align-items: center;
  padding: 0 16px;
  background: var(--bg-secondary);
  border-bottom: 1px solid var(--border-color);
  z-index: 10;
}

.topbar-left {
  display: flex;
  align-items: center;
}

.topbar-center {
  display: flex;
  justify-content: center;
}

.topbar-right {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 16px;
}

.next-room-btn {
  background: transparent;
  border: 1px solid var(--border-color);
  color: var(--text-secondary);
  font-size: 12px;
  font-weight: 600;
  padding: 4px 12px;
  border-radius: var(--radius);
  letter-spacing: 0.5px;
  transition: border-color var(--transition), color var(--transition);
}

.next-room-btn:hover {
  border-color: var(--accent-blue);
  color: var(--accent-blue);
  opacity: 1;
}

.logo {
  font-size: 14px;
  font-weight: 700;
  letter-spacing: 2px;
  color: var(--text-primary);
  font-family: var(--font-mono);
}

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

.clue-sidebar.collapsed {
  width: 24px;
}

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
  position: relative;
  cursor: pointer;
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

.progress-dot.active::before {
  background: var(--accent-blue);
  box-shadow: 0 0 6px var(--accent-blue);
}

.progress-dot.done::before {
  background: var(--accent-green);
}

.dot-label {
  font-size: 9px;
  color: var(--text-muted);
  text-transform: uppercase;
  letter-spacing: 0.5px;
  margin-top: 2px;
}

.progress-dot.active .dot-label {
  color: var(--text-secondary);
}

.progress-dot.done .dot-label {
  color: var(--accent-green);
}
</style>
