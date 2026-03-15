<template>
  <div class="report" @click="menuOpen = false">
    <!-- Top bar (matches challenge room style) -->
    <header class="topbar">
      <div class="topbar-left">
        <div class="logo-wrap" @click.stop="menuOpen = !menuOpen">
          <span class="logo">🔐 CLAIDO CASEBOARD</span>
          <span class="logo-arrow">{{ menuOpen ? '▲' : '▼' }}</span>
        </div>
        <Transition name="dropdown">
          <div v-if="menuOpen" class="logo-menu" @click.stop>
            <div class="menu-item" @click="$router.push('/hub'); menuOpen = false">
              <span class="menu-icon">🏠</span> Return to Hub
            </div>
          </div>
        </Transition>
      </div>

      <div class="topbar-center">
        <div class="room-heading">
          <span class="room-tag">Investigation File</span>
          <span class="room-name-label">Case Report</span>
        </div>
      </div>

      <div class="topbar-right">
        <div class="timer-card">
          <span class="timer-label">Elapsed</span>
          <span class="timer">{{ formattedTime }}</span>
        </div>
      </div>
    </header>

    <!-- Main content -->
    <div class="report-body">
      <!-- Clues column -->
      <div class="report-col">
        <div class="paper-card">
          <div class="card-eyebrow">Evidence Collected</div>
          <h2 class="card-title">Discovered Clues</h2>
          <div v-if="store.discoveredClues.length === 0" class="empty-state">
            No clues discovered yet.
          </div>
          <div v-else class="clues-list">
            <div v-for="clue in store.discoveredClues" :key="clue.id" class="clue-item">
              <span class="clue-room-tag">{{ clue.room.toUpperCase() }}</span>
              <p class="clue-text">{{ getDisplayedClueText(clue) }}</p>
            </div>
          </div>
        </div>

        <div class="paper-card">
          <div class="card-eyebrow">Status Overview</div>
          <h2 class="card-title">Investigation Progress</h2>
          <div class="progress-stat">
            <span class="stat-val">{{ store.completedRooms.length }}</span>
            <span class="stat-sep">/</span>
            <span class="stat-total">6</span>
            <span class="stat-label"> rooms cleared</span>
          </div>
          <div class="rooms-list">
            <div v-for="item in rooms" :key="item.room" class="room-item">
              <span class="room-id">{{ item.room.toUpperCase() }}</span>
              <span :class="['room-badge', `badge--${item.status.toLowerCase()}`]">{{ item.status }}</span>
            </div>
          </div>
        </div>
      </div>

      <!-- Notes column -->
      <div class="report-col report-col--notes">
        <div class="paper-card paper-card--lined">
          <div class="card-eyebrow">Investigator Log</div>
          <h2 class="card-title">Case Notes</h2>
          <textarea
            v-model="store.notes"
            placeholder="Record observations, hypotheses and key insights here..."
            class="notes-textarea"
            @input="persistNotes"
          ></textarea>
        </div>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed } from 'vue'
import { useGameStore } from '../stores/gameStore.js'

const store = useGameStore()
const menuOpen = ref(false)

const formattedTime = computed(() => {
  const s = store.elapsedSeconds
  const h = Math.floor(s / 3600)
  const m = Math.floor((s % 3600) / 60)
  const sec = s % 60
  if (h > 0) return `${h}:${String(m).padStart(2, '0')}:${String(sec).padStart(2, '0')}`
  return `${String(m).padStart(2, '0')}:${String(sec).padStart(2, '0')}`
})

const rooms = computed(() => {
  const allRooms = ['shell', 'database', 'mail', 'wiki', 'search', 'onion', 'vault']
  return allRooms.map(room => {
    if (store.completedRooms.includes(room)) return { room, status: 'CLEARED' }
    if (store.roomCache[room]) return { room, status: 'ENTERED' }
    return { room, status: 'ACTIVE' }
  })
})

function persistNotes() {
  // notes are v-model bound to store.notes; store handles persistence via gameStore actions
}

function getDisplayedClueText(clue) {
  const isMaskedView = store.teamMode === 'team' && store.teamRole === 'good'
  if (clue?.locked && isMaskedView) return 'Clue hidden by the saboteur.'
  return clue?.text || ''
}
</script>

<style scoped>
.report {
  display: grid;
  grid-template-rows: 72px 1fr;
  min-height: 100vh;
  background: transparent;
  overflow: hidden;
  position: relative;
}

/* ── Topbar (mirrors RoomLayout) ───────────────────── */
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
  font-family: var(--font-mono);
}

.room-name-label {
  font-size: 14px;
  font-weight: 600;
  color: var(--text-primary);
  text-transform: uppercase;
  letter-spacing: 2px;
  font-family: var(--font-mono);
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
  font-family: var(--font-mono);
}

.timer {
  font-family: var(--font-mono);
  font-size: 14px;
  color: var(--accent-orange);
  letter-spacing: 1px;
}

/* Dropdown transition */
.dropdown-enter-active,
.dropdown-leave-active {
  transition: opacity 0.15s, transform 0.15s;
}
.dropdown-enter-from,
.dropdown-leave-to {
  opacity: 0;
  transform: translateY(-6px);
}

/* ── Report Body ────────────────────────────────────── */
.report-body {
  display: grid;
  grid-template-columns: 1fr 1.2fr;
  gap: 18px;
  padding: 18px 22px;
  overflow-y: auto;
  align-items: start;
  height: 100%;
}

.report-col {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.report-col--notes {
  height: 100%;
}

/* ── Paper card ─────────────────────────────────────── */
.paper-card {
  background: linear-gradient(180deg, rgba(255, 251, 244, 0.97), rgba(237, 224, 202, 0.97));
  border: 1px solid rgba(88, 63, 41, 0.2);
  border-radius: 12px;
  padding: 18px 20px;
  box-shadow: var(--paper-shadow);
  position: relative;
}

.paper-card--lined {
  display: flex;
  flex-direction: column;
  height: 100%;
  background:
    repeating-linear-gradient(
      180deg,
      transparent 0 27px,
      rgba(160, 130, 95, 0.07) 27px 28px
    ),
    linear-gradient(180deg, rgba(255, 251, 244, 0.97), rgba(237, 224, 202, 0.97));
}

.card-eyebrow {
  font-family: var(--font-mono);
  font-size: 9px;
  font-weight: 700;
  letter-spacing: 2.5px;
  text-transform: uppercase;
  color: var(--text-muted);
  margin-bottom: 4px;
}

.card-title {
  font-family: var(--font-mono);
  font-size: 14px;
  font-weight: 700;
  color: var(--text-primary);
  letter-spacing: 1.5px;
  text-transform: uppercase;
  margin-bottom: 14px;
  padding-bottom: 10px;
  border-bottom: 1px solid var(--border-color);
}

.empty-state {
  font-family: var(--font-mono);
  font-size: 12px;
  color: var(--text-muted);
  font-style: italic;
}

/* Clues */
.clues-list {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.clue-item {
  background: rgba(255, 248, 236, 0.65);
  border: 1px solid var(--border-color);
  border-radius: 8px;
  padding: 10px 14px;
}

.clue-room-tag {
  display: inline-block;
  font-family: var(--font-mono);
  font-size: 9px;
  font-weight: 700;
  letter-spacing: 2px;
  text-transform: uppercase;
  color: var(--accent-orange);
  margin-bottom: 5px;
}

.clue-text {
  font-size: 12px;
  color: var(--text-secondary);
  line-height: 1.5;
  font-family: var(--font-mono);
}

/* Progress */
.progress-stat {
  display: flex;
  align-items: baseline;
  gap: 4px;
  margin-bottom: 14px;
}

.stat-val {
  font-family: var(--font-mono);
  font-size: 28px;
  font-weight: 700;
  color: var(--accent-orange);
}

.stat-sep,
.stat-total {
  font-family: var(--font-mono);
  font-size: 18px;
  color: var(--text-muted);
}

.stat-label {
  font-family: var(--font-mono);
  font-size: 11px;
  color: var(--text-muted);
  text-transform: uppercase;
  letter-spacing: 1px;
}

.rooms-list {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.room-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 8px 12px;
  background: rgba(255, 248, 236, 0.55);
  border: 1px solid var(--border-color);
  border-radius: 6px;
}

.room-id {
  font-family: var(--font-mono);
  font-size: 11px;
  font-weight: 700;
  letter-spacing: 1.5px;
  color: var(--text-primary);
  text-transform: uppercase;
}

.room-badge {
  font-family: var(--font-mono);
  font-size: 9px;
  font-weight: 700;
  letter-spacing: 1px;
  text-transform: uppercase;
  padding: 2px 7px;
  border-radius: 3px;
}

.badge--cleared {
  background: rgba(95, 112, 65, 0.2);
  color: var(--accent-green);
  border: 1px solid rgba(95, 112, 65, 0.3);
}

.badge--entered {
  background: rgba(86, 120, 140, 0.15);
  color: var(--accent-blue);
  border: 1px solid rgba(86, 120, 140, 0.3);
}

.badge--active {
  background: rgba(80, 58, 39, 0.08);
  color: var(--text-muted);
  border: 1px solid var(--border-color);
}

/* Notes */
.notes-textarea {
  flex: 1;
  width: 100%;
  min-height: 340px;
  background: transparent;
  border: none;
  outline: none;
  padding: 4px 0;
  color: var(--text-primary);
  font-family: var(--font-mono);
  font-size: 13px;
  line-height: 28px;
  resize: none;
  caret-color: var(--accent-orange);
}

.notes-textarea::placeholder {
  color: var(--text-muted);
}
</style>
