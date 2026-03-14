<template>
  <div class="npc-chat-wrapper">
    <!-- Toggle button -->
    <button class="npc-toggle" @click="open = !open">
      <span class="npc-avatar">{{ npcAvatar }}</span>
      <span class="npc-toggle-name">{{ npcName }}</span>
      <span class="npc-toggle-role">{{ npcRole }}</span>
      <span class="toggle-arrow">{{ open ? '▼' : '▲' }}</span>
    </button>

    <!-- Chat panel (pops up above the toggle) -->
    <div v-if="open" class="npc-panel">
      <div class="npc-header">
        <div class="npc-info">
          <span class="npc-name">{{ npcName }}</span>
          <span class="npc-role-label">{{ npcRole }}</span>
        </div>
        <button class="npc-close" @click="open = false">✕</button>
      </div>

      <div class="npc-messages" ref="messagesEl">
        <div v-if="messages.length === 0" class="npc-intro">
          <p>Begin your interrogation. Be strategic — witnesses won't reveal everything.</p>
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
        <textarea
          v-model="draft"
          class="npc-input"
          :placeholder="`Ask ${npcName}...`"
          rows="2"
          :disabled="loading"
          @keydown.enter.exact.prevent="send"
        />
        <button class="btn-primary npc-send" :disabled="loading || !draft.trim()" @click="send">
          Send
        </button>
      </div>
    </div>
  </div>
</template>

<script setup>
import { ref, computed, watch, nextTick } from 'vue'
import { useGameStore } from '../stores/gameStore.js'

const props = defineProps({
  npcId: { type: String, required: true },
  npcName: { type: String, required: true },
  npcRole: { type: String, required: true },
})

const store = useGameStore()
const open = ref(false)
const draft = ref('')
const loading = ref(false)
const messagesEl = ref(null)

const npcAvatars = {
  receptionist: '👩',
  sysadmin: '🧑‍💻',
  archivist: '📚',
  cfo: '💼',
  ceo: '👔',
}
const npcAvatar = computed(() => npcAvatars[props.npcId] || '🧑')

const messages = computed(() => store.getNpcHistory(props.npcId))

watch(
  () => messages.value.length,
  async () => {
    await nextTick()
    if (messagesEl.value) {
      messagesEl.value.scrollTop = messagesEl.value.scrollHeight
    }
  }
)

async function send() {
  const text = draft.value.trim()
  if (!text || loading.value) return
  draft.value = ''
  loading.value = true
  try {
    await store.sendNpcMessage(props.npcId, text)
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.npc-chat-wrapper {
  position: fixed;
  bottom: 44px;
  left: 24px;
  z-index: 500;
  font-family: var(--font-mono);
}

/* Toggle button */
.npc-toggle {
  display: flex;
  align-items: center;
  gap: 8px;
  background: var(--bg-surface);
  border: 1px solid var(--accent-purple);
  color: var(--text-primary);
  padding: 6px 12px;
  border-radius: var(--radius);
  cursor: pointer;
  font-size: 12px;
  font-family: var(--font-mono);
  transition: border-color var(--transition), color var(--transition);
}

.npc-toggle:hover {
  border-color: var(--accent-purple);
  color: var(--text-primary);
  opacity: 1;
}

.npc-avatar { font-size: 14px; }
.npc-toggle-name { font-weight: 700; }
.npc-toggle-role { color: var(--text-muted); font-size: 11px; }
.toggle-arrow { font-size: 10px; color: var(--text-muted); }

/* Panel pops up above the toggle */
.npc-panel {
  position: absolute;
  bottom: calc(100% + 8px);
  left: 0;
  width: 340px;
  background: var(--bg-secondary);
  border: 1px solid var(--border-color);
  border-radius: var(--radius);
  display: flex;
  flex-direction: column;
  max-height: 420px;
  box-shadow: 0 8px 32px rgba(0, 0, 0, 0.6);
}

.npc-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 10px 14px;
  border-bottom: 1px solid var(--border-color);
  flex-shrink: 0;
}

.npc-info { display: flex; flex-direction: column; gap: 2px; }

.npc-name {
  font-size: 13px;
  font-weight: 700;
  color: var(--text-primary);
}

.npc-role-label {
  font-size: 11px;
  color: var(--accent-purple);
}

.npc-close {
  background: transparent;
  border: none;
  color: var(--text-muted);
  cursor: pointer;
  font-size: 13px;
  padding: 2px 4px;
}

.npc-close:hover { color: var(--text-primary); opacity: 1; }

.npc-messages {
  flex: 1;
  overflow-y: auto;
  padding: 12px;
  display: flex;
  flex-direction: column;
  gap: 8px;
  min-height: 120px;
}

.npc-intro {
  font-size: 12px;
  color: var(--text-muted);
  font-style: italic;
}

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
  background: rgba(31, 111, 235, 0.15);
  border: 1px solid rgba(31, 111, 235, 0.3);
  color: var(--text-primary);
}

.msg-npc .msg-bubble {
  background: var(--bg-surface);
  border: 1px solid var(--border-color);
  color: var(--text-secondary);
}

/* Typing dots */
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

/* Input row */
.npc-input-row {
  display: flex;
  gap: 8px;
  padding: 10px 12px;
  border-top: 1px solid var(--border-color);
  flex-shrink: 0;
}

.npc-input {
  flex: 1;
  font-family: var(--font-mono);
  font-size: 12px;
  resize: none;
}

.npc-send {
  padding: 6px 12px;
  font-size: 12px;
  font-weight: 600;
  align-self: flex-end;
}
</style>
