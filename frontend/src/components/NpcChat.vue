<template>
  <div :class="['npc-chat', { open: isOpen }]">
    <!-- Toggle button -->
    <button class="npc-toggle" @click="isOpen = !isOpen">
      <span class="npc-avatar">👤</span>
      <span class="npc-toggle-name">{{ npcName }}</span>
      <span class="npc-toggle-role">{{ npcRole }}</span>
      <span class="toggle-arrow">{{ isOpen ? '▼' : '▲' }}</span>
    </button>

    <!-- Chat panel -->
    <div v-if="isOpen" class="npc-panel">
      <div class="npc-header">
        <div class="npc-info">
          <span class="npc-name">{{ npcName }}</span>
          <span class="npc-role-label">{{ npcRole }}</span>
        </div>
        <button class="npc-close" @click="isOpen = false">✕</button>
      </div>

      <div class="npc-messages" ref="messagesEl">
        <div v-if="messages.length === 0" class="npc-intro">
          <p>You can ask {{ npcName }} for information about this investigation.</p>
        </div>
        <div
          v-for="(msg, i) in messages"
          :key="i"
          :class="['npc-msg', msg.role === 'user' ? 'msg-user' : 'msg-npc']"
        >
          <div class="msg-bubble">{{ msg.content }}</div>
        </div>
        <div v-if="loading" class="npc-typing">
          <span class="dot"></span><span class="dot"></span><span class="dot"></span>
        </div>
      </div>

      <div class="npc-input-row">
        <input
          v-model="inputText"
          class="npc-input"
          :placeholder="`Ask ${npcName}...`"
          :disabled="loading"
          @keydown.enter="sendMessage"
        />
        <button class="npc-send" :disabled="loading || !inputText.trim()" @click="sendMessage">
          Send
        </button>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, nextTick, onMounted } from 'vue'
import { useGameStore } from '../stores/gameStore.js'

const props = defineProps({
  npcId: { type: String, required: true },
  npcName: { type: String, required: true },
  npcRole: { type: String, default: '' },
})

const store = useGameStore()
const isOpen = ref(false)
const inputText = ref('')
const loading = ref(false)
const messagesEl = ref(null)

const messages = computed(() => store.getNpcHistory(props.npcId))

async function sendMessage() {
  const text = inputText.value.trim()
  if (!text || loading.value) return
  inputText.value = ''
  loading.value = true
  try {
    await store.sendNpcMessage(props.npcId, text)
    await nextTick()
    if (messagesEl.value) {
      messagesEl.value.scrollTop = messagesEl.value.scrollHeight
    }
  } catch (e) {
    store.addNpcMessage(props.npcId, 'assistant', 'Sorry, I\'m unable to respond right now.')
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.npc-chat {
  position: fixed;
  bottom: 36px;
  left: 24px;
  z-index: 1000;
  font-family: var(--font-mono);
}

.npc-toggle {
  display: flex;
  align-items: center;
  gap: 8px;
  background: #1e1e1e;
  border: 1px solid rgba(255, 255, 255, 0.1);
  color: #f5f5f5;
  padding: 6px 12px;
  border-radius: var(--radius);
  cursor: pointer;
  font-size: 12px;
  font-family: var(--font-mono);
  transition: border-color var(--transition);
}

.npc-toggle:hover { border-color: #c9a24a; color: #fff9d4; }

.npc-avatar { font-size: 14px; }

.npc-toggle-name { font-weight: 700; }

.npc-toggle-role {
  color: var(--text-muted);
  font-size: 11px;
}

.toggle-arrow { font-size: 10px; color: var(--text-muted); }

.npc-panel {
  position: absolute;
  bottom: calc(100% + 8px);
  left: 0;
  width: 320px;
  background: #1e1e1e;
  border: 1px solid rgba(255, 255, 255, 0.12);
  border-radius: var(--radius);
  display: flex;
  flex-direction: column;
  max-height: 400px;
  box-shadow: 0 8px 32px rgba(0, 0, 0, 0.5);
}

.npc-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 10px 14px;
  border-bottom: 1px solid rgba(255, 255, 255, 0.12);
}

.npc-info { display: flex; flex-direction: column; gap: 2px; }

.npc-name { font-size: 13px; font-weight: 700; color: #fff8e5; }

.npc-role-label { font-size: 11px; color: #c9a24a; }

.npc-close {
  background: transparent;
  border: none;
  color: var(--text-muted);
  cursor: pointer;
  font-size: 13px;
  padding: 2px 4px;
}

.npc-close:hover { color: var(--text-primary); }

.npc-messages {
  flex: 1;
  overflow-y: auto;
  padding: 12px;
  display: flex;
  flex-direction: column;
  gap: 8px;
  min-height: 120px;
}

.npc-intro { font-size: 12px; color: var(--text-muted); font-style: italic; }

.npc-msg { display: flex; }

.msg-user { justify-content: flex-end; }
.msg-npc { justify-content: flex-start; }

.msg-bubble {
  max-width: 80%;
  padding: 8px 12px;
  border-radius: var(--radius);
  font-size: 12px;
  line-height: 1.5;
}

.msg-user .msg-bubble {
  background: #3a3a3a;
  border: 1px solid rgba(255, 255, 255, 0.15);
  color: #ffffff;
}

.msg-npc .msg-bubble {
  background: #2a2a2a;
  border: 1px solid rgba(255, 255, 255, 0.08);
  color: #f0f0f0;
}

.npc-typing {
  display: flex;
  gap: 4px;
  padding: 4px 0;
}

.dot {
  width: 6px;
  height: 6px;
  border-radius: 50%;
  background: var(--text-muted);
  animation: bounce 1.2s infinite;
}

.dot:nth-child(2) { animation-delay: 0.2s; }
.dot:nth-child(3) { animation-delay: 0.4s; }

@keyframes bounce {
  0%, 100% { transform: translateY(0); opacity: 0.4; }
  50% { transform: translateY(-4px); opacity: 1; }
}

.npc-input-row {
  display: flex;
  gap: 8px;
  padding: 10px 12px;
  border-top: 1px solid rgba(255, 255, 255, 0.1);
  background: #121212;
  border-radius: 0 0 var(--radius) var(--radius);
}

.npc-input {
  flex: 1;
  font-family: var(--font-mono);
  font-size: 12px;
  padding: 6px 10px;
  background: #1b1b1b;
  border: 1px solid rgba(255, 255, 255, 0.12);
  border-radius: var(--radius);
  color: #f5f5f5;
  outline: none;
}

.npc-input:focus { border-color: #c9a24a; }

.npc-send {
  padding: 6px 12px;
  font-size: 12px;
  font-weight: 600;
  font-family: var(--font-mono);
  background: #c9a24a;
  border: 1px solid #c9a24a;
  color: #050505;
  border-radius: var(--radius);
  cursor: pointer;
  transition: transform 0.15s;
}

.npc-send:hover:not(:disabled) { transform: scale(1.02); }
.npc-send:disabled { opacity: 0.5; cursor: not-allowed; }
</style>
