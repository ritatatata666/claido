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
  right: 24px;
  z-index: 9999;
  display: flex;
  align-items: flex-start;
  gap: 12px;
  background: linear-gradient(180deg, rgba(255, 250, 242, 0.98), rgba(241, 228, 205, 0.98));
  border: 1px solid rgba(94, 67, 44, 0.22);
  border-radius: 6px;
  padding: 14px 18px 14px 16px;
  box-shadow: var(--paper-shadow);
  cursor: pointer;
  max-width: 340px;
  transform: rotate(-1.2deg);
}

.toast::before {
  content: '';
  position: absolute;
  top: -8px;
  left: 18px;
  width: 14px;
  height: 14px;
  border-radius: 50%;
  background: #c74941;
  box-shadow: 0 2px 6px rgba(0, 0, 0, 0.24), inset 0 1px 1px rgba(255, 255, 255, 0.6);
}

.toast::after {
  content: '';
  position: absolute;
  inset: 0;
  border-radius: 6px;
  background: repeating-linear-gradient(
    180deg,
    transparent 0 23px,
    rgba(119, 92, 69, 0.08) 23px 24px
  );
  pointer-events: none;
}

.toast-icon {
  font-size: 20px;
  flex-shrink: 0;
  position: relative;
  z-index: 1;
}

.toast-title {
  font-size: 12px;
  font-weight: 700;
  color: var(--accent-red);
  text-transform: uppercase;
  letter-spacing: 1px;
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
  position: relative;
  z-index: 1;
}

.toast-body {
  position: relative;
  z-index: 1;
}

.toast-enter-active {
  transition: transform 0.3s ease, opacity 0.3s ease;
}
.toast-leave-active {
  transition: transform 0.2s ease, opacity 0.2s ease;
}
.toast-enter-from {
  transform: translateX(100%);
  opacity: 0;
}
.toast-leave-to {
  transform: translateX(100%);
  opacity: 0;
}
</style>
