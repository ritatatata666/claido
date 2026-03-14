<template>
  <Transition name="toast">
    <div v-if="store.clueNotification" class="toast" @click="store.clearNotification()">
      <div class="toast-icon">🔍</div>
      <div class="toast-body">
        <div class="toast-title">Clue Discovered</div>
        <div class="toast-room">{{ store.clueNotification.room }}</div>
        <div class="toast-text">{{ store.clueNotification.text }}</div>
      </div>
    </div>
  </Transition>
</template>

<script setup>
import { useGameStore } from '../stores/gameStore.js'
const store = useGameStore()
</script>

<style scoped>
.toast {
  position: fixed;
  bottom: 48px;
  left: 24px;
  z-index: 9999;
  display: flex;
  align-items: flex-start;
  gap: 12px;
  background: var(--bg-surface);
  border: 1px solid var(--accent-purple);
  border-left: 4px solid var(--accent-purple);
  border-radius: var(--radius);
  padding: 12px 16px;
  box-shadow: 0 4px 16px rgba(0, 0, 0, 0.6);
  cursor: pointer;
  max-width: 320px;
}

.toast-icon {
  font-size: 20px;
  flex-shrink: 0;
}

.toast-title {
  font-size: 12px;
  font-weight: 700;
  color: var(--accent-purple);
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.toast-room {
  font-size: 10px;
  color: var(--text-muted);
  text-transform: uppercase;
  letter-spacing: 0.5px;
  margin-top: 2px;
}

.toast-text {
  font-size: 13px;
  color: var(--text-primary);
  margin-top: 4px;
  line-height: 1.4;
}

.toast-enter-active {
  transition: transform 0.3s ease, opacity 0.3s ease;
}
.toast-leave-active {
  transition: transform 0.2s ease, opacity 0.2s ease;
}
.toast-enter-from {
  transform: translateX(-100%);
  opacity: 0;
}
.toast-leave-to {
  transform: translateX(-100%);
  opacity: 0;
}
</style>
