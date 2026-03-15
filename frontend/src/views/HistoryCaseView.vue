<template>
  <div class="landing">
    <button class="window-corner-btn" @click="router.push('/history')">Back to Recent Cases</button>
    <div class="landing-board">
      <div class="top-bar evidence-strip">
        <span class="classified-badge">● Case Archive</span>
        <div class="top-right">
          <span class="case-file">Case detail</span>
          <span>Signed in as <strong>{{ auth.user?.username }}</strong></span>
          <button class="top-right__logout" @click="logout">Logout</button>
        </div>
      </div>

      <div class="hero-block">
        <div class="stamp-frame">
          <h1 class="claido-heading">CASE FILE</h1>
        </div>
        <div v-if="entry" class="case-header">
          <span class="case-header__label">{{ entry.caseFile || shortCaseLabel }}</span>
          <span class="case-header__divider">—</span>
          <span class="case-header__tag">Solved Investigation</span>
        </div>
      </div>

      <div class="folder-wrapper">
        <div class="folder-tab-main">QUESTION REVIEW</div>
        <section class="briefing-card">
          <div class="watermark">ARCHIVE</div>
          <div class="card-inner">
            <p v-if="loading">Loading case file...</p>
            <p v-else-if="error" class="history-error">{{ error }}</p>
            <template v-else-if="entry">
              <div class="meta-grid">
                <div class="meta-item">
                  <span class="meta-label">Points</span>
                  <span class="meta-value">{{ entry.points }}</span>
                </div>
                <div class="meta-item">
                  <span class="meta-label">Elapsed</span>
                  <span class="meta-value">{{ formatDuration(entry.elapsedSeconds) }}</span>
                </div>
                <div class="meta-item">
                  <span class="meta-label">Completed</span>
                  <span class="meta-value">{{ formatDate(entry.completedAtUtc) }}</span>
                </div>
                <div class="meta-item">
                  <span class="meta-label">Culprit</span>
                  <span class="meta-value">{{ entry.culpritName || '-' }}</span>
                </div>
              </div>

              <div v-if="questions.length === 0" class="empty-questions">
                No stored question review found for this case.
              </div>
              <div v-else class="questions-list">
                <article v-for="item in questions" :key="item.questionId || item.prompt" class="question-card">
                  <p class="question-room">{{ item.room }}</p>
                  <p class="question-prompt">{{ item.prompt }}</p>
                  <p class="question-solution">Solution: <strong>{{ item.solution }}</strong></p>
                </article>
              </div>
            </template>
          </div>
        </section>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { useAuthStore } from '../stores/authStore.js'
import { useGameStore } from '../stores/gameStore.js'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const game = useGameStore()
const loading = ref(false)
const error = ref('')
const entry = ref(null)

const shortCaseLabel = computed(() => {
  const id = String(route.params.sessionId || '')
  return `CASE ${id.slice(0, 8).toUpperCase()}`
})

const questions = computed(() => {
  const list = Array.isArray(entry.value?.questions) ? entry.value.questions : []
  return list.filter(q => q && q.prompt)
})

function formatDuration(seconds) {
  const s = Number(seconds) || 0
  const m = Math.floor(s / 60)
  const rem = s % 60
  return `${m}m ${String(rem).padStart(2, '0')}s`
}

function formatDate(value) {
  if (!value) return '-'
  return new Date(value).toLocaleString('en-AU')
}

async function loadCase() {
  const sessionId = route.params.sessionId
  if (!sessionId) {
    error.value = 'Missing session id.'
    return
  }
  loading.value = true
  error.value = ''
  try {
    entry.value = await auth.fetchHistoryCase(sessionId)
  } catch (e) {
    error.value = e.message || 'Failed to load case history.'
  } finally {
    loading.value = false
  }
}

async function logout() {
  await auth.logout()
  game.resetState()
  router.push('/login')
}

onMounted(() => {
  loadCase().catch(() => {})
})
</script>

<style scoped>
.landing {
  min-height: 100vh;
  display: flex;
  justify-content: flex-start;
  padding: 40px 24px 60px;
  overflow-y: auto;
  position: relative;
  flex-direction: column;
  align-items: center;
  gap: 24px;
  background:
    repeating-linear-gradient(
      0deg,
      rgba(255, 255, 255, 0.03) 0px,
      rgba(255, 255, 255, 0.03) 1px,
      transparent 1px,
      transparent 8px
    ),
    repeating-linear-gradient(
      90deg,
      rgba(255, 255, 255, 0.015) 0px,
      rgba(255, 255, 255, 0.015) 1px,
      transparent 1px,
      transparent 8px
    ),
    linear-gradient(135deg, #8B5A3C 0%, #6d4730 30%, #5a3a26 70%, #4a2f1d 100%);
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
.case-file {
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
}

.stamp-frame::before {
  content: '';
  position: absolute;
  inset: 3px;
  border: 1.5px solid rgba(183, 11, 11, 0.3);
  border-radius: 3px;
  pointer-events: none;
}

.claido-heading {
  margin: 0;
  font-size: clamp(42px, 8vw, 72px);
  letter-spacing: 8px;
  text-transform: uppercase;
  font-family: var(--font-display);
  color: rgba(183, 11, 11, 0.95);
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

.folder-tab-main {
  position: relative;
  display: inline-block;
  margin-left: 24px;
  padding: 6px 20px 4px;
  background: #c8a97a;
  border-radius: 6px 6px 0 0;
  font-family: var(--font-mono);
  font-size: 11px;
  font-weight: 700;
  letter-spacing: 2px;
  text-transform: uppercase;
  color: #5a3d24;
  box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.2);
}

.briefing-card {
  width: 100%;
  position: relative;
  background:
    repeating-linear-gradient(
      180deg,
      transparent 0 28px,
      rgba(160, 130, 95, 0.06) 28px 29px
    ),
    linear-gradient(180deg, #d4b896, #c8a97a);
  border: 1px solid #a88b62;
  border-radius: 0 6px 6px 6px;
  box-shadow:
    0 8px 24px rgba(80, 50, 20, 0.25),
    inset 0 1px 0 rgba(255, 255, 255, 0.15),
    inset 0 -1px 0 rgba(0, 0, 0, 0.05);
}

.briefing-card::after {
  content: '';
  position: absolute;
  inset: 0;
  background: repeating-linear-gradient(180deg, transparent 0 24px, rgba(121, 92, 67, 0.08) 24px 25px);
  pointer-events: none;
}

.watermark {
  position: absolute;
  top: 50%;
  left: 52%;
  transform: translate(-50%, -50%) rotate(-24deg);
  font-size: clamp(44px, 9vw, 88px);
  letter-spacing: 6px;
  color: rgba(139, 69, 19, 0.1);
  font-weight: 900;
  pointer-events: none;
}

.card-inner {
  position: relative;
  z-index: 1;
  padding: 32px;
  display: grid;
  gap: 16px;
}

.meta-grid {
  display: grid;
  grid-template-columns: repeat(4, minmax(0, 1fr));
  gap: 10px;
}

.meta-item {
  display: grid;
  gap: 4px;
}

.meta-label {
  font-size: 11px;
  text-transform: uppercase;
  letter-spacing: 1px;
  color: #8a6b4a;
}

.meta-value {
  color: #523925;
  font-family: var(--font-mono);
  font-size: 12px;
}

.questions-list {
  display: grid;
  gap: 10px;
}

.question-card {
  border: 1px solid rgba(105, 75, 44, 0.25);
  border-radius: 8px;
  padding: 12px;
  background: linear-gradient(180deg, rgba(255, 245, 230, 0.95), rgba(248, 234, 211, 0.95));
  display: grid;
  gap: 6px;
}

.question-room {
  margin: 0;
  color: #8a6b4a;
  font-size: 11px;
  text-transform: uppercase;
  letter-spacing: 1.5px;
  font-family: var(--font-mono);
}

.question-prompt {
  margin: 0;
  color: #5e3f26;
  font-weight: 600;
}

.question-solution {
  margin: 0;
  color: #523925;
}

.history-error {
  color: #8f2018;
}

.empty-questions {
  color: #6f5033;
}

@media (max-width: 760px) {
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

  .meta-grid {
    grid-template-columns: 1fr 1fr;
  }
}
</style>
