<template>
  <div v-if="store.teamMode === 'team'" class="team-folder-wrapper">
    <div class="team-folder-tab">TEAM OPS</div>
    <section class="team-panel">
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
            {{ clue.room }} — Hidden clue
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
      <p v-if="actionError" class="team-panel__error">{{ actionError }}</p>
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
        <span class="log-snippet">“{{ displayLogSnippet(entry) }}”</span>
      </div>
    </div>
  </section>
  </div>
</template>

<script setup>
import { computed, ref, watch } from 'vue'
import { useGameStore } from '../stores/gameStore.js'

const store = useGameStore()
const selectedClueId = ref('')
const isProcessing = ref(false)
const copyFeedback = ref('')
const actionError = ref('')

const lockedClues = computed(() => store.discoveredClues.filter(c => c.locked))
const visibleClues = computed(() => store.discoveredClues.filter(c => !c.locked && !store.protectedClueIds.includes(c.id)))

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

function displayLogSnippet(entry) {
  const isInvestigator = store.teamRole !== 'villain'
  const isVillainMaskAction = entry?.actor === 'villain' && entry?.action === 'lock'
  if (isInvestigator && isVillainMaskAction) return 'Hidden clue'
  return truncate(entry?.snippet || '', 42)
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
  actionError.value = ''
  try {
    await store.lockClue(clue)
    selectedClueId.value = visibleClues.value[0]?.id ?? ''
  } catch (err) {
    console.error(err)
    actionError.value = err?.message || 'Could not sabotage that clue.'
  } finally {
    isProcessing.value = false
  }
}

async function handleUnlock() {
  if (!selectedClueId.value) return
  const clue = lockedClues.value.find(c => c.id === selectedClueId.value)
  if (!clue) return
  isProcessing.value = true
  actionError.value = ''
  try {
    await store.unlockClue(clue)
    selectedClueId.value = lockedClues.value[0]?.id ?? ''
  } catch (err) {
    console.error(err)
    actionError.value = err?.message || 'Could not expose that clue.'
  } finally {
    isProcessing.value = false
  }
}
</script>

<style scoped>
.team-folder-wrapper {
  width: 100%;
  position: relative;
  z-index: 2;
  margin-top: 24px;
}

.team-folder-tab {
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

.team-panel {
  background:
    repeating-linear-gradient(
      180deg,
      transparent 0 28px,
      rgba(160, 130, 95, 0.06) 28px 29px
    ),
    linear-gradient(180deg, #d4b896, #c8a97a);
  border: 1px solid #a88b62;
  border-radius: 0 6px 6px 6px;
  padding: 24px 22px;
  color: #3d2510;
  box-shadow:
    0 8px 24px rgba(80, 50, 20, 0.25),
    inset 0 1px 0 rgba(255, 255, 255, 0.15),
    inset 0 -1px 0 rgba(0, 0, 0, 0.05);
}

.team-panel__header {
  display: flex;
  justify-content: space-between;
  gap: 16px;
  align-items: flex-start;
  border-bottom: 2px solid rgba(139, 100, 60, 0.3);
  padding-bottom: 14px;
  margin-bottom: 18px;
}

.team-panel__label {
  font-size: 12px;
  font-family: var(--font-mono);
  letter-spacing: 1.5px;
  text-transform: uppercase;
  color: #7a5c3a;
  margin: 0;
}

.team-panel__title {
  margin: 6px 0 4px;
  font-size: 22px;
  line-height: 1.2;
  color: #3d2510;
}

.team-panel__desc {
  margin: 0;
  font-size: 13px;
  color: #6b5030;
}

.team-panel__tokens {
  display: flex;
  gap: 12px;
}

.token-pill {
  background: rgba(90, 61, 36, 0.1);
  border: 1px solid rgba(139, 100, 60, 0.3);
  border-radius: 6px;
  padding: 8px 16px;
  display: flex;
  flex-direction: column;
  align-items: center;
  font-size: 12px;
  letter-spacing: 0.5px;
}

.token-pill--good {
  background: rgba(60, 90, 130, 0.12);
  border-color: rgba(70, 100, 140, 0.3);
}

.token-label {
  color: #7a5c3a;
  font-family: var(--font-mono);
  font-size: 11px;
  letter-spacing: 1px;
  text-transform: uppercase;
}

.token-value {
  font-size: 20px;
  font-weight: 700;
  color: #3d2510;
}

.team-panel__actions {
  display: flex;
  flex-direction: column;
  gap: 10px;
  margin-bottom: 18px;
}

.team-panel__input-label {
  font-size: 12px;
  font-family: var(--font-mono);
  text-transform: uppercase;
  letter-spacing: 1px;
  color: #7a5c3a;
  margin-bottom: 4px;
}

.team-panel__select {
  background: rgba(255, 250, 240, 0.6);
  border: 1px solid rgba(139, 100, 60, 0.3);
  border-radius: 6px;
  padding: 10px 12px;
  color: #3d2510;
  font-size: 13px;
}

.team-panel__button {
  background: linear-gradient(180deg, #c45144, #a83c30);
  border: none;
  border-radius: 6px;
  padding: 12px 16px;
  font-size: 13px;
  font-weight: 700;
  color: #fffaf0;
  cursor: pointer;
  text-transform: uppercase;
  letter-spacing: 1px;
  transition: transform 0.2s ease, box-shadow 0.2s ease;
  box-shadow: 0 6px 16px rgba(139, 58, 42, 0.25);
}

.team-panel__button:disabled {
  opacity: 0.5;
  cursor: not-allowed;
  transform: none;
  box-shadow: none;
}

.team-panel__button:not(:disabled):hover {
  transform: translateY(-1px);
  box-shadow: 0 8px 20px rgba(185, 70, 54, 0.3);
}

.team-panel__hint {
  font-size: 12px;
  color: #8b6f4e;
  margin: 0;
}

.team-panel__error {
  font-size: 12px;
  color: #ad3328;
  margin: 2px 0 0;
}

.team-panel__code {
  padding: 14px 0 6px;
  border-bottom: 2px solid rgba(139, 100, 60, 0.3);
}

.team-panel__code-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  gap: 10px;
}

.code-label {
  font-size: 11px;
  font-family: var(--font-mono);
  letter-spacing: 1.5px;
  text-transform: uppercase;
  color: #7a5c3a;
}

.code-value {
  font-size: 24px;
  letter-spacing: 4px;
  color: #3d2510;
  display: block;
  margin-top: 6px;
  font-family: var(--font-mono);
}

.code-note {
  margin: 6px 0 0;
  font-size: 11px;
  color: #8b6f4e;
}

.code-copy-btn {
  background: rgba(90, 61, 36, 0.1);
  color: #5a3d24;
  border: 1px solid rgba(139, 100, 60, 0.3);
  border-radius: 6px;
  padding: 5px 12px;
  font-size: 12px;
  font-family: var(--font-mono);
  letter-spacing: 1px;
  text-transform: uppercase;
  cursor: pointer;
  transition: background 0.2s ease, transform 0.2s ease;
}

.code-copy-btn:disabled {
  opacity: 0.4;
  cursor: not-allowed;
}

.code-copy-btn:not(:disabled):hover {
  background: rgba(90, 61, 36, 0.2);
  transform: translateY(-1px);
}

.team-panel__roster {
  padding-top: 14px;
  border-top: 2px solid rgba(139, 100, 60, 0.3);
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.team-panel__roster-title {
  margin: 0;
  font-size: 13px;
  font-family: var(--font-mono);
  letter-spacing: 1.5px;
  text-transform: uppercase;
  color: #7a5c3a;
}

.team-panel__roster-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  font-size: 13px;
  color: #3d2510;
}

.roster-role {
  padding: 3px 10px;
  border-radius: 4px;
  font-size: 11px;
  font-family: var(--font-mono);
  letter-spacing: 1px;
  text-transform: uppercase;
  font-weight: 700;
}

.roster-role--villain {
  background: rgba(185, 70, 54, 0.15);
  color: #8b3a2a;
  border: 1px solid rgba(185, 70, 54, 0.3);
}

.roster-role--good {
  background: rgba(60, 100, 140, 0.12);
  color: #3a5c7a;
  border: 1px solid rgba(60, 100, 140, 0.25);
}

.team-panel__log-title {
  margin: 0 0 6px;
  font-size: 13px;
  font-family: var(--font-mono);
  text-transform: uppercase;
  letter-spacing: 1.5px;
  color: #7a5c3a;
}

.team-panel__log {
  border-top: 2px solid rgba(139, 100, 60, 0.3);
  padding-top: 14px;
  max-height: 150px;
  overflow: auto;
}

.team-panel__log-entry {
  display: flex;
  flex-direction: column;
  gap: 2px;
  padding: 8px 0;
  border-bottom: 1px solid rgba(139, 100, 60, 0.15);
}

.team-panel__log-entry:last-child {
  border-bottom: none;
}

.log-role {
  font-size: 11px;
  font-family: var(--font-mono);
  text-transform: uppercase;
  letter-spacing: 1px;
}

.log-role--villain {
  color: #8b3a2a;
}

.log-role--good {
  color: #3a5c7a;
}

.log-text {
  font-size: 14px;
  font-weight: 600;
  color: #3d2510;
}

.log-snippet {
  font-size: 12px;
  color: #8b6f4e;
}

.team-panel__log-empty {
  font-size: 12px;
  color: #8b6f4e;
  padding: 6px 0;
}
</style>
