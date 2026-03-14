<template>
  <div class="room-layout" @click="closeAll">
    <!-- Top bar -->
    <header class="topbar">
      <div class="topbar-left">
        <!-- CLAIDO logo menu -->
        <div class="logo-wrap" @click.stop="menuOpen = !menuOpen">
          <span class="logo">🔐 CLAIDO CASEBOARD</span>
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
        <div class="room-heading">
          <span class="room-tag flicker-slow">Investigation Node</span>
          <span class="room-name flicker-text">{{ roomLabel }}</span>
        </div>
        <button class="help-btn" :title="'How to solve ' + roomLabel" @click.stop="helpOpen = !helpOpen">?</button>
      </div>

      <div class="topbar-right">
        <div class="timer-card">
          <span class="timer-label">Elapsed</span>
          <span class="timer">{{ formattedTime }}</span>
        </div>
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
          <h3 class="sidebar-title">Evidence Board</h3>
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
  grid-template-rows: 72px 1fr 58px;
  height: 100vh;
  background: transparent;
  overflow: hidden;
  position: relative;
}

.room-layout::before {
  content: '';
  position: absolute;
  inset: 0;
  background:
    radial-gradient(circle at 10% 12%, rgba(255, 247, 227, 0.2), transparent 20%),
    radial-gradient(circle at 88% 16%, rgba(255, 247, 227, 0.14), transparent 16%),
    linear-gradient(180deg, rgba(160, 113, 73, 0.12), rgba(130, 87, 50, 0.08));
  pointer-events: none;
  z-index: 0;
}

.topbar {
  display: grid;
  grid-template-columns: 1fr auto 1fr;
  align-items: center;
  padding: 14px 20px 0;
  z-index: 100;
  position: relative;
}

.topbar::before {
  content: '';
  position: absolute;
  inset: 10px 18px 0;
  background: linear-gradient(180deg, rgba(255, 251, 244, 0.96), rgba(237, 224, 202, 0.96));
  border: 1px solid rgba(88, 63, 41, 0.2);
  border-radius: 14px 14px 6px 6px;
  box-shadow: var(--paper-shadow);
  z-index: -1;
}

.topbar::after {
  content: '';
  position: absolute;
  top: 2px;
  left: 36px;
  width: 92px;
  height: 24px;
  background: rgba(240, 220, 184, 0.75);
  border-radius: 3px;
  transform: rotate(-3deg);
  box-shadow: 0 3px 6px rgba(90, 60, 35, 0.08);
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
  padding: 6px 10px;
  border-radius: 8px;
  transition: background var(--transition), transform var(--transition);
  user-select: none;
}

.logo-wrap:hover {
  background: rgba(255, 248, 236, 0.55);
  transform: translateY(-1px);
}

.logo {
  font-size: 13px;
  font-weight: 700;
  letter-spacing: 1.6px;
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
  background: linear-gradient(180deg, rgba(255, 250, 242, 0.98), rgba(240, 228, 208, 0.98));
  border: 1px solid rgba(88, 63, 41, 0.22);
  border-radius: 10px;
  box-shadow: var(--paper-shadow);
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
  background: rgba(189, 149, 109, 0.12);
  color: var(--text-primary);
}

.menu-icon { font-size: 14px; }

.menu-divider {
  height: 1px;
  background: var(--border-color);
  margin: 2px 0;
}

/* Room name + help button */
.room-heading {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 2px;
}

.room-tag {
  font-size: 9px;
  font-weight: 700;
  color: var(--text-muted);
  text-transform: uppercase;
  letter-spacing: 2px;
}

.room-name {
  font-size: 14px;
  font-weight: 600;
  color: var(--text-primary);
  text-transform: uppercase;
  letter-spacing: 2px;
}

.help-btn {
  width: 24px;
  height: 24px;
  border-radius: 50%;
  background: rgba(255, 247, 233, 0.9);
  border: 1px solid rgba(88, 63, 41, 0.26);
  color: var(--accent-red);
  font-size: 11px;
  font-weight: 700;
  padding: 0;
  line-height: 1;
  cursor: pointer;
  transition: border-color var(--transition), color var(--transition);
  font-family: var(--font-mono);
}

.help-btn:hover {
  border-color: rgba(185, 70, 54, 0.45);
  color: var(--accent-red);
  opacity: 1;
}

.timer-card {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  padding: 6px 10px;
  border-radius: 8px;
  background: rgba(255, 248, 236, 0.62);
}

.timer-label {
  font-size: 9px;
  color: var(--text-muted);
  text-transform: uppercase;
  letter-spacing: 2px;
}

.timer {
  font-family: var(--font-mono);
  font-size: 14px;
  color: var(--accent-orange);
  letter-spacing: 1px;
}

.room-body {
  display: flex;
  overflow: hidden;
  min-height: 0;
  gap: 18px;
  padding: 18px 22px 0;
  position: relative;
  z-index: 1;
}

.room-main {
  flex: 1;
  overflow: hidden;
  min-width: 0;
  background: rgba(104, 72, 44, 0.1);
  border-radius: 24px 24px 12px 12px;
  padding: 12px;
  box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.22), 0 18px 32px rgba(82, 52, 27, 0.12);
}

.clue-sidebar {
  width: 260px;
  background: linear-gradient(180deg, rgba(255, 251, 244, 0.96), rgba(235, 222, 202, 0.96));
  border: 1px solid rgba(88, 63, 41, 0.2);
  border-radius: 12px 12px 0 0;
  display: flex;
  flex-direction: column;
  position: relative;
  transition: width var(--transition);
  box-shadow: var(--paper-shadow);
  overflow: hidden;
}

.clue-sidebar.collapsed {
  width: 24px;
  border-radius: 14px;
}

.sidebar-toggle {
  position: absolute;
  left: -12px;
  top: 50%;
  transform: translateY(-50%);
  width: 24px;
  height: 24px;
  background: rgba(255, 248, 236, 0.96);
  border: 1px solid rgba(88, 63, 41, 0.22);
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
  padding: 18px 14px 14px;
  overflow-y: auto;
  flex: 1;
}

.sidebar-title {
  font-size: 11px;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 2px;
  color: var(--accent-red);
  margin-bottom: 16px;
}

.no-clues {
  color: var(--text-muted);
  font-size: 12px;
  font-style: italic;
}

.clue-item {
  margin-bottom: 14px;
  padding: 12px 10px 10px;
  background: rgba(255, 250, 242, 0.82);
  border: 1px solid rgba(88, 63, 41, 0.18);
  border-radius: 8px;
  box-shadow: 0 10px 18px rgba(82, 52, 27, 0.08);
  position: relative;
}

.clue-item::before {
  content: '';
  position: absolute;
  top: -7px;
  left: 14px;
  width: 12px;
  height: 12px;
  border-radius: 50%;
  background: #4d78a5;
  box-shadow: 0 2px 6px rgba(0, 0, 0, 0.24), inset 0 1px 1px rgba(255, 255, 255, 0.65);
}

.clue-room {
  display: inline-block;
  font-size: 10px;
  font-weight: 700;
  text-transform: uppercase;
  color: var(--accent-red);
  letter-spacing: 1.2px;
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

/*Progress Bar*/
.clue-text--masked {
  color: #9ea0b5;
  font-style: italic;
}

.progress-bar {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 24px;
  margin: 0 22px 18px;
  background: linear-gradient(180deg, rgba(255, 251, 244, 0.94), rgba(236, 223, 202, 0.94));
  border: 1px solid rgba(88, 63, 41, 0.2);
  border-radius: 0 0 14px 14px;
  padding: 12px 16px;
  box-shadow: var(--paper-shadow);
  position: relative;
  z-index: 1;
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
  width: 10px;
  height: 10px;
  border-radius: 50%;
  background: rgba(120, 88, 60, 0.45);
  display: block;
  transition: background var(--transition);
}

.progress-dot.active::before {
  background: var(--accent-red);
  box-shadow: 0 0 0 3px rgba(185, 70, 54, 0.12);
}

.progress-dot.done::before {
  background: var(--accent-green);
}

.dot-label {
  font-size: 10px;
  color: var(--text-muted);
  text-transform: uppercase;
  letter-spacing: 1px;
  margin-top: 4px;
}

.progress-dot.active .dot-label {
  color: var(--text-secondary);
}

.progress-dot.done .dot-label {
  color: var(--accent-green);
}

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
  background: linear-gradient(180deg, rgba(255, 251, 244, 0.98), rgba(237, 224, 203, 0.98));
  border: 1px solid rgba(88, 63, 41, 0.22);
  border-radius: 12px;
  width: 100%;
  max-width: 480px;
  box-shadow: 0 24px 48px rgba(46, 28, 14, 0.24);
  overflow: hidden;
}

.modal-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 14px 18px;
  border-bottom: 1px solid rgba(88, 63, 41, 0.14);
  background: rgba(255, 247, 233, 0.7);
}

.modal-title {
  font-size: 14px;
  font-weight: 700;
  color: var(--text-primary);
  font-family: var(--font-mono);
  letter-spacing: 1.2px;
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
  background: rgba(255, 248, 236, 0.72);
  border-radius: 8px;
  border: 1px solid rgba(88, 63, 41, 0.14);
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
