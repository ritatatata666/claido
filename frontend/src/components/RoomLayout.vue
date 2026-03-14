<template>
  <div class="room-layout" @click="closeAll">
    <!-- Top bar -->
    <header class="topbar">
      <div class="topbar-left">
        <!-- CLAIDO logo menu -->
        <div class="logo-wrap" @click.stop="menuOpen = !menuOpen">
          <span class="logo">🔐 CLAIDO</span>
          <span class="logo-arrow">{{ menuOpen ? '▲' : '▼' }}</span>
        </div>
        <Transition name="dropdown">
          <div v-if="menuOpen" class="logo-menu" @click.stop>
            <div class="menu-item" @click="goHub">
              <span class="menu-icon">🏠</span> Return to Hub
            </div>
            <div class="menu-divider"></div>
            <div class="menu-item" @click="goalsOpen = true; menuOpen = false">
              <span class="menu-icon">🎯</span> Mission Goals
            </div>
            <div class="menu-item" @click="helpOpen = true; menuOpen = false">
              <span class="menu-icon">❓</span> Challenge Help
            </div>
          </div>
        </Transition>
      </div>

      <div class="topbar-center">
        <span class="room-name">{{ roomLabel }}</span>
        <button class="help-btn" :title="'How to solve ' + roomLabel" @click.stop="helpOpen = !helpOpen">?</button>
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
            <p
              class="clue-text"
              :class="{ 'clue-text--masked': clue.locked && isMaskedView }"
            >
              <span v-if="clue.locked && isMaskedView">
                Clue hidden by the saboteur — use the Team Mode console to expose it.
              </span>
              <span v-else>{{ clue.text }}</span>
            </p>
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
        @click="router.push('/' + room.id)"
      >
        <span class="dot-label">{{ room.label }}</span>
      </div>
    </footer>

    <!-- Mission Goals modal -->
    <Transition name="fade">
      <div v-if="goalsOpen" class="modal-backdrop" @click="goalsOpen = false">
        <div class="modal" @click.stop>
          <div class="modal-header">
            <span class="modal-title">🎯 Mission Goals</span>
            <button class="modal-close" @click="goalsOpen = false">✕</button>
          </div>
          <div class="modal-body">
            <p class="modal-intro">You are an investigator assigned to the <strong>Project Nova Incident</strong>. A corporate vault has been breached. Your mission:</p>
            <ol class="goal-list">
              <li>Investigate all <strong>6 challenge rooms</strong> to uncover evidence.</li>
              <li>Collect <strong>4 vault words</strong> hidden across the rooms — each tied to a theme: <em>Time, Location, Identity, Motive</em>.</li>
              <li>Interrogate NPCs in each room for additional leads.</li>
              <li>Enter the <strong>four-word passphrase</strong> in the Vault to reveal the culprit and close the case.</li>
            </ol>
            <div class="progress-summary">
              <span class="prog-label">Rooms cleared:</span>
              <span class="prog-value">{{ store.completedRooms?.length ?? 0 }} / 6</span>
            </div>
          </div>
        </div>
      </div>
    </Transition>

    <!-- Challenge Help modal -->
    <Transition name="fade">
      <div v-if="helpOpen" class="modal-backdrop" @click="helpOpen = false">
        <div class="modal" @click.stop>
          <div class="modal-header">
            <span class="modal-title">❓ {{ roomLabel }} — How to Solve</span>
            <button class="modal-close" @click="helpOpen = false">✕</button>
          </div>
          <div class="modal-body">
            <p v-if="currentHelp" class="help-text">{{ currentHelp.desc }}</p>
            <ul v-if="currentHelp" class="help-steps">
              <li v-for="step in currentHelp.steps" :key="step">{{ step }}</li>
            </ul>
            <p v-else class="help-text">No specific guidance available for this room.</p>
          </div>
        </div>
      </div>
    </Transition>
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
const menuOpen = ref(false)
const goalsOpen = ref(false)
const helpOpen = ref(false)
const isMaskedView = computed(() => store.teamMode === 'team' && store.teamRole === 'good')

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

const roomHelp = {
  shell: {
    desc: 'You have access to NovaCorp\'s internal forensic terminal. A vault word is hidden in an environment file.',
    steps: [
      'Use ls and cd to explore the filesystem.',
      'Find the .env file in the home directory.',
      'Use base64 -d <value> to decode the VAULT_WORD.',
      'Check access.log in the logs/ folder for suspicious activity.',
    ],
  },
  database: {
    desc: 'Query NovaCorp\'s internal database to find evidence of the breach.',
    steps: [
      'Select a table from the sidebar to preview its contents.',
      'Write SQL queries to investigate employees and access logs.',
      'Look for an employee with suspicious after-hours activity.',
      'The culprit\'s ID will be highlighted in red when found.',
    ],
  },
  mail: {
    desc: 'Search through NovaCorp\'s internal email system for incriminating messages.',
    steps: [
      'Browse the Inbox for suspicious emails.',
      'Flag emails that seem relevant using the 🚩 button.',
      'Open an email and click "Submit as evidence" if it contains a vault keyword.',
      'Check the Sent and Flagged folders too.',
    ],
  },
  wiki: {
    desc: 'NovaCorp\'s internal wiki contains classified documents — some with redacted sections.',
    steps: [
      'Browse pages in the sidebar and read their contents.',
      'Look for a page marked with a redacted section (orange dashed border).',
      'Click "Decode ROT13" to reveal the hidden text.',
      'If the decoded text contains a vault keyword, it will be logged as evidence.',
    ],
  },
  search: {
    desc: 'Analyse NovaCorp\'s system logs using the Discover tool to find anomalous activity.',
    steps: [
      'Use the search bar to filter logs by keyword, service, or user.',
      'Filter by "Incident window (00:00–03:00)" to narrow down suspicious entries.',
      'Look for an ERROR-level log from the whistleblower user.',
      'Click a log row to expand its full details.',
    ],
  },
  onion: {
    desc: 'Access NovaCorp\'s dark web presence to find underground communications.',
    steps: [
      'Browse the Forum and Market tabs for suspicious posts.',
      'Look for messages referencing the incident or vault keywords.',
      'Submit any suspicious post as evidence using the flag button.',
    ],
  },
  vault: {
    desc: 'You have collected clues from across the investigation. Now unlock the vault.',
    steps: [
      'Review your collected clues in the Evidence sidebar.',
      'Each clue contains one vault word tied to a theme: Time, Location, Identity, Motive.',
      'Enter all four words as a space-separated passphrase.',
      'Click "Unlock Vault" to reveal the culprit.',
    ],
  },
}

const currentRoomId = computed(() => route.path.replace('/', ''))
const roomLabel = computed(() => roomLabels[currentRoomId.value] || currentRoomId.value)
const currentHelp = computed(() => roomHelp[currentRoomId.value] || null)

function goHub() {
  menuOpen.value = false
  router.push('/hub')
}

function closeAll() {
  menuOpen.value = false
}

// Timer
const elapsed = ref(0)
let timerInterval = null

onMounted(() => {
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
  z-index: 100;
  position: relative;
}

.topbar-left {
  display: flex;
  align-items: center;
  position: relative;
}

.topbar-center {
  display: flex;
  align-items: center;
  gap: 8px;
  justify-content: center;
}

.topbar-right {
  display: flex;
  justify-content: flex-end;
}

/* Logo menu trigger */
.logo-wrap {
  display: flex;
  align-items: center;
  gap: 6px;
  cursor: pointer;
  padding: 4px 8px;
  border-radius: var(--radius);
  transition: background var(--transition);
  user-select: none;
}

.logo-wrap:hover { background: var(--bg-surface); }

.logo {
  font-size: 14px;
  font-weight: 700;
  letter-spacing: 2px;
  color: var(--text-primary);
  font-family: var(--font-mono);
}

.logo-arrow {
  font-size: 8px;
  color: var(--text-muted);
  margin-top: 1px;
}

/* Dropdown menu */
.logo-menu {
  position: absolute;
  top: calc(100% + 6px);
  left: 0;
  min-width: 200px;
  background: var(--bg-surface);
  border: 1px solid var(--border-color);
  border-radius: var(--radius);
  box-shadow: 0 8px 24px rgba(0, 0, 0, 0.5);
  z-index: 200;
  overflow: hidden;
}

.menu-item {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 10px 14px;
  font-size: 13px;
  color: var(--text-secondary);
  cursor: pointer;
  font-family: var(--font-mono);
  transition: background var(--transition), color var(--transition);
}

.menu-item:hover {
  background: var(--bg-primary);
  color: var(--text-primary);
}

.menu-icon { font-size: 14px; }

.menu-divider {
  height: 1px;
  background: var(--border-color);
  margin: 2px 0;
}

/* Room name + help button */
.room-name {
  font-size: 13px;
  font-weight: 600;
  color: var(--text-secondary);
  text-transform: uppercase;
  letter-spacing: 1px;
}

.help-btn {
  width: 18px;
  height: 18px;
  border-radius: 50%;
  background: transparent;
  border: 1px solid var(--text-muted);
  color: var(--text-muted);
  font-size: 11px;
  font-weight: 700;
  padding: 0;
  line-height: 1;
  cursor: pointer;
  transition: border-color var(--transition), color var(--transition);
  font-family: var(--font-mono);
}

.help-btn:hover {
  border-color: var(--accent-purple);
  color: var(--accent-purple);
  opacity: 1;
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

.clue-text--masked {
  color: #9ea0b5;
  font-style: italic;
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

/* Modals */
.modal-backdrop {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.6);
  z-index: 1000;
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 24px;
}

.modal {
  background: var(--bg-secondary);
  border: 1px solid var(--border-color);
  border-radius: var(--radius);
  width: 100%;
  max-width: 480px;
  box-shadow: 0 16px 48px rgba(0, 0, 0, 0.7);
  overflow: hidden;
}

.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 14px 18px;
  border-bottom: 1px solid var(--border-color);
  background: var(--bg-surface);
}

.modal-title {
  font-size: 14px;
  font-weight: 700;
  color: var(--text-primary);
  font-family: var(--font-mono);
  letter-spacing: 1px;
}

.modal-close {
  background: transparent;
  border: none;
  color: var(--text-muted);
  font-size: 14px;
  padding: 2px 6px;
  cursor: pointer;
}

.modal-close:hover { color: var(--text-primary); opacity: 1; }

.modal-body {
  padding: 20px 18px;
  display: flex;
  flex-direction: column;
  gap: 14px;
}

.modal-intro {
  font-size: 13px;
  color: var(--text-secondary);
  line-height: 1.6;
}

.modal-intro strong { color: var(--text-primary); }

.goal-list {
  padding-left: 18px;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.goal-list li {
  font-size: 13px;
  color: var(--text-secondary);
  line-height: 1.5;
}

.goal-list li strong { color: var(--text-primary); }
.goal-list li em { color: var(--accent-purple); font-style: normal; }

.progress-summary {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 10px 14px;
  background: var(--bg-surface);
  border-radius: var(--radius);
  border: 1px solid var(--border-color);
}

.prog-label {
  font-size: 12px;
  color: var(--text-muted);
  font-family: var(--font-mono);
  text-transform: uppercase;
  letter-spacing: 1px;
}

.prog-value {
  font-size: 14px;
  font-weight: 700;
  color: var(--accent-green);
  font-family: var(--font-mono);
}

.help-text {
  font-size: 13px;
  color: var(--text-secondary);
  line-height: 1.6;
}

.help-steps {
  padding-left: 18px;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.help-steps li {
  font-size: 13px;
  color: var(--text-secondary);
  line-height: 1.5;
}

/* Dropdown transition */
.dropdown-enter-active { transition: opacity 0.15s ease, transform 0.15s ease; }
.dropdown-leave-active { transition: opacity 0.1s ease, transform 0.1s ease; }
.dropdown-enter-from { opacity: 0; transform: translateY(-6px); }
.dropdown-leave-to { opacity: 0; transform: translateY(-6px); }

/* Fade transition */
.fade-enter-active, .fade-leave-active { transition: opacity 0.2s ease; }
.fade-enter-from, .fade-leave-to { opacity: 0; }
</style>
