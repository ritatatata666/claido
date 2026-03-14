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

    <main class="hub-main">
      <div class="map-intro">
        <div class="map-intro__label">FIELD GUIDE</div>
        <p>
          Welcome to the mansion floor plan. Tap a chamber to move the investigation there; each tile glows brighter as you clear that area.
        </p>
      </div>

      <MansionMap
        :rooms="mansionRooms"
        :active-room="activeRoom"
        @room-select="handleRoomSelect"
      />
    </main>
  </div>
</template>

<script setup>
import { computed, onMounted, onUnmounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import MansionMap from '../components/MansionMap.vue'
import { useGameStore } from '../stores/gameStore.js'

const router = useRouter()
const route = useRoute()
const store = useGameStore()

const roomBlueprints = [
  {
    id: 'hall',
    area: 'hall',
    roomName: 'Hall',
    theme: 'gold',
    route: '/hub',
    alias: null,
    desc: 'Central corridor and foyer',
    backgroundImage:
      'https://images.unsplash.com/photo-1502673530728-f79b4cab31b1?auto=format&fit=crop&w=1200&q=80',
    challengeTitle: 'Main Gateway',
  },
  {
    id: 'kitchen',
    area: 'kitchen',
    roomName: 'Kitchen',
    theme: 'green',
    route: '/database',
    alias: 'database',
    desc: 'Messy notes, receipts and delivered packages',
    backgroundImage:
      'https://images.unsplash.com/photo-1505693416388-ac5ce068fe85?auto=format&fit=crop&w=1200&q=80',
    challengeTitle: 'Database',
  },
  {
    id: 'ballroom',
    area: 'ballroom',
    roomName: 'Ballroom',
    theme: 'gold',
    route: '/wiki',
    alias: 'wiki',
    desc: 'Dark ballroom with broken chandeliers',
    backgroundImage:
      'https://images.unsplash.com/photo-1469474968028-56623f02e42e?auto=format&fit=crop&w=1200&q=80',
    challengeTitle: 'Wiki',
  },
  {
    id: 'dining',
    area: 'dining',
    roomName: 'Dining Room',
    theme: 'brown',
    route: '/mail',
    alias: 'mail',
    desc: 'Long table of patterns — query the records for clue feasts',
    backgroundImage:
      'https://images.unsplash.com/photo-1504674900247-0877df9cc836?auto=format&fit=crop&w=1200&q=80',
    challengeTitle: 'Mail',
  },
  {
    id: 'conservatory',
    area: 'conservatory',
    roomName: 'Conservatory',
    theme: 'green',
    route: '/onion',
    alias: 'onion',
    desc: 'Overgrown greenhouse full of moonlit leaves',
    backgroundImage:
      'https://images.unsplash.com/photo-1501004318641-b39e6451bec6?auto=format&fit=crop&w=1200&q=80',
    challengeTitle: 'Onion',
  },
  {
    id: 'billiard',
    area: 'billiard',
    roomName: 'Billiard Room',
    theme: 'brown',
    route: '/vault',
    alias: 'vault',
    desc: 'Patterned pool tables and statistical break shots',
    backgroundImage:
      'https://images.unsplash.com/photo-1489515217757-5fd1be406fef?auto=format&fit=crop&w=900&q=80',
    challengeTitle: 'Vault',
  },
  {
    id: 'library',
    area: 'library',
    roomName: 'Library',
    theme: 'brown',
    route: '/search',
    alias: 'search',
    desc: 'Gothic shelves and candlelight',
    backgroundImage:
      'https://images.unsplash.com/photo-1511993226950-233b77a4cb4d?auto=format&fit=crop&w=1000&q=80',
    challengeTitle: 'Search',
  },
  {
    id: 'lounge',
    area: 'lounge',
    roomName: 'Lounge',
    theme: 'gold',
    route: '/mail',
    alias: 'mail',
    desc: 'Dim fireside planning room',
    backgroundImage:
      'https://images.unsplash.com/photo-1484154218962-a197022b5858?auto=format&fit=crop&w=1200&q=80',
    challengeTitle: 'Mail',
  },
  {
    id: 'study',
    area: 'study',
    roomName: 'Study',
    theme: 'green',
    route: '/shell',
    alias: 'shell',
    desc: 'Detective desk with scattered case files',
    backgroundImage:
      'https://images.unsplash.com/photo-1529333166437-7750a6dd5a70?auto=format&fit=crop&w=1200&q=80',
    challengeTitle: 'Shell',
  },
]

function getRoomStatus(roomId) {
  if (!roomId) return 'ACTIVE'
  if (store.completedRooms.includes(roomId)) return 'CLEARED'
  if (store.roomCache[roomId]) return 'IN PROGRESS'
  return 'ACTIVE'
}

const mansionRooms = computed(() =>
  roomBlueprints.map(room => ({
    ...room,
    status: getRoomStatus(room.alias ?? room.id),
  }))
)

const progressPercent = computed(() => {
  const completed = store.completedRooms.length
  return Math.round((completed / 6) * 100)
})

const clearedCount = computed(() => store.completedRooms.length)

const activeRoom = computed(() => {
  const match = roomBlueprints.find((room) => room.route === route.path)
  return match?.id ?? 'hall'
})

function handleRoomSelect(roomId) {
  const selection = roomBlueprints.find((room) => room.id === roomId)
  if (!selection) return
  if (selection.route === '/hub') {
    router.push('/hub')
    return
  }
  router.push(selection.route)
}

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
.hub {
  min-height: 100vh;
  background: #03040a;
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

.hub-topbar {
  position: relative;
  z-index: 1;
  display: grid;
  grid-template-columns: 1fr auto 1fr;
  align-items: center;
  padding: 0 28px;
  height: 64px;
  background: #0d0d14;
  border-bottom: 1px solid #111320;
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
  background: #11131f;
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

.hub-main {
  position: relative;
  z-index: 1;
  flex: 1;
  padding: 32px 36px 48px;
  display: flex;
  flex-direction: column;
  gap: 20px;
  max-width: 1200px;
  margin: 0 auto;
  width: 100%;
}

.map-intro {
  background: rgba(255, 255, 255, 0.03);
  border: 1px solid rgba(255, 255, 255, 0.05);
  padding: 20px 24px;
  border-radius: 10px;
  box-shadow: 0 10px 40px rgba(0, 0, 0, 0.35);
}

.map-intro__label {
  font-size: 10px;
  letter-spacing: 4px;
  text-transform: uppercase;
  color: #6cf3ff;
  margin-bottom: 6px;
}

.map-intro p {
  margin: 0;
  font-size: 14px;
  color: #c8c9d6;
  line-height: 1.6;
}

@media (max-width: 900px) {
  .hub-topbar {
    grid-template-columns: 1fr;
    height: auto;
    padding: 12px 20px;
    gap: 12px;
  }

  .hub-timer {
    align-items: flex-start;
  }

  .hub-main {
    padding: 24px 20px 32px;
  }
}
@media (max-width: 640px) {
  .map-intro {
    padding: 16px 18px;
  }
}
</style>
