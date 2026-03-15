<template>
  <div class="landing">
    <button class="window-corner-btn" @click="router.push('/')">Back to New Case</button>
    <div class="landing-board">
      <div class="top-bar evidence-strip">
        <span class="classified-badge">● Case Archive</span>
        <div class="top-right">
          <span class="case-file">CASE FILE #NC-2025-0303</span>
          <span>Signed in as <strong>{{ auth.user?.username }}</strong></span>
          <button class="top-right__logout" @click="logout">Logout</button>
        </div>
      </div>

      <div class="hero-block">
        <div class="stamp-frame">
          <h1 class="claido-heading" data-text="CLAIDO">CLAIDO</h1>
        </div>
        <div class="case-header">
          <span class="case-header__label">NovaCorp Investigation Logs</span>
          <span class="case-header__divider">—</span>
          <span class="case-header__tag">Investigator Access Only</span>
        </div>
      </div>

      <div class="folder-wrapper">
        <div class="folder-tab">
          <span class="folder-tab__title">CASE ARCHIVE</span>
          <span class="folder-tab__state">OPEN</span>
        </div>
        <div class="briefing-card is-revealed">
          <div class="briefing-card__content">
            <div class="watermark">ARCHIVE</div>
            <div class="card-inner">
              <div class="card-header">
                <div class="card-title-block">
                  <span class="card-stamp-label">INVESTIGATION LOGS</span>
                  <span class="card-date">2025-03-03</span>
                </div>
              </div>
              <p class="card-summary archive-summary">
                Review completed cases from your account.
                Click a case to review the questions and solutions.
              </p>
              <div class="archive-list-wrap">
                <div class="leaderboard-card__header">
                  <span class="leaderboard-card__eyebrow">Recent Investigations</span>
                  <h3 class="leaderboard-card__title">Case Archive</h3>
                </div>
                <div v-if="loading" class="leaderboard-card__empty">Loading history...</div>
                <div v-else-if="history.length === 0" class="leaderboard-card__empty">No completed cases yet.</div>
                <div v-else class="leaderboard-list">
                  <button
                    v-for="(entry, index) in history"
                    :key="entry.sessionId + entry.completedAtUtc"
                    class="leaderboard-row archive-row"
                    type="button"
                    @click="goToCase(entry.sessionId)"
                  >
                    <span class="leaderboard-rank">#{{ index + 1 }}</span>
                    <span class="leaderboard-name archive-name">
                      <strong>CASE {{ String(entry.sessionId).slice(0, 8).toUpperCase() }}</strong>
                      <small>{{ formatDate(entry.completedAtUtc) }} · {{ entry.teamMode }}</small>
                    </span>
                    <span class="leaderboard-time archive-time">{{ formatDuration(entry.elapsedSeconds) }}</span>
                    <span class="archive-points">{{ entry.points }} pts</span>
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>
      </div>

      <div v-if="error" class="error-msg">{{ error }}</div>

      <p class="disclaimer">Session expires when tab closes. Each case is AI-generated.</p>
      <img :src="cautionTapeImg" alt="" class="landing-caution-tape" aria-hidden="true" />
    </div>
  </div>
</template>

<script setup>
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import cautionTapeImg from '../../images/caution.webp'
import { useAuthStore } from '../stores/authStore.js'
import { useGameStore } from '../stores/gameStore.js'

const router = useRouter()
const auth = useAuthStore()
const game = useGameStore()

const loading = ref(false)
const error = ref('')
const history = ref([])

async function loadHistory() {
  loading.value = true
  error.value = ''
  try {
    history.value = await auth.fetchHistory()
  } catch (e) {
    error.value = e.message || 'Failed to load history.'
  } finally {
    loading.value = false
  }
}

async function logout() {
  await auth.logout()
  game.resetState()
  router.push('/login')
}

function formatDuration(totalSeconds) {
  const safe = Math.max(0, Number(totalSeconds) || 0)
  const minutes = Math.floor(safe / 60)
  const seconds = safe % 60
  if (minutes >= 60) {
    const hours = Math.floor(minutes / 60)
    const remMinutes = minutes % 60
    return `${hours}:${String(remMinutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`
  }
  return `${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`
}

function formatDate(value) {
  if (!value) return '-'
  return new Date(value).toLocaleString('en-AU')
}

function goToCase(sessionId) {
  if (!sessionId) return
  router.push(`/history/${sessionId}`)
}

onMounted(() => {
  loadHistory().catch(() => {})
})
</script>

<style scoped>
.landing {
  min-height: 100vh;
  display: flex;
  justify-content: center;
  padding: 40px 24px 60px;
  overflow-y: auto;
  position: relative;
  flex-direction: column;
  align-items: center;
  gap: 24px;
  background:
    repeating-linear-gradient(
      0deg,
      rgba(255, 255, 255, 0.02) 0px,
      rgba(255, 255, 255, 0.02) 1px,
      transparent 1px,
      transparent 8px
    ),
    repeating-linear-gradient(
      90deg,
      rgba(255, 255, 255, 0.01) 0px,
      rgba(255, 255, 255, 0.01) 1px,
      transparent 1px,
      transparent 8px
    ),
    linear-gradient(135deg, #392317 0%, #2b1a12 35%, #1f130d 70%, #140b08 100%);
}

.window-corner-btn {
  position: fixed;
  top: 16px;
  left: 18px;
  z-index: 20;
  border: 1px solid rgba(255, 240, 220, 0.4);
  border-radius: 999px;
  background: rgba(0, 0, 0, 0.25);
  color: rgba(255, 240, 220, 0.95);
  font-family: var(--font-mono);
  font-size: 12px;
  text-transform: uppercase;
  letter-spacing: 1px;
  min-height: 42px;
  padding: 0 18px;
}

.landing-board {
  width: 100%;
  max-width: 800px;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 28px;
  position: relative;
}

.landing-caution-tape {
  position: absolute;
  right: -18px;
  bottom: 36px;
  width: 190px;
  opacity: 0.72;
  transform: rotate(-13deg);
  pointer-events: none;
  z-index: 2;
  filter: drop-shadow(0 8px 16px rgba(0, 0, 0, 0.42));
}

.evidence-strip {
  position: relative;
  z-index: 1;
  width: 100%;
  max-width: 660px;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 28px;
}

.top-bar {
  width: 100%;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.classified-badge,
.case-file,
.card-stamp-label,
.card-date,
.disclaimer {
  font-family: var(--font-mono);
}

.classified-badge {
  color: var(--accent-red);
  font-size: 13px;
  font-weight: 700;
  letter-spacing: 2px;
  text-transform: uppercase;
}

.top-right {
  display: flex;
  align-items: center;
  gap: 12px;
  color: rgba(255, 240, 220, 0.9);
  font-family: var(--font-mono);
  font-size: 12px;
}

.case-file {
  color: rgba(255, 240, 220, 0.5);
  font-size: 12px;
  letter-spacing: 1.2px;
}

.top-right__logout {
  border: 1px solid rgba(255, 240, 220, 0.35);
  background: rgba(0, 0, 0, 0.25);
  color: rgba(255, 240, 220, 0.95);
  border-radius: 6px;
  padding: 6px 10px;
  cursor: pointer;
}

.hero-block {
  text-align: center;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 12px;
}

.stamp-frame {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  padding: 12px 32px;
  border: 4px solid rgba(183, 11, 11, 0.95);
  border-radius: 6px;
  transform: rotate(-3deg);
  position: relative;
  cursor: default;
  transition: border-color 0.15s, box-shadow 0.15s;
}

.stamp-frame::before {
  content: '';
  position: absolute;
  inset: 3px;
  border: 1.5px solid rgba(183, 11, 11, 0.3);
  border-radius: 3px;
  pointer-events: none;
}

.stamp-frame:hover {
  border-color: rgba(183, 11, 11, 0.95);
  box-shadow:
    0 0 12px rgba(160, 26, 26, 0.5),
    0 0 30px rgba(160, 26, 26, 0.25);
}

.stamp-frame:hover::before {
  border-color: rgba(160, 26, 26, 0.6);
}

.claido-heading {
  margin: 0;
  font-size: clamp(48px, 10vw, 90px);
  letter-spacing: 14px;
  text-transform: uppercase;
  font-family: var(--font-display);
  color: rgba(183, 11, 11, 0.95);
  transition: color 0.1s, text-shadow 0.1s;
}

.stamp-frame:hover .claido-heading {
  color: #a01a1a;
  text-shadow:
    0 0 8px rgba(160, 26, 26, 0.9),
    0 0 20px rgba(160, 26, 26, 0.7),
    0 0 40px rgba(160, 26, 26, 0.5);
  animation: red-flicker 0.15s steps(2) infinite;
}

@keyframes red-flicker {
  0% { opacity: 1; }
  50% { opacity: 0.85; }
  100% { opacity: 1; }
}

.case-header {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-wrap: wrap;
  justify-content: center;
}

.case-header__label {
  font-family: var(--font-mono);
  font-size: clamp(13px, 2vw, 16px);
  font-weight: 700;
  letter-spacing: 2.5px;
  text-transform: uppercase;
  color: rgba(255, 220, 180, 0.7);
}

.case-header__divider {
  color: rgba(255, 220, 180, 0.3);
  font-size: 16px;
}

.case-header__tag {
  font-family: var(--font-mono);
  font-size: clamp(11px, 1.5vw, 13px);
  letter-spacing: 2px;
  text-transform: uppercase;
  color: rgba(220, 180, 140, 0.85);
  border: 1px solid rgba(220, 180, 140, 0.4);
  padding: 3px 10px;
  border-radius: 2px;
}

.folder-wrapper {
  width: 100%;
  position: relative;
}

.folder-tab {
  position: relative;
  margin-left: 24px;
  min-height: 38px;
  display: inline-flex;
  align-items: center;
  justify-content: space-between;
  gap: 18px;
  padding: 0 20px;
  border-radius: 10px 10px 0 0;
  background: linear-gradient(180deg, #d7b98d 0%, #c7a87c 100%);
  border: 1px solid #b08d5f;
  border-bottom: none;
  box-shadow: 0 -1px 0 rgba(255, 255, 255, 0.35) inset;
  text-transform: uppercase;
  letter-spacing: 1.4px;
  color: #624220;
  font-family: var(--font-mono);
  font-size: 11px;
  font-weight: 700;
}

.folder-tab__title {
  font-size: 11px;
}

.folder-tab__state {
  font-size: 10px;
  letter-spacing: 1.8px;
  color: rgba(98, 66, 32, 0.72);
}

.briefing-card {
  width: 100%;
  position: relative;
  border: 1px solid #b08d5f;
  border-radius: 0 8px 8px 8px;
  background:
    repeating-linear-gradient(
      180deg,
      rgba(255, 255, 255, 0.045) 0px,
      rgba(255, 255, 255, 0.045) 1px,
      transparent 1px,
      transparent 28px
    ),
    linear-gradient(180deg, #d4b58a 0%, #cba87a 100%);
  box-shadow:
    0 12px 28px rgba(0, 0, 0, 0.22),
    inset 0 1px 0 rgba(255, 255, 255, 0.25);
}

.briefing-card__content {
  position: relative;
}

.briefing-card::before,
.leaderboard-card::before {
  content: '';
  position: absolute;
  inset: 0;
  pointer-events: none;
  background:
    linear-gradient(to bottom, rgba(255, 255, 255, 0.2), transparent 22%),
    linear-gradient(to top, rgba(0, 0, 0, 0.08), transparent 30%);
}

.briefing-card::after,
.leaderboard-card::after {
  content: '';
  position: absolute;
  inset: 0;
  pointer-events: none;
  background-image: linear-gradient(transparent 0 97%, rgba(0, 0, 0, 0.04) 100%);
  background-size: 100% 30px;
  opacity: 0.45;
}

.watermark {
  position: absolute;
  top: 50%;
  left: 52%;
  transform: translate(-50%, -50%) rotate(-18deg);
  font-size: clamp(44px, 8vw, 88px);
  letter-spacing: 7px;
  font-weight: 900;
  color: rgba(100, 58, 31, 0.14);
  pointer-events: none;
  user-select: none;
}

.card-inner {
  position: relative;
  z-index: 1;
  padding: 30px 32px 36px;
  color: #3d2717;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 14px;
  margin-bottom: 16px;
}

.card-title-block {
  display: grid;
  gap: 6px;
}

.card-stamp-label {
  font-size: 11px;
  letter-spacing: 2px;
  color: #7f4e2c;
  text-transform: uppercase;
}

.card-date {
  font-size: 11px;
  letter-spacing: 1.6px;
  color: rgba(79, 50, 28, 0.74);
}

.card-summary {
  margin: 0;
  line-height: 1.6;
  font-size: 15px;
  max-width: 60ch;
}

.archive-summary {
  margin-bottom: 0;
}

.archive-list-wrap {
  margin-top: 18px;
}

.error-msg {
  width: 100%;
  max-width: 660px;
  color: rgba(255, 194, 180, 0.95);
  background: rgba(94, 23, 20, 0.55);
  border: 1px solid rgba(198, 84, 84, 0.55);
  border-radius: 8px;
  padding: 10px 12px;
  font-family: var(--font-mono);
  font-size: 12px;
  letter-spacing: 0.5px;
}

.disclaimer {
  font-size: 11px;
  letter-spacing: 1.6px;
  color: rgba(255, 230, 190, 0.55);
  text-transform: uppercase;
}

.leaderboard-card {
  width: 100%;
  max-width: 660px;
  position: relative;
  border: 1px solid #b08d5f;
  border-radius: 10px;
  background:
    repeating-linear-gradient(
      180deg,
      rgba(255, 255, 255, 0.04) 0px,
      rgba(255, 255, 255, 0.04) 1px,
      transparent 1px,
      transparent 28px
    ),
    linear-gradient(180deg, #cfb084 0%, #c29f70 100%);
  box-shadow:
    0 14px 30px rgba(0, 0, 0, 0.24),
    inset 0 1px 0 rgba(255, 255, 255, 0.25);
  padding: 18px 20px;
  z-index: 1;
  overflow: hidden;
}

.leaderboard-card__header {
  display: grid;
  gap: 4px;
  margin-bottom: 12px;
}

.leaderboard-card__eyebrow {
  font-family: var(--font-mono);
  font-size: 10px;
  letter-spacing: 2px;
  text-transform: uppercase;
  color: #7c4a28;
}

.leaderboard-card__title {
  margin: 0;
  font-family: var(--font-display);
  font-size: 26px;
  letter-spacing: 1px;
  color: #4c2f1c;
}

.leaderboard-list {
  display: grid;
  gap: 8px;
}

.leaderboard-row {
  width: 100%;
  border: 1px solid rgba(108, 72, 39, 0.2);
  display: grid;
  grid-template-columns: auto minmax(0, 1fr) auto auto;
  align-items: center;
  gap: 12px;
  background: rgba(255, 246, 230, 0.52);
  border-radius: 8px;
  padding: 10px 12px;
}

.archive-row {
  cursor: pointer;
  text-align: left;
  font: inherit;
  color: inherit;
  transition: transform 0.12s ease, border-color 0.12s ease, background 0.12s ease;
}

.archive-row:hover {
  transform: translateY(-1px);
  border-color: rgba(108, 72, 39, 0.45);
  background: rgba(255, 246, 230, 0.7);
}

.leaderboard-rank,
.leaderboard-time,
.leaderboard-name {
  font-family: var(--font-mono);
}

.leaderboard-rank {
  font-size: 11px;
  color: #7b4c2a;
}

.leaderboard-name {
  color: #4d2f1a;
}

.leaderboard-time {
  font-size: 12px;
  letter-spacing: 1px;
  color: #6a3e20;
}

.archive-name {
  display: grid;
  gap: 3px;
}

.archive-name small {
  color: rgba(77, 47, 26, 0.78);
  font-size: 11px;
}

.archive-points {
  font-family: var(--font-mono);
  font-size: 12px;
  color: #8c3f16;
  font-weight: 700;
}

.leaderboard-card__empty {
  font-family: var(--font-mono);
  font-size: 12px;
  color: rgba(77, 47, 26, 0.78);
}

@media (max-width: 860px) {
  .landing {
    padding-top: 64px;
  }

  .window-corner-btn {
    top: 10px;
    left: 10px;
    min-height: 38px;
    padding: 0 14px;
    font-size: 11px;
  }

  .top-bar {
    flex-direction: column;
    align-items: flex-start;
    gap: 8px;
  }

  .top-right {
    flex-wrap: wrap;
  }

  .folder-tab {
    margin-left: 12px;
  }
}

@media (max-width: 640px) {
  .card-inner {
    padding: 24px 20px 26px;
  }

  .leaderboard-row {
    grid-template-columns: auto 1fr;
    gap: 8px 12px;
  }

  .archive-time,
  .archive-points {
    grid-column: 2;
  }
}
</style>
