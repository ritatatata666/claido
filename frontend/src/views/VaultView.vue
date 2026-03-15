<template>
  <RoomLayout>
    <div :class="['vault-view', { 'vault-view--solved': solved }]">
      <!-- Solved screen -->
      <div v-if="solved" class="solved-screen">
        <div class="solved-topline">
          <div class="vault-hero vault-hero--solved" aria-hidden="true">
            <div class="vault-graphic">
              <div class="vault-dial">🔐</div>
            </div>
            <h1 class="vault-title">
              <span class="vault-title__brand">NOVACORP</span>
              <span class="vault-title__rest"> VAULT</span>
            </h1>
          </div>
          <div class="solved-stamp">SOLVED</div>
        </div>

        <div class="case-file">
          <div class="case-header">
            <span class="case-label">CASE FILE — PROJECT NOVA INCIDENT</span>
            <span class="case-date">2025-03-03</span>
          </div>
          <div class="case-section">
            <div class="case-field">
              <span class="field-label">CULPRIT</span>
              <span class="field-value culprit-name">{{ store.sessionState?.culprit?.name }}</span>
            </div>
            <div class="case-field">
              <span class="field-label">DEPARTMENT</span>
              <span class="field-value">{{ store.sessionState?.culprit?.department }}</span>
            </div>
            <div class="case-field">
              <span class="field-label">ROLE</span>
              <span class="field-value">{{ store.sessionState?.culprit?.role }}</span>
            </div>
            <div class="case-field">
              <span class="field-label">MOTIVE</span>
              <span class="field-value motive-text">{{ store.sessionState?.motive }}</span>
            </div>
            <div class="case-field">
              <span class="field-label">INCIDENT TIME</span>
              <span class="field-value">{{ store.sessionState?.incidentTimestamp }}</span>
            </div>
            <div class="case-field">
              <span class="field-label">VAULT CODE</span>
              <span class="field-value vault-code">{{ store.sessionState?.vaultCode }}</span>
            </div>
          </div>
          <div class="case-footer">
            <span>Case solved by {{ store.currentPlayerName }} in {{ formattedTime }}</span>
          </div>
        </div>
        <div class="leaderboard-panel">
          <div class="leaderboard-panel__header">
            <span class="leaderboard-panel__eyebrow">Fastest Times</span>
            <h2 class="leaderboard-panel__title">Top 5 Leaderboard</h2>
          </div>
          <div v-if="store.leaderboard.length === 0" class="leaderboard-panel__empty">
            No leaderboard entries recorded yet.
          </div>
          <div v-else class="leaderboard-panel__list">
            <div
              v-for="(entry, index) in store.leaderboard"
              :key="`${entry.displayName}-${entry.solveSeconds}-${index}`"
              :class="['leaderboard-panel__row', { 'leaderboard-panel__row--current': isCurrentEntry(entry, index) }]"
            >
              <span class="leaderboard-panel__rank">#{{ index + 1 }}</span>
              <span class="leaderboard-panel__name">{{ entry.displayName }}</span>
              <span class="leaderboard-panel__time">{{ formatClock(entry.solveSeconds) }}</span>
            </div>
          </div>
        </div>
        <button class="btn-primary play-again" @click="$router.push('/')">
          Begin New Investigation
        </button>

        <div v-if="winModalVisible" class="vault-win-backdrop" @click.self="dismissWinModal">
          <div class="vault-win-modal" role="dialog" aria-modal="true" aria-label="Case solved">
            <div class="vault-win-modal__eyebrow">CASE STATUS: CLOSED</div>
            <h3 class="vault-win-modal__title">Congratulations, Detective {{ store.currentPlayerName }}</h3>
            <p class="vault-win-modal__copy">You cracked the NovaCorp vault, exposed the culprit, and secured the stolen intelligence before it disappeared.</p>
            <p class="vault-win-modal__copy">Final investigation time: <strong>{{ formattedTime }}</strong></p>
            <p class="vault-win-modal__copy">NovaCorp systems stabilized. Civilian data has been saved.</p>
            <button class="vault-win-modal__btn" type="button" @click="dismissWinModal">Review Case File</button>
          </div>
        </div>
      </div>

      <!-- Vault entry -->
      <div v-else class="vault-entry">
        <div class="vault-file-shell">
          <div class="vault-file-tab">
            <span class="vault-file-tab__title">VAULT FILE</span>
            <span class="vault-file-tab__state">LIVE ACCESS</span>
          </div>

          <div class="vault-file">
            <div class="vault-access-bar">
              <span class="vault-access-bar__primary">NOVACORP VAULT</span>
              <span class="vault-access-bar__divider">-</span>
              <span class="vault-access-bar__secondary">AUTHORIZED ACCESS ONLY</span>
            </div>

            <div class="vault-hero vault-hero--active">
              <div class="vault-graphic">
                <div :class="['vault-dial', { spinning: attempting }]">🔐</div>
              </div>

              <h1 class="vault-title">
                <span class="vault-title__brand">NOVACORP</span>
                <span class="vault-title__rest"> VAULT</span>
              </h1>
            </div>

            <p class="vault-subtitle">
              Enter the four-word passphrase to unlock the vault and reveal the culprit.
            </p>

            <div class="clue-reminder">
              <div class="reminder-title">Collected clues:</div>
              <div v-if="store.discoveredClues.length === 0" class="reminder-empty">
                No clues yet. Return to previous rooms.
              </div>
              <div v-for="clue in store.discoveredClues" :key="clue.id" class="reminder-clue">
                <span class="reminder-room">{{ clue.room }}</span>
                {{ clue.text }}
              </div>
            </div>

            <div class="passphrase-input-wrap">
              <input
                v-model="passphrase"
                class="passphrase-input"
                placeholder="word1 word2 word3 word4"
                @keydown.enter="submit"
                :disabled="attempting"
              />
              <button
                class="btn-primary vault-btn"
                :disabled="!passphrase.trim() || attempting"
                @click="submit"
              >
                {{ attempting ? 'Verifying...' : 'Unlock Vault' }}
              </button>
            </div>

            <div v-if="feedback" :class="['vault-feedback', feedbackType === 'error' ? 'feedback-error' : 'feedback-success']">
              {{ feedback }}
            </div>

            <div class="word-hints">
              <div class="hint-item"><span class="hint-num">1</span><span>Theme: Time</span><span class="hint-room">NovaShell</span></div>
              <div class="hint-item"><span class="hint-num">2</span><span>Theme: Location</span><span class="hint-room">NovaMail</span></div>
              <div class="hint-item"><span class="hint-num">3</span><span>Theme: Identity</span><span class="hint-room">NovaWiki</span></div>
              <div class="hint-item"><span class="hint-num">4</span><span>Theme: Motive</span><span class="hint-room">NovaSearch</span></div>
            </div>
          </div>
        </div>
      </div>
    </div>
    <NpcChat npc-id="ceo" npc-name="Victoria Stone" npc-role="CEO, NovaCorp" />
  </RoomLayout>
</template>

<script setup>
import { ref, computed } from 'vue'
import RoomLayout from '../components/RoomLayout.vue'
import NpcChat from '../components/NpcChat.vue'
import { useGameStore } from '../stores/gameStore.js'

const store = useGameStore()
const passphrase = ref('')
const attempting = ref(false)
const solved = ref(false)
const feedback = ref('')
const feedbackType = ref('error')
const solveTime = ref(0)
const wrongAttempts = ref(0)
const winModalVisible = ref(false)

const formattedTime = computed(() => {
  return formatClock(solveTime.value, true)
})

const currentRank = computed(() => {
  const myName = store.currentPlayerName
  const mySeconds = solveTime.value
  if (!myName || !mySeconds) return -1
  return store.leaderboard.findIndex(entry =>
    entry.displayName === myName && Number(entry.solveSeconds) === Number(mySeconds)
  )
})

function formatClock(totalSeconds, verbose = false) {
  const safe = Math.max(0, Number(totalSeconds) || 0)
  const hours = Math.floor(safe / 3600)
  const minutes = Math.floor((safe % 3600) / 60)
  const seconds = safe % 60

  if (verbose) {
    if (hours > 0) return `${hours}h ${minutes}m ${seconds}s`
    return `${minutes}m ${seconds}s`
  }

  if (hours > 0) {
    return `${hours}:${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`
  }
  return `${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`
}

function isCurrentEntry(entry, index) {
  return currentRank.value === index
    && entry.displayName === store.currentPlayerName
    && Number(entry.solveSeconds) === Number(solveTime.value)
}

function dismissWinModal() {
  winModalVisible.value = false
}

async function submit() {
  const answer = passphrase.value.trim()
  if (!answer) return
  attempting.value = true
  feedback.value = ''

  try {
    const elapsed = Math.floor((Date.now() - store.gameStartTime) / 1000)
    const points = Math.max(0, 5000 - elapsed * 4)
    const res = await store.validateAnswer('vault', answer, {
      elapsedSeconds: elapsed,
      points,
      wrongAnswers: wrongAttempts.value,
      timePenaltySeconds: 0,
    })
    if (res.correct) {
      solveTime.value = elapsed
      store.markRoomComplete('vault')
      solved.value = true
      winModalVisible.value = true
    } else {
      wrongAttempts.value += 1
      feedbackType.value = 'error'
      feedback.value = res.hint || 'Incorrect passphrase. Keep investigating.'
    }
  } catch (e) {
    feedbackType.value = 'error'
    feedback.value = 'Connection error. Try again.'
  } finally {
    attempting.value = false
  }
}
</script>

<style scoped>
.vault-view {
  display: flex;
  align-items: center;
  justify-content: center;
  height: 100%;
  padding: 32px;
  overflow-y: auto;
  position: relative;
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
  font-family: 'Courier New', Courier, monospace;
}

.vault-view--solved {
  background:
    radial-gradient(circle at top left, rgba(255, 245, 228, 0.55), transparent 24%),
    radial-gradient(circle at bottom right, rgba(214, 188, 150, 0.32), transparent 26%),
    linear-gradient(180deg, #e2cba8 0%, #d5bb95 52%, #caa980 100%);
}

/* Vault does not need the RoomLayout evidence sidebar */
:deep(.clue-sidebar) {
  display: none;
}

:deep(.room-main) {
  width: 100%;
}

/* Remove RoomLayout framed board in Vault so the scene matches Landing style. */
:deep(.room-body) {
  padding: 18px 0 0;
  justify-content: center;
}

:deep(.room-main) {
  background: transparent;
  border-radius: 0;
  padding: 0;
  box-shadow: none;
  overflow: visible;
}

.vault-view::before {
  content: '';
  position: absolute;
  inset: 0;
  background: repeating-linear-gradient(
    0deg,
    transparent,
    transparent 2px,
    rgba(255, 225, 180, 0.025) 2px,
    rgba(255, 225, 180, 0.025) 4px
  );
  pointer-events: none;
  z-index: 999;
}

.vault-view--solved::before {
  background:
    repeating-linear-gradient(
      180deg,
      transparent 0 27px,
      rgba(160, 130, 95, 0.08) 27px 28px
    ),
    repeating-linear-gradient(
      90deg,
      rgba(255, 255, 255, 0.05) 0px,
      rgba(255, 255, 255, 0.05) 1px,
      transparent 1px,
      transparent 12px
    );
}

/* Vault entry */
.vault-entry {
  width: 100%;
  max-width: 820px;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 22px;
}

.vault-file-shell {
  width: 100%;
  position: relative;
}

.vault-file-tab {
  position: relative;
  display: inline-flex;
  align-items: center;
  gap: 12px;
  margin-left: 24px;
  padding: 6px 18px 4px;
  background: #c8a97a;
  border: 1px solid #6a4b33;
  border-bottom: none;
  border-radius: 6px 6px 0 0;
  color: #5a3d24;
  font-family: var(--font-mono);
  font-size: 11px;
  font-weight: 700;
  letter-spacing: 1.6px;
  text-transform: uppercase;
}

.vault-file-tab__state {
  font-size: 9px;
  letter-spacing: 1.2px;
  color: rgba(49, 26, 12, 0.9);
  border-left: 1px solid rgba(90, 61, 36, 0.35);
  padding-left: 8px;
}

.vault-file {
  width: 100%;
  margin-top: -1px;
  padding: 22px 24px 24px;
  border: 1px solid rgba(159, 124, 90, 0.46);
  border-radius: 0 8px 8px 8px;
  background:
    repeating-linear-gradient(
      180deg,
      rgba(158, 124, 92, 0.1) 0,
      rgba(158, 124, 92, 0.1) 1px,
      transparent 1px,
      transparent 28px
    ),
    linear-gradient(180deg, rgba(214, 188, 151, 0.94), rgba(178, 149, 112, 0.9));
  box-shadow: 0 16px 34px rgba(0, 0, 0, 0.48), inset 0 0 0 1px rgba(240, 220, 190, 0.22);
}

.vault-access-bar {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 10px;
  margin-bottom: 18px;
  color: rgba(76, 41, 30, 0.88);
  letter-spacing: 2px;
  text-transform: uppercase;
  font-size: 13px;
  font-weight: 800;
}

.vault-access-bar__secondary {
  border: 1px solid rgba(95, 58, 43, 0.35);
  padding: 3px 8px;
  font-size: 11px;
}

.vault-access-bar__divider {
  opacity: 0.75;
}

.vault-hero {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 12px;
  margin-bottom: 18px;
}

.vault-graphic {
  font-size: 64px;
  line-height: 1;
  filter: drop-shadow(0 0 18px rgba(0, 255, 65, 0.5));
  transition: filter 0.18s ease;
}

.vault-dial {
  display: block;
  transition: transform 1s ease;
}

.vault-dial.spinning {
  animation: spin-vault 0.8s linear infinite;
}

@keyframes spin-vault {
  to { transform: rotate(360deg); }
}

.vault-title {
  font-size: 28px;
  font-weight: 900;
  letter-spacing: 8px;
  font-family: 'Courier New', Courier, monospace;
  text-align: center;
  color: #00ff41;
  text-shadow: 0 0 20px rgba(0, 255, 65, 0.62), 0 0 42px rgba(0, 255, 65, 0.3);
}

.vault-title__brand,
.vault-title__rest {
  color: #00ff41;
  text-shadow: 0 0 20px rgba(0, 255, 65, 0.62), 0 0 42px rgba(0, 255, 65, 0.3);
  transition: color 0.18s ease, text-shadow 0.18s ease;
}

.vault-hero--active .vault-graphic {
  filter: drop-shadow(0 0 18px rgba(188, 24, 24, 0.78)) drop-shadow(0 0 42px rgba(188, 24, 24, 0.58));
  animation: vault-menace-lock 4.8s ease-in-out infinite;
}

.vault-hero--active .vault-title__brand,
.vault-hero--active .vault-title__rest {
  color: #bc1818;
  text-shadow:
    0 0 14px rgba(188, 24, 24, 0.86),
    0 0 34px rgba(188, 24, 24, 0.64),
    0 0 58px rgba(188, 24, 24, 0.46);
  animation: vault-menace-text 4.8s ease-in-out infinite;
}

.vault-hero--solved .vault-graphic {
  filter: drop-shadow(0 0 12px rgba(22, 16, 15, 0.72)) drop-shadow(0 0 28px rgba(22, 16, 15, 0.55));
  animation: vault-menace-lock-black 3.1s ease-in-out infinite;
}

.vault-hero--solved .vault-title__brand,
.vault-hero--solved .vault-title__rest {
  color: #271a18;
  text-shadow:
    0 0 8px rgba(30, 22, 20, 0.5),
    0 0 22px rgba(30, 22, 20, 0.35);
  animation: vault-menace-text-black 3.1s ease-in-out infinite;
}

@keyframes vault-menace-text {
  0%, 100% {
    color: #bc1818;
    text-shadow:
      0 0 14px rgba(188, 24, 24, 0.84),
      0 0 34px rgba(188, 24, 24, 0.62),
      0 0 58px rgba(188, 24, 24, 0.44);
  }
  22% {
    color: #d63535;
    text-shadow:
      0 0 18px rgba(214, 53, 53, 0.88),
      0 0 46px rgba(214, 53, 53, 0.66),
      0 0 72px rgba(214, 53, 53, 0.5);
  }
  48% {
    color: #7f1212;
    text-shadow:
      0 0 8px rgba(127, 18, 18, 0.74),
      0 0 20px rgba(127, 18, 18, 0.46),
      0 0 38px rgba(127, 18, 18, 0.34);
  }
  74% {
    color: #cc2c2c;
    text-shadow:
      0 0 20px rgba(204, 44, 44, 0.9),
      0 0 54px rgba(204, 44, 44, 0.7),
      0 0 84px rgba(204, 44, 44, 0.52);
  }
}

@keyframes vault-menace-lock {
  0%, 100% {
    filter: drop-shadow(0 0 18px rgba(188, 24, 24, 0.78)) drop-shadow(0 0 42px rgba(188, 24, 24, 0.58));
  }
  22% {
    filter: drop-shadow(0 0 22px rgba(214, 53, 53, 0.86)) drop-shadow(0 0 56px rgba(214, 53, 53, 0.64));
  }
  48% {
    filter: drop-shadow(0 0 10px rgba(127, 18, 18, 0.66)) drop-shadow(0 0 28px rgba(127, 18, 18, 0.46));
  }
  74% {
    filter: drop-shadow(0 0 24px rgba(204, 44, 44, 0.88)) drop-shadow(0 0 62px rgba(204, 44, 44, 0.66));
  }
}

@keyframes vault-menace-text-black {
  0%, 100% {
    color: #271a18;
    text-shadow:
      0 0 10px rgba(25, 18, 16, 0.52),
      0 0 24px rgba(25, 18, 16, 0.34);
  }
  30% {
    color: #120d0c;
    text-shadow:
      0 0 6px rgba(18, 13, 12, 0.58),
      0 0 14px rgba(18, 13, 12, 0.42);
  }
  68% {
    color: #3c2a27;
    text-shadow:
      0 0 14px rgba(60, 42, 39, 0.55),
      0 0 30px rgba(60, 42, 39, 0.38);
  }
}

@keyframes vault-menace-lock-black {
  0%, 100% {
    filter: drop-shadow(0 0 12px rgba(22, 16, 15, 0.72)) drop-shadow(0 0 28px rgba(22, 16, 15, 0.55));
  }
  30% {
    filter: drop-shadow(0 0 6px rgba(15, 11, 10, 0.8)) drop-shadow(0 0 16px rgba(15, 11, 10, 0.6));
  }
  68% {
    filter: drop-shadow(0 0 16px rgba(43, 31, 28, 0.74)) drop-shadow(0 0 36px rgba(43, 31, 28, 0.56));
  }
}

.vault-subtitle {
  font-size: 14px;
  color: rgba(48, 26, 20, 0.84);
  text-align: center;
  line-height: 1.6;
  max-width: 66ch;
  margin: 0 auto 18px;
}

.clue-reminder {
  width: 100%;
  background: rgba(52, 26, 20, 0.08);
  border: 1px solid rgba(84, 46, 30, 0.26);
  border-radius: var(--radius);
  padding: 16px 18px;
  max-height: 160px;
  margin-bottom: 14px;
  overflow-y: auto;
}

.reminder-title {
  font-size: 11px;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 1px;
  color: rgba(66, 36, 24, 0.88);
  margin-bottom: 10px;
}

.reminder-empty {
  font-size: 13px;
  color: rgba(90, 60, 46, 0.82);
  font-style: italic;
}

.reminder-clue {
  font-size: 12px;
  color: rgba(52, 25, 16, 0.9);
  padding: 6px 0;
  border-bottom: 1px solid rgba(90, 60, 40, 0.18);
  line-height: 1.5;
  display: flex;
  gap: 8px;
  align-items: flex-start;
}

.reminder-clue:last-child { border-bottom: none; }

.reminder-room {
  flex-shrink: 0;
  font-size: 10px;
  font-weight: 700;
  color: #6f1818;
  background: rgba(111, 24, 24, 0.14);
  padding: 1px 6px;
  border-radius: 10px;
}

.passphrase-input-wrap {
  display: flex;
  gap: 12px;
  width: 100%;
  margin-bottom: 14px;
}

.passphrase-input {
  flex: 1;
  font-family: var(--font-mono);
  font-size: 16px;
  letter-spacing: 2px;
  padding: 12px 16px;
  text-align: center;
}

.vault-btn {
  padding: 12px 20px;
  font-size: 14px;
  font-weight: 700;
  white-space: nowrap;
}

.vault-feedback {
  width: 100%;
  padding: 12px 16px;
  border-radius: var(--radius);
  font-size: 13px;
  text-align: center;
  margin-bottom: 14px;
}

.feedback-error {
  background: rgba(248, 81, 73, 0.1);
  border: 1px solid var(--accent-red);
  color: var(--accent-red);
}

.feedback-success {
  background: rgba(63, 185, 80, 0.1);
  border: 1px solid var(--accent-green);
  color: var(--accent-green);
}

.word-hints {
  display: grid;
  grid-template-columns: repeat(4, 1fr);
  gap: 10px;
  width: 100%;
}

.hint-item {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 4px;
  padding: 10px 8px;
  background: rgba(76, 40, 25, 0.08);
  border: 1px solid rgba(86, 47, 30, 0.24);
  border-radius: var(--radius);
  font-size: 11px;
  color: rgba(64, 36, 25, 0.8);
  text-align: center;
}

.hint-num {
  font-size: 16px;
  font-weight: 700;
  color: #00ff41;
  font-family: 'Courier New', Courier, monospace;
  text-shadow: 0 0 6px rgba(0, 255, 65, 0.4);
}

.hint-room {
  font-size: 10px;
  color: #6f1818;
  font-weight: 600;
}

/* Solved screen */
.solved-screen {
  width: 100%;
  max-width: 1120px;
  display: flex;
  flex-direction: column;
  align-items: stretch;
  gap: 24px;
  padding: 4px 10px 24px;
}

.solved-topline {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 10px;
}

.solved-stamp {
  font-family: var(--font-display);
  font-size: clamp(28px, 5.8vw, 52px);
  color: rgba(38, 24, 20, 0.84);
  border: 3px solid rgba(38, 24, 20, 0.84);
  padding: 6px 20px;
  letter-spacing: 3px;
  text-transform: uppercase;
  transform: rotate(-7deg) scale(0.3);
  opacity: 0;
  animation: solved-stamp-in 0.7s cubic-bezier(0.2, 1.1, 0.32, 1) 0.24s forwards;
}

@keyframes solved-stamp-in {
  0% {
    opacity: 0;
    transform: rotate(-11deg) scale(0.28);
  }
  60% {
    opacity: 1;
    transform: rotate(-5deg) scale(1.08);
  }
  100% {
    opacity: 1;
    transform: rotate(-7deg) scale(1);
  }
}

.vault-win-backdrop {
  position: fixed;
  inset: 0;
  z-index: 1200;
  background: rgba(10, 8, 7, 0.68);
  backdrop-filter: blur(2px);
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 20px;
}

.vault-win-modal {
  width: min(680px, 94vw);
  padding: 24px;
  border: 2px solid rgba(90, 60, 40, 0.6);
  background:
    repeating-linear-gradient(
      180deg,
      rgba(125, 95, 65, 0.08) 0,
      rgba(125, 95, 65, 0.08) 1px,
      transparent 1px,
      transparent 30px
    ),
    linear-gradient(180deg, rgba(235, 218, 187, 0.96), rgba(193, 164, 127, 0.94));
  box-shadow: 0 24px 52px rgba(0, 0, 0, 0.56);
}

.vault-win-modal__eyebrow {
  display: inline-block;
  margin-bottom: 10px;
  border: 1px solid rgba(90, 60, 40, 0.45);
  padding: 4px 8px;
  font-size: 11px;
  font-weight: 800;
  letter-spacing: 1.2px;
  color: rgba(78, 44, 32, 0.88);
}

.vault-win-modal__title {
  margin: 0;
  color: #2b1711;
  font-size: clamp(22px, 4vw, 30px);
}

.vault-win-modal__copy {
  margin: 12px 0 0;
  color: rgba(50, 28, 20, 0.88);
  line-height: 1.58;
}

.vault-win-modal__btn {
  margin-top: 16px;
  border: 1px solid rgba(83, 34, 32, 0.72);
  background: linear-gradient(180deg, #7f3230, #591d1d);
  color: #f8e5d0;
  font-family: var(--font-mono);
  letter-spacing: 1px;
  font-weight: 800;
  padding: 10px 14px;
  cursor: pointer;
}

.case-file {
  width: 100%;
  margin-top: 2px;
  padding: 0;
}

.case-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 0 0 14px;
  border-bottom: 2px solid var(--border-color);
}

.case-label {
  font-size: 12px;
  font-weight: 700;
  font-family: var(--font-mono);
  letter-spacing: 1px;
  color: var(--text-secondary);
}

.case-date {
  font-family: var(--font-mono);
  font-size: 12px;
  color: var(--text-secondary);
}

.case-section {
  padding: 20px 0;
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.case-field {
  display: flex;
  align-items: flex-start;
  gap: 16px;
  padding-bottom: 16px;
  border-bottom: 1px solid var(--border-color);
}

.case-field:last-child { border-bottom: none; }

.field-label {
  flex-shrink: 0;
  width: 140px;
  font-size: 11px;
  font-weight: 700;
  font-family: var(--font-mono);
  letter-spacing: 1px;
  color: var(--text-muted);
  text-transform: uppercase;
  padding-top: 2px;
}

.field-value {
  font-size: 14px;
  color: var(--text-primary);
  line-height: 1.5;
}

.culprit-name {
  font-size: 20px;
  font-weight: 700;
  color: var(--accent-red);
}

.motive-text {
  font-style: italic;
  color: var(--text-secondary);
}

.vault-code {
  font-family: var(--font-mono);
  letter-spacing: 2px;
  color: var(--accent-orange);
}

.case-footer {
  padding-top: 14px;
  border-top: 1px solid var(--border-color);
  font-size: 16px;
  font-weight: 800;
  color: var(--accent-red);
  font-family: var(--font-mono);
  text-align: center;
}

.leaderboard-panel {
  width: 100%;
  padding: 16px 0 0;
  display: flex;
  flex-direction: column;
  gap: 16px;
  border-top: 1px solid var(--border-color);
  margin-top: 14px;
}

.leaderboard-panel__header {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.leaderboard-panel__eyebrow {
  font-size: 11px;
  font-family: var(--font-mono);
  font-weight: 700;
  letter-spacing: 2px;
  text-transform: uppercase;
  color: var(--accent-red);
}

.leaderboard-panel__title {
  margin: 0;
  font-size: 22px;
  color: var(--text-secondary);
}

.leaderboard-panel__list {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.leaderboard-panel__row {
  display: grid;
  grid-template-columns: 56px 1fr auto;
  gap: 12px;
  align-items: center;
  padding: 10px 0;
  border-bottom: 1px solid var(--border-color);
}

.leaderboard-panel__row--current {
  border-bottom-color: rgba(185, 70, 54, 0.55);
}

.leaderboard-panel__rank,
.leaderboard-panel__name,
.leaderboard-panel__time,
.leaderboard-panel__empty {
  font-family: var(--font-mono);
}

.leaderboard-panel__rank {
  font-size: 12px;
  color: var(--text-muted);
}

.leaderboard-panel__name {
  font-size: 15px;
  color: var(--text-primary);
}

.leaderboard-panel__time {
  font-size: 15px;
  font-weight: 700;
  color: var(--accent-orange);
}

.leaderboard-panel__empty {
  font-size: 13px;
  color: var(--text-muted);
}

.play-again {
  align-self: center;
  width: auto;
  padding: 10px 18px;
  font-size: 13px;
  font-weight: 700;
  letter-spacing: 1.2px;
}

@media (max-width: 720px) {
  :deep(.room-body) {
    padding: 12px 0 0;
  }

  .vault-entry {
    max-width: 100%;
  }

  .vault-access-bar {
    flex-wrap: wrap;
    gap: 6px;
    font-size: 12px;
    text-align: center;
  }

  .vault-access-bar__secondary {
    font-size: 10px;
  }

  .vault-file-tab {
    margin-left: 12px;
    gap: 10px;
    font-size: 10px;
    letter-spacing: 1.2px;
    padding: 6px 12px 4px;
  }

  .vault-file {
    padding: 16px;
  }

  .passphrase-input-wrap {
    flex-direction: column;
  }

  .leaderboard-panel__row {
    grid-template-columns: 1fr;
  }
}
</style>
