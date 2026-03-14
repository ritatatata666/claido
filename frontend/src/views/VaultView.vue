<template>
  <RoomLayout>
    <div class="vault-view">
      <!-- Solved screen -->
      <div v-if="solved" class="solved-screen">
        <div class="declassified-stamp">DECLASSIFIED</div>
        <div class="case-file card">
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
            <span>Case solved by investigator in {{ formattedTime }}</span>
          </div>
        </div>
        <button class="btn-primary play-again" @click="$router.push('/')">
          Begin New Investigation
        </button>
      </div>

      <!-- Vault entry -->
      <div v-else class="vault-entry">
        <div class="vault-graphic">
          <div :class="['vault-dial', { spinning: attempting }]">🔐</div>
        </div>

        <h1 class="vault-title">NOVACORP VAULT</h1>
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

const formattedTime = computed(() => {
  const s = solveTime.value
  const m = Math.floor(s / 60)
  const sec = s % 60
  return `${m}m ${sec}s`
})

async function submit() {
  const answer = passphrase.value.trim()
  if (!answer) return
  attempting.value = true
  feedback.value = ''

  try {
    const res = await store.validateAnswer('vault', answer)
    if (res.correct) {
      solveTime.value = Math.floor((Date.now() - store.gameStartTime) / 1000)
      store.markRoomComplete('vault')
      solved.value = true
    } else {
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
}

/* Vault entry */
.vault-entry {
  width: 100%;
  max-width: 560px;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 28px;
}

.vault-graphic {
  font-size: 64px;
  line-height: 1;
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
  font-family: var(--font-mono);
  color: var(--text-primary);
  text-align: center;
}

.vault-subtitle {
  font-size: 14px;
  color: var(--text-secondary);
  text-align: center;
  line-height: 1.6;
  max-width: 400px;
}

.clue-reminder {
  width: 100%;
  background: var(--bg-surface);
  border: 1px solid var(--border-color);
  border-radius: var(--radius);
  padding: 16px;
  max-height: 160px;
  overflow-y: auto;
}

.reminder-title {
  font-size: 11px;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 1px;
  color: var(--text-muted);
  margin-bottom: 10px;
}

.reminder-empty {
  font-size: 13px;
  color: var(--text-muted);
  font-style: italic;
}

.reminder-clue {
  font-size: 12px;
  color: var(--text-secondary);
  padding: 6px 0;
  border-bottom: 1px solid var(--border-color);
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
  color: var(--accent-purple);
  background: rgba(139, 92, 246, 0.1);
  padding: 1px 6px;
  border-radius: 10px;
}

.passphrase-input-wrap {
  display: flex;
  gap: 10px;
  width: 100%;
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
  gap: 8px;
  width: 100%;
}

.hint-item {
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 4px;
  padding: 10px 8px;
  background: var(--bg-surface);
  border: 1px solid var(--border-color);
  border-radius: var(--radius);
  font-size: 11px;
  color: var(--text-muted);
  text-align: center;
}

.hint-num {
  font-size: 16px;
  font-weight: 700;
  color: var(--accent-orange);
  font-family: var(--font-mono);
}

.hint-room {
  font-size: 10px;
  color: var(--accent-purple);
  font-weight: 600;
}

/* Solved screen */
.solved-screen {
  width: 100%;
  max-width: 640px;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 24px;
}

.declassified-stamp {
  font-size: 32px;
  font-weight: 900;
  letter-spacing: 12px;
  color: var(--accent-red);
  border: 4px solid var(--accent-red);
  padding: 8px 24px;
  transform: rotate(-6deg);
  opacity: 0.9;
}

.case-file {
  width: 100%;
  padding: 0;
  overflow: hidden;
}

.case-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 16px 24px;
  background: var(--bg-secondary);
  border-bottom: 2px solid var(--accent-red);
}

.case-label {
  font-size: 12px;
  font-weight: 700;
  font-family: var(--font-mono);
  letter-spacing: 1px;
  color: var(--text-muted);
}

.case-date {
  font-family: var(--font-mono);
  font-size: 12px;
  color: var(--text-muted);
}

.case-section {
  padding: 24px;
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
  color: var(--accent-green);
}

.case-footer {
  padding: 14px 24px;
  background: var(--bg-secondary);
  border-top: 1px solid var(--border-color);
  font-size: 12px;
  color: var(--text-muted);
  font-family: var(--font-mono);
}

.play-again {
  padding: 12px 32px;
  font-size: 15px;
  font-weight: 700;
}
</style>
