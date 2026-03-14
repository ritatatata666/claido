<template>
  <section v-if="store.teamMode === 'team'" class="team-panel">
    <div class="team-panel__header">
      <div>
        <p class="team-panel__label">{{ roleLabel }}</p>
        <h3 class="team-panel__title">{{ roleTitle }}</h3>
        <p class="team-panel__desc">{{ roleDescription }}</p>
      </div>
      <div class="team-panel__tokens">
        <div class="token-pill">
          <span class="token-label">Sabotage</span>
          <span class="token-value">{{ store.villainTokens }}</span>
        </div>
        <div class="token-pill token-pill--good">
          <span class="token-label">Countermeasures</span>
          <span class="token-value">{{ store.goodTokens }}</span>
        </div>
      </div>
    </div>

    <div class="team-panel__code">
      <div class="team-panel__code-header">
        <span class="code-label">Invite code</span>
        <button
          class="code-copy-btn"
          type="button"
          :disabled="!store.joinCode"
          @click="copyInviteCode"
        >
          {{ copyFeedback || 'Copy code' }}
        </button>
      </div>
      <strong class="code-value">{{ store.joinCode || 'Generating…' }}</strong>
      <p class="code-note">Share this code with other players so they can join your session.</p>
    </div>

    <div v-if="store.teamMembers.length" class="team-panel__roster">
      <h4 class="team-panel__roster-title">Players in session</h4>
      <div
        class="team-panel__roster-item"
        v-for="member in store.teamMembers"
        :key="member.memberId"
      >
        <span>{{ member.displayName }}</span>
        <span
          :class="[
            'roster-role',
            member.role === 'villain' ? 'roster-role--villain' : 'roster-role--good'
          ]"
        >
          {{ member.role === 'villain' ? 'Villain' : 'Investigator' }}
        </span>
      </div>
    </div>

    <div class="team-panel__actions">
      <template v-if="store.teamRole === 'villain'">
        <label class="team-panel__input-label">Choose a discovered clue to mask</label>
        <select v-model="selectedClueId" class="team-panel__select">
          <option disabled value="">Select a clue</option>
          <option
            v-for="clue in visibleClues"
            :key="clue.id"
            :value="clue.id"
          >
            {{ clue.room }} — {{ truncate(clue.text, 40) }}
          </option>
        </select>
        <button
          class="team-panel__button"
          :disabled="!selectedClueId || !canUseVillainToken || isProcessing"
          @click="handleLock"
        >
          Sabotage hint ({{ store.villainTokens }} left)
        </button>
        <p class="team-panel__hint">Masked clues will appear hidden to investigate team members until they spend a counter token.</p>
      </template>
      <template v-else>
        <label class="team-panel__input-label">Reveal a villain-locked clue</label>
        <select v-model="selectedClueId" class="team-panel__select">
          <option disabled value="">Select a locked clue</option>
          <option
            v-for="clue in lockedClues"
            :key="clue.id"
            :value="clue.id"
          >
            {{ clue.room }} — {{ truncate(clue.text, 40) }}
          </option>
        </select>
        <button
          class="team-panel__button"
          :disabled="!selectedClueId || !canUseGoodToken || isProcessing"
          @click="handleUnlock"
        >
          Expose hint ({{ store.goodTokens }} counters)
        </button>
        <p class="team-panel__hint">Good team reveals unblock a locked clue so the final passphrase stays in reach.</p>
      </template>
    </div>

    <div class="team-panel__log">
      <h4 class="team-panel__log-title">Team Pulse</h4>
      <div v-if="recentActions.length === 0" class="team-panel__log-empty">No team actions this round.</div>
      <div
        v-for="entry in recentActions"
        :key="entry.timestamp + entry.action + entry.clueId"
        class="team-panel__log-entry"
      >
        <span :class="['log-role', entry.actor === 'villain' ? 'log-role--villain' : 'log-role--good']">
          {{ entry.actor === 'villain' ? 'Villain' : 'Investigator' }}
        </span>
        <span class="log-text">
          {{ entry.actor === 'villain' ? 'masked' : 'revealed' }} {{ entry.room }} clue
        </span>
        <span class="log-snippet">“{{ truncate(entry.snippet, 42) }}”</span>
      </div>
    </div>
  </section>
</template>

<script setup>
import { computed, ref, watch } from 'vue'
import { useGameStore } from '../stores/gameStore.js'

const store = useGameStore()
const selectedClueId = ref('')
const isProcessing = ref(false)
const copyFeedback = ref('')

const lockedClues = computed(() => store.discoveredClues.filter(c => c.locked))
const visibleClues = computed(() => store.discoveredClues.filter(c => !c.locked))

const recentActions = computed(() => store.teamActionLog)

const roleTitle = computed(() => store.teamRole === 'villain' ? 'Villain Saboteur' : 'Lead Investigator')
const roleLabel = computed(() => store.teamRole === 'villain' ? 'Team Mode — Saboteur' : 'Team Mode — Lead')
const roleDescription = computed(() => store.teamRole === 'villain'
  ? 'Keep the clues obscured and force investigators to spend precious time countering your sabotage.'
  : 'Coordinate the investigation, reveal sabotaged clues, and keep the trail to the vault clear.')

const canUseVillainToken = computed(() => store.villainTokens > 0 && visibleClues.value.length > 0)
const canUseGoodToken = computed(() => store.goodTokens > 0 && lockedClues.value.length > 0)

watch([visibleClues, lockedClues], () => {
  if (store.teamRole === 'villain') {
    if (!visibleClues.value.find(c => c.id === selectedClueId.value)) {
      selectedClueId.value = visibleClues.value[0]?.id ?? ''
    }
  } else {
    if (!lockedClues.value.find(c => c.id === selectedClueId.value)) {
      selectedClueId.value = lockedClues.value[0]?.id ?? ''
    }
  }
}, { immediate: true })
function truncate(text, max) {
  if (!text) return ''
  return text.length <= max ? text : `${text.slice(0, max)}…`
}

async function copyInviteCode() {
  if (!store.joinCode) return
  if (!navigator?.clipboard) {
    copyFeedback.value = 'Clipboard unavailable'
  } else {
    try {
      await navigator.clipboard.writeText(store.joinCode)
      copyFeedback.value = 'Copied!'
    } catch (err) {
      console.error(err)
      copyFeedback.value = 'Copy failed'
    }
  }
  setTimeout(() => { copyFeedback.value = '' }, 1500)
}

async function handleLock() {
  if (!selectedClueId.value) return
  const clue = visibleClues.value.find(c => c.id === selectedClueId.value)
  if (!clue) return
  isProcessing.value = true
  try {
    await store.lockClue(clue)
    selectedClueId.value = visibleClues.value[0]?.id ?? ''
  } catch (err) {
    console.error(err)
  } finally {
    isProcessing.value = false
  }
}

async function handleUnlock() {
  if (!selectedClueId.value) return
  const clue = lockedClues.value.find(c => c.id === selectedClueId.value)
  if (!clue) return
  isProcessing.value = true
  try {
    await store.unlockClue(clue)
    selectedClueId.value = lockedClues.value[0]?.id ?? ''
  } catch (err) {
    console.error(err)
  } finally {
    isProcessing.value = false
  }
}
</script>

<style scoped>
.team-panel {
  background: #06060b;
  border: 1px solid rgba(255, 255, 255, 0.08);
  border-radius: 14px;
  padding: 20px;
  margin-top: 24px;
  color: #f5f5f7;
  box-shadow: 0 12px 40px rgba(0, 0, 0, 0.6);
}

.team-panel__header {
  display: flex;
  justify-content: space-between;
  gap: 16px;
  align-items: flex-start;
  border-bottom: 1px solid rgba(255, 255, 255, 0.08);
  padding-bottom: 14px;
  margin-bottom: 18px;
}

.team-panel__label {
  font-size: 12px;
  letter-spacing: 1px;
  text-transform: uppercase;
  color: #9b9ba3;
  margin: 0;
}

.team-panel__title {
  margin: 6px 0 4px;
  font-size: 22px;
  line-height: 1.2;
}

.team-panel__desc {
  margin: 0;
  font-size: 13px;
  color: #b6b6c3;
}

.team-panel__tokens {
  display: flex;
  gap: 12px;
}

.token-pill {
  background: rgba(255, 255, 255, 0.05);
  border-radius: 999px;
  padding: 6px 14px;
  display: flex;
  flex-direction: column;
  align-items: center;
  font-size: 12px;
  letter-spacing: 0.5px;
}

.token-pill--good {
  background: rgba(45, 158, 255, 0.15);
}

.token-label {
  color: #8a8a97;
}

.token-value {
  font-size: 18px;
  font-weight: 700;
}

.team-panel__actions {
  display: flex;
  flex-direction: column;
  gap: 10px;
  margin-bottom: 18px;
}

.team-panel__input-label {
  font-size: 12px;
  text-transform: uppercase;
  letter-spacing: 1px;
  color: #8a8a97;
  margin-bottom: 4px;
}

.team-panel__select {
  background: #0b0b12;
  border: 1px solid rgba(255, 255, 255, 0.1);
  border-radius: 8px;
  padding: 8px 12px;
  color: #fff;
  font-size: 13px;
}

.team-panel__button {
  background: linear-gradient(135deg, #ff4f78, #ffb347);
  border: none;
  border-radius: 10px;
  padding: 10px 14px;
  font-size: 13px;
  font-weight: 700;
  color: #0b0b12;
  cursor: pointer;
  transition: transform 0.2s ease, box-shadow 0.2s ease;
}

.team-panel__button:disabled {
  opacity: 0.6;
  cursor: not-allowed;
  transform: none;
  box-shadow: none;
}

.team-panel__button:not(:disabled):hover {
  transform: translateY(-1px);
  box-shadow: 0 6px 12px rgba(255, 79, 120, 0.35);
}

.team-panel__hint {
  font-size: 12px;
  color: #87878f;
  margin: 0;
}

.team-panel__code {
  padding: 12px 0 4px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.08);
}

.team-panel__code-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 10px;
}

.code-label {
  font-size: 11px;
  letter-spacing: 0.5px;
  text-transform: uppercase;
  color: #9d9da5;
}

.code-value {
  font-size: 24px;
  letter-spacing: 4px;
  color: #f3f3ff;
  display: block;
  margin-top: 6px;
}

.code-note {
  margin: 4px 0 0;
  font-size: 11px;
  color: #a6a6b3;
}

.code-copy-btn {
  background: rgba(255, 255, 255, 0.1);
  color: #f6f6ff;
  border: 1px solid rgba(255, 255, 255, 0.25);
  border-radius: 999px;
  padding: 4px 10px;
  font-size: 12px;
  letter-spacing: 0.5px;
  text-transform: uppercase;
  cursor: pointer;
  transition: background 0.2s ease, transform 0.2s ease;
}

.code-copy-btn:disabled {
  opacity: 0.4;
  cursor: not-allowed;
}

.code-copy-btn:not(:disabled):hover {
  background: rgba(255, 255, 255, 0.18);
  transform: translateY(-1px);
}

.team-panel__roster {
  padding-top: 12px;
  border-top: 1px solid rgba(255, 255, 255, 0.06);
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.team-panel__roster-title {
  margin: 0;
  font-size: 13px;
  letter-spacing: 0.5px;
  text-transform: uppercase;
  color: #8a8a97;
}

.team-panel__roster-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  font-size: 13px;
}

.roster-role {
  padding: 2px 8px;
  border-radius: 999px;
  font-size: 11px;
  letter-spacing: 0.5px;
  text-transform: uppercase;
}

.roster-role--villain {
  background: rgba(255, 92, 92, 0.15);
  color: #ff8b8b;
}

.roster-role--good {
  background: rgba(93, 209, 255, 0.15);
  color: #7dd7ff;
}

.team-panel__log-title {
  margin: 0 0 6px;
  font-size: 13px;
  text-transform: uppercase;
  letter-spacing: 1px;
  color: #8a8a97;
}

.team-panel__log {
  border-top: 1px solid rgba(255, 255, 255, 0.06);
  padding-top: 12px;
  max-height: 150px;
  overflow: auto;
}

.team-panel__log-entry {
  display: flex;
  flex-direction: column;
  gap: 2px;
  padding: 8px 0;
  border-bottom: 1px solid rgba(255, 255, 255, 0.04);
}

.team-panel__log-entry:last-child {
  border-bottom: none;
}

.log-role {
  font-size: 11px;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.log-role--villain {
  color: #ff5c5c;
}

.log-role--good {
  color: #5dd1ff;
}

.log-text {
  font-size: 14px;
  font-weight: 600;
}

.log-snippet {
  font-size: 12px;
  color: #a0a0b0;
}

.team-panel__log-empty {
  font-size: 12px;
  color: #8a8a97;
  padding: 6px 0;
}
</style>
