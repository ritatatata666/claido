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

      <section class="hero-note evidence-card evidence-card--wide">
        <span class="note-pin note-pin--blue"></span>
        <div class="hero-copy">
          <p class="hero-kicker">Internal Investigation Board</p>
          <h1 class="claido-heading">CLAIDO</h1>
          <p class="hero-subtitle">NovaCorp breach analysis, witness interviews, and evidence correlation.</p>
        </div>
        <div class="hero-tag">Investigator Access</div>
      </section>

      <section class="briefing-grid">
        <article class="briefing-card evidence-card">
          <span class="note-pin note-pin--red"></span>
          <div class="watermark">CONFIDENTIAL</div>
          <div class="card-inner">
            <div class="card-header">
              <div class="card-title-block">
                <span class="card-stamp-label">Incident briefing</span>
                <span class="card-date">2025-03-03</span>
              </div>
            </div>
            <p class="card-summary">
              A corporate breach occurred overnight at NovaCorp headquarters. Sensitive vault data was compromised.
              The suspect is still at large. Traverse seven internal systems, cross-reference the evidence, and lock in the four-word passphrase.
            </p>
            <ul class="room-list">
              <li v-for="room in rooms" :key="room.id">
                <span class="bullet">✦</span>
                <strong>{{ room.label }}</strong>
                <span>{{ room.desc }}</span>
              </li>
            </ul>
            <div class="declassified-stamp">Prepared for field use</div>
          </div>
        </article>

        <aside class="side-column">
          <section class="snapshot-card evidence-card">
            <span class="note-pin note-pin--gold"></span>
            <p class="snapshot-label">Primary leads</p>
            <ul class="snapshot-list">
              <li>Hidden vault words are distributed across rooms.</li>
              <li>NPC interviews may reveal motive and access paths.</li>
              <li>Each session generates a fresh case state from the backend.</li>
            </ul>
          </section>

          <section class="start-card evidence-card">
            <span class="note-pin note-pin--red"></span>
            <p class="start-label">Open a new evidence board</p>
            <button
              class="start-btn"
              :disabled="loading"
              @click="startGame"
            >
              <span v-if="loading">
                <span class="spinner-dot"></span>
                Generating case file...
              </span>
              <span v-else>Begin Investigation</span>
            </button>
            <p class="disclaimer">Frontend visuals updated only; backend session flow stays unchanged.</p>
          </section>
        </aside>
      </section>

      <div v-if="error" class="error-msg evidence-card">{{ error }}</div>
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

const rooms = [
  { id: 'shell', label: 'NovaShell', desc: 'Explore the internal filesystem' },
  { id: 'database', label: 'NovaCrime DB', desc: 'Query employee and access records' },
  { id: 'mail', label: 'NovaMail', desc: 'Read intercepted corporate emails' },
  { id: 'wiki', label: 'NovaWiki', desc: 'Browse classified internal documents' },
  { id: 'search', label: 'NovaSearch', desc: 'Analyse 50,000 system log entries' },
  { id: 'onion', label: 'The Onion', desc: 'Browse the dark web for leads' },
  { id: 'vault', label: 'Vault', desc: 'Enter the four-word passphrase to win' },
]

async function startGame() {
  loading.value = true
  error.value = ''
  try {
    await store.createSession()
    router.push('/hub')
  } catch (e) {
    error.value = e.message || 'Failed to connect to backend. Is it running?'
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.landing {
  min-height: 100vh;
  display: flex;
  justify-content: center;
  padding: 34px 24px 60px;
  overflow-y: auto;
}

.landing-board {
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
  background: linear-gradient(180deg, rgba(255, 250, 241, 0.98), rgba(238, 225, 204, 0.96));
  border: 1px solid rgba(89, 65, 42, 0.2);
  box-shadow: var(--paper-shadow);
}

.evidence-strip {
  padding: 12px 18px;
  border-radius: 10px;
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
</style>
