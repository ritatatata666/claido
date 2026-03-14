<template>
  <Transition name="clue-slide">
    <div v-if="store.clueNotification" class="clue-notification">
      <div class="clue-icon">🔍</div>
      <div class="clue-body">
        <div class="clue-label">New Evidence Found</div>
        <div class="clue-text">{{ store.clueNotification.text }}</div>
        <div class="clue-room">{{ store.clueNotification.room }}</div>
      </div>
      <button class="clue-close" @click="store.clearNotification()">✕</button>
    </div>
  </Transition>
</template>

<script setup>
import { useGameStore } from '../stores/gameStore.js'
const store = useGameStore()
</script>

<style scoped>
.clue-notification {
  position: fixed;
  bottom: 48px;
  right: 24px;
  z-index: 10000;
  display: flex;
  align-items: flex-start;
  gap: 12px;
  background: #0d1a10;
  border: 1px solid var(--accent-green);
  border-radius: var(--radius);
  padding: 14px 16px;
  max-width: 340px;
  box-shadow: 0 4px 24px rgba(0, 0, 0, 0.5), 0 0 20px rgba(63, 185, 80, 0.2);
}

.clue-icon { font-size: 20px; flex-shrink: 0; }

.clue-body { flex: 1; min-width: 0; }

.clue-label {
  font-size: 10px;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 1px;
  color: var(--accent-green);
  margin-bottom: 4px;
}

.clue-text {
  font-size: 13px;
  color: var(--text-primary);
  line-height: 1.4;
  margin-bottom: 4px;
}

.clue-room {
  font-size: 11px;
  color: var(--text-muted);
}

.clue-close {
  background: transparent;
  border: none;
  color: var(--text-muted);
  font-size: 12px;
  padding: 0;
  cursor: pointer;
  flex-shrink: 0;
}

.clue-close:hover { color: var(--text-primary); }

.clue-slide-enter-active { transition: transform 0.3s ease, opacity 0.3s ease; }
.clue-slide-leave-active { transition: transform 0.2s ease, opacity 0.2s ease; }
.clue-slide-enter-from { transform: translateX(100%); opacity: 0; }
.clue-slide-leave-to { transform: translateX(100%); opacity: 0; }
</style>
