<template>
  <div class="landing">
    <div class="landing-board">
      <span class="board-thread board-thread--one"></span>
      <span class="board-thread board-thread--two"></span>
      <span class="board-thread board-thread--three"></span>

      <div class="top-bar evidence-strip">
        <span class="classified-badge">● Active Case</span>
        <span class="case-file">CASE FILE #NC-2025-0303</span>
      </div>

      <!-- Glitchy heading -->
      <div class="hero-block">
        <h1 class="claido-heading" data-text="CLAIDO">CLAIDO</h1>
        <p class="hero-subtitle">NovaCorp Internal Breach — Investigator Access Only</p>
      </div>

      <!-- Briefing card styled as classified document -->
      <div class="briefing-card">
        <div class="watermark">TOP SECRET</div>
        <div class="card-inner">
          <div class="card-header">
            <div class="card-title-block">
              <span class="card-stamp-label">INCIDENT REPORT</span>
              <span class="card-date">2025-03-03</span>
            </div>
          </div>
          <p class="card-summary">
            A corporate breach occurred overnight at NovaCorp headquarters.
            Sensitive vault data was compromised. The culprit is still at large.
            You have been deployed as a forensic investigator with access to
            seven internal systems. Find the culprit. Unlock the vault.
          </p>
          <ul class="room-list">
            <li v-for="room in rooms" :key="room.id">
              <span class="bullet">▪</span>
              <strong>{{ room.label }}</strong> — {{ room.desc }}
            </li>
          </ul>
          <div class="declassified-stamp">DECLASSIFIED</div>
        </div>
      </div>

      <!-- Error -->
      <div v-if="error" class="error-msg">{{ error }}</div>

      <!-- CTA button -->
      <button
        class="start-btn"
        :disabled="loading"
        @click="startGame"
      >
        <span v-if="loading">
          <span class="spinner-dot"></span>
          Generating case file...
        </span>
        <span v-else>BEGIN INVESTIGATION</span>
      </button>

      <p class="disclaimer">Session expires when tab closes. Each case is AI-generated.</p>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useGameStore } from '../stores/gameStore.js'

const router = useRouter()
const store = useGameStore()
const loading = ref(false)
const error = ref('')
const joinCodeInput = ref('')
const joinName = ref('')
const joinLoading = ref(false)
const joinError = ref('')
const selectedMode = ref('standard')

const rooms = [
  { id: 'shell', label: 'NovaShell', desc: 'Explore the internal filesystem' },
  { id: 'database', label: 'NovaCrime DB', desc: 'Query employee and access records' },
  { id: 'mail', label: 'NovaMail', desc: 'Read intercepted corporate emails' },
  { id: 'wiki', label: 'NovaWiki', desc: 'Browse classified internal documents' },
  { id: 'search', label: 'NovaSearch', desc: 'Analyse 50,000 system log entries' },
  { id: 'onion', label: 'The Onion', desc: 'Browse the dark web for leads' },
  { id: 'vault', label: 'Vault', desc: 'Enter the four-word passphrase to win' },
]

async function startSoloSession() {
  loading.value = true
  error.value = ''
  try {
    store.configureTeamMode('standard')
    await store.createSession()
    router.push('/hub')
  } catch (e) {
    error.value = e.message || 'Failed to connect to backend. Is it running?'
  } finally {
    loading.value = false
  }
}

async function joinExistingSession() {
  if (!joinCodeInput.value.trim()) {
    joinError.value = 'Enter a join code first.'
    return
  }
  joinLoading.value = true
  joinError.value = ''
  try {
    store.configureTeamMode('team')
    await store.joinTeamSession(joinCodeInput.value.trim(), joinName.value.trim() || 'Investigator')
    router.push('/hub')
  } catch (e) {
    joinError.value = e.message || 'Could not join that session.'
  } finally {
    joinLoading.value = false
  }
}

async function createTeamRoom() {
  loading.value = true
  error.value = ''
  joinError.value = ''
  try {
    store.configureTeamMode('team')
    await store.createTeamRoom('Host Investigator')
    router.push('/hub')
  } catch (e) {
    error.value = e.message || 'Failed to create a team room. Is the backend running?'
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.landing {
  min-height: 100vh;
  background: #0a0a0a;
  display: flex;
  justify-content: center;
  padding: 40px 24px 60px;
  overflow-y: auto;
  position: relative;
  width: min(1120px, 100%);
  display: flex;
  flex-direction: column;
  gap: 24px;
}

.board-thread {
  position: absolute;
  height: 3px;
  border-radius: 999px;
  background: linear-gradient(90deg, rgba(133, 18, 21, 0.2), rgba(173, 29, 33, 0.78), rgba(133, 18, 21, 0.2));
  box-shadow: 0 1px 4px rgba(102, 17, 20, 0.16);
  pointer-events: none;
}

.board-thread--one {
  top: 92px;
  right: 26%;
  width: 220px;
  transform: rotate(-12deg);
}

.board-thread--two {
  top: 250px;
  left: 38%;
  width: 280px;
  transform: rotate(10deg);
}

.board-thread--three {
  bottom: 84px;
  left: 16%;
  width: 240px;
  transform: rotate(-15deg);
}

.evidence-strip,
.evidence-card {
  position: relative;
  z-index: 1;
  width: 100%;
  max-width: 660px;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 28px;
}

/* ── Top bar ──────────────────────────────────────────── */
.top-bar {
  width: 100%;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.evidence-card {
  border-radius: 8px;
  overflow: hidden;
}

.evidence-card--wide {
  display: flex;
  justify-content: space-between;
  align-items: flex-end;
  gap: 24px;
  padding: 28px 32px;
}

.note-pin {
  position: absolute;
  top: -8px;
  left: 24px;
  width: 16px;
  height: 16px;
  border-radius: 50%;
  box-shadow: 0 2px 6px rgba(0, 0, 0, 0.24), inset 0 1px 1px rgba(255, 255, 255, 0.68);
}

.note-pin--red { background: #ca4b42; }
.note-pin--blue { background: #4d78a5; }
.note-pin--gold { background: #bc8c2c; }

.top-bar {
  z-index: 1;
}

.classified-badge,
.case-file,
.hero-kicker,
.hero-tag,
.card-stamp-label,
.card-date,
.snapshot-label,
.start-label,
.disclaimer {
  font-family: var(--font-mono);
}

.classified-badge {
  color: var(--accent-red);
  font-size: 12px;
  font-weight: 700;
  letter-spacing: 2px;
  text-transform: uppercase;
}

.case-file {
  color: var(--text-muted);
  font-size: 11px;
  letter-spacing: 1.2px;
}

.hero-copy {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.hero-kicker {
  font-size: 11px;
  letter-spacing: 3px;
  text-transform: uppercase;
  color: var(--text-muted);
}

.claido-heading {
  margin: 0;
  font-size: clamp(52px, 10vw, 92px);
  letter-spacing: 10px;
  color: #311d0e;
  text-transform: uppercase;
  font-family: var(--font-display);
}

.hero-subtitle {
  margin: 0;
  max-width: 620px;
  font-size: 15px;
  line-height: 1.65;
  color: var(--text-secondary);
}

.hero-tag {
  align-self: flex-start;
  padding: 10px 14px;
  border: 2px solid rgba(185, 70, 54, 0.6);
  color: rgba(185, 70, 54, 0.82);
  text-transform: uppercase;
  letter-spacing: 2px;
  font-size: 12px;
  font-weight: 800;
  transform: rotate(-6deg);
}

.briefing-grid {
  display: grid;
  grid-template-columns: minmax(0, 1.45fr) minmax(280px, 0.8fr);
  gap: 24px;
  align-items: start;
}

.briefing-card {
  min-height: 100%;
}

.briefing-card::after,
.snapshot-card::after,
.start-card::after {
  content: '';
  position: absolute;
  inset: 0;
  background: repeating-linear-gradient(180deg, transparent 0 24px, rgba(121, 92, 67, 0.08) 24px 25px);
  pointer-events: none;
}

.watermark {
  position: absolute;
  top: 48%;
  left: 52%;
  transform: translate(-50%, -50%) rotate(-24deg);
  font-size: clamp(44px, 9vw, 88px);
  letter-spacing: 6px;
  color: rgba(185, 70, 54, 0.08);
  font-weight: 900;
  pointer-events: none;
}

.card-inner {
  position: relative;
  z-index: 1;
  padding: 28px 28px 32px;
}

.card-header {
  margin-bottom: 16px;
  padding-bottom: 12px;
  border-bottom: 2px solid rgba(125, 96, 69, 0.24);
}

.card-title-block {
  display: flex;
  justify-content: space-between;
  align-items: baseline;
  gap: 12px;
}

.card-stamp-label {
  font-size: 13px;
  font-weight: 700;
  letter-spacing: 2px;
  text-transform: uppercase;
}

.card-date {
  font-size: 12px;
  color: var(--text-muted);
}

.card-summary {
  margin: 0 0 20px;
  color: var(--text-secondary);
  line-height: 1.8;
  font-size: 15px;
}

.room-list {
  list-style: none;
  display: grid;
  gap: 10px;
  margin: 0;
  padding: 0;
}

.room-list li {
  display: grid;
  grid-template-columns: auto auto 1fr;
  gap: 10px;
  align-items: baseline;
  color: var(--text-secondary);
}

.bullet {
  color: var(--accent-red);
}

.room-list strong {
  color: var(--text-primary);
}

.declassified-stamp {
  display: inline-flex;
  margin-top: 22px;
  padding: 5px 12px;
  border: 2px solid rgba(185, 70, 54, 0.65);
  color: rgba(185, 70, 54, 0.82);
  font-family: var(--font-mono);
  font-size: 12px;
  font-weight: 800;
  letter-spacing: 2px;
  text-transform: uppercase;
  transform: rotate(-5deg);
}

.side-column {
  display: grid;
  gap: 20px;
}

.snapshot-card,
.start-card {
  padding: 22px 20px 20px;
}

.snapshot-label,
.start-label {
  position: relative;
  z-index: 1;
  font-size: 11px;
  letter-spacing: 2px;
  text-transform: uppercase;
  color: var(--accent-red);
  margin: 0 0 12px;
}

.snapshot-list {
  position: relative;
  z-index: 1;
  margin: 0;
  padding-left: 18px;
  display: grid;
  gap: 8px;
  color: var(--text-secondary);
}

.start-btn {
  width: 100%;
  padding: 16px 20px;
  text-transform: uppercase;
  letter-spacing: 3px;
  font-family: var(--font-mono);
  font-weight: 800;
}

.spinner-dot {
  display: inline-block;
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: #fff;
  margin-right: 10px;
  vertical-align: middle;
  animation: spinner-pulse 0.8s ease-in-out infinite;
}

@keyframes spinner-pulse {
  0%, 100% { opacity: 1; transform: scale(1); }
  50% { opacity: 0.3; transform: scale(0.6); }
}

.disclaimer {
  position: relative;
  z-index: 1;
  margin: 14px 0 0;
  color: var(--text-muted);
  font-size: 11px;
  line-height: 1.6;
}

.error-msg {
  padding: 14px 16px;
  color: #8f2018;
  border-color: rgba(143, 32, 24, 0.3);
}

@media (max-width: 860px) {
  .briefing-grid {
    grid-template-columns: 1fr;
  }

  .evidence-card--wide {
    flex-direction: column;
    align-items: flex-start;
  }
}

@media (max-width: 640px) {
  .landing {
    padding: 22px 14px 40px;
  }

  .top-bar {
    flex-direction: column;
    align-items: flex-start;
    gap: 6px;
  }

  .card-title-block,
  .room-list li {
    grid-template-columns: 1fr;
    display: grid;
  }
}

.mode-card {
  width: 100%;
  background: rgba(6, 6, 6, 0.78);
  border: 1px solid rgba(255, 255, 255, 0.08);
  border-radius: 16px;
  padding: 20px 24px;
  display: flex;
  flex-direction: column;
  gap: 14px;
  box-shadow: 0 20px 50px rgba(0, 0, 0, 0.65);
}

.mode-card__title {
  margin: 0;
  font-size: 13px;
  letter-spacing: 1.2px;
  text-transform: uppercase;
  color: #9999a3;
}

.mode-card__toggle {
  display: flex;
  gap: 12px;
}

.mode-card__tab {
  flex: 1;
  border: 1px solid rgba(255, 255, 255, 0.08);
  border-radius: 14px;
  background: rgba(8, 10, 20, 0.95);
  color: rgba(255, 255, 255, 0.85);
  font-size: 13px;
  padding: 12px 16px;
  cursor: pointer;
  font-weight: 700;
  letter-spacing: 1px;
  text-transform: uppercase;
  transition: transform 0.2s ease, box-shadow 0.2s ease, background 0.3s ease;
  box-shadow: inset 0 -2px 0 rgba(255, 255, 255, 0.08);
}

.mode-card__tab--standard {
  background: linear-gradient(135deg, rgba(24, 10, 37, 0.9), rgba(39, 18, 64, 0.9));
}

.mode-card__tab--team {
  background: linear-gradient(135deg, rgba(7, 8, 24, 0.95), rgba(101, 24, 120, 0.9));
}

.mode-card__tab:not(.active) {
  opacity: 0.75;
}

.mode-card__tab.active {
  border-color: #c48bff;
  box-shadow: 0 12px 28px rgba(196, 139, 255, 0.35), inset 0 1px 0 rgba(255, 255, 255, 0.3);
  transform: translateY(-2px);
  color: #fff;
}

.mode-card__desc {
  margin: 0;
  font-size: 13px;
  color: #c3c3cd;
}

.mode-card__team {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.mode-card__subheading {
  margin: 0;
  font-size: 11px;
  letter-spacing: 1px;
  text-transform: uppercase;
  color: #8b8b99;
}

.mode-card__team-option {
  border: 1px solid rgba(255, 255, 255, 0.08);
  border-radius: 12px;
  padding: 12px;
  display: flex;
  gap: 10px;
  align-items: center;
  cursor: pointer;
  color: #f5f5f7;
  font-size: 13px;
}

.mode-card__team-option input {
  margin: 0;
}

.mode-card__team-option strong {
  font-size: 14px;
}

.mode-card__team-option span {
  display: block;
  font-size: 12px;
  color: #aaaab5;
}

.mode-card__team-text {
  margin: 0;
  font-size: 13px;
  color: #c0c0c9;
  line-height: 1.4;
}

.team-lobby-grid {
  width: 100%;
  display: grid;
  gap: 18px;
}

@media (min-width: 780px) {
  .team-lobby-grid {
    grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
    align-items: stretch;
  }
}

.team-lobby-card {
  width: 100%;
  background: linear-gradient(135deg, rgba(14, 15, 30, 0.95), rgba(48, 22, 63, 0.95));
  border: 1px solid rgba(255, 255, 255, 0.1);
  border-radius: 18px;
  padding: 20px;
  box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.08), 0 18px 40px rgba(0, 0, 0, 0.6);
  min-height: 280px;
}

.team-lobby-card__inner {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.team-lobby-card__actions {
  margin-top: 12px;
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.team-lobby-card__button {
  border: none;
  border-radius: 12px;
  padding: 14px;
  background: linear-gradient(135deg, #8c3bff, #d22766);
  color: #fff;
  font-weight: 700;
  cursor: pointer;
  text-transform: uppercase;
  font-size: 13px;
  letter-spacing: 1px;
  transition: transform 0.2s ease, box-shadow 0.2s ease;
}

.team-lobby-card__button:disabled {
  opacity: 0.5;
  cursor: not-allowed;
  box-shadow: none;
  transform: none;
}

.team-lobby-card__button:not(:disabled):hover {
  transform: translateY(-1px);
  box-shadow: 0 10px 24px rgba(210, 39, 102, 0.4);
}

.team-lobby-card__hint {
  font-size: 12px;
  color: #b4b1c7;
}

.team-lobby-card__error {
  font-size: 12px;
  color: #ff8b8b;
}

.team-lobby-card__eyebrow {
  font-size: 11px;
  letter-spacing: 1.5px;
  text-transform: uppercase;
  color: rgba(208, 183, 255, 0.9);
}

.team-lobby-card__title {
  margin: 0;
  font-size: 18px;
  letter-spacing: 1px;
  color: #f5f5ff;
}

.team-lobby-card__body {
  margin: 0;
  font-size: 13px;
  color: rgba(220, 220, 255, 0.9);
  line-height: 1.5;
}

.team-lobby-card__list {
  margin: 0;
  padding-left: 18px;
  display: flex;
  flex-direction: column;
  gap: 6px;
  color: rgba(195, 195, 255, 0.9);
}

.team-lobby-card__list li {
  font-size: 13px;
  line-height: 1.4;
}

.join-card {
  width: 100%;
  background: rgba(4, 4, 6, 0.85);
  border: 1px solid rgba(255, 255, 255, 0.08);
  border-radius: 16px;
  padding: 18px 22px;
  display: flex;
  flex-direction: column;
  gap: 12px;
  min-height: 280px;
}

.join-card__row {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.join-card__label {
  font-size: 11px;
  text-transform: uppercase;
  letter-spacing: 1px;
  color: #8d8d95;
}

.join-card__input {
  background: #0b0b11;
  border: 1px solid rgba(255, 255, 255, 0.08);
  border-radius: 10px;
  padding: 10px 12px;
  color: #fff;
  font-size: 13px;
}

.join-card__button {
  border: none;
  border-radius: 12px;
  padding: 12px;
  background: linear-gradient(135deg, #ff6b80, #ffb347);
  color: #0b0b0f;
  font-weight: 700;
  cursor: pointer;
  transition: transform 0.2s ease;
}

.join-card__button:disabled {
  opacity: 0.6;
  cursor: not-allowed;
  transform: none;
}

.join-card__error {
  color: #ff7b7b;
  font-size: 12px;
  margin: 0;
}
</style>
