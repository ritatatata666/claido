<template>
  <div class="npc-chat-wrapper">
    <!-- Trigger button -->
    <button class="interrogate-btn" @click="open = true">
      Interrogate
    </button>

    <!-- Backdrop -->
    <Transition name="backdrop">
      <div v-if="open" class="backdrop" @click="open = false" />
    </Transition>

    <!-- Drawer -->
    <Transition name="drawer">
      <div v-if="open" class="drawer">
        <div class="drawer-header">
          <div class="npc-info">
            <span class="npc-avatar">{{ npcAvatar }}</span>
            <div>
              <div class="npc-name">{{ npcName }}</div>
              <div class="npc-role">{{ npcRole }}</div>
            </div>
          </div>
          <button class="close-btn" @click="open = false">✕</button>
        </div>

        <div class="messages" ref="messagesEl">
          <div v-if="messages.length === 0" class="empty-chat">
            <p>Begin your interrogation. Be strategic — witnesses won't reveal everything.</p>
          </div>
          <div
            v-for="(msg, i) in messages"
            :key="i"
            :class="['message', msg.role === 'user' ? 'message-user' : 'message-npc']"
          >
            <div class="bubble">{{ msg.content }}</div>
          </div>
          <div v-if="loading" class="message message-npc">
            <div class="bubble typing">
              <span></span><span></span><span></span>
            </div>
          </div>
        </div>

        <div class="input-area">
          <textarea
            v-model="draft"
            placeholder="Ask a question..."
            rows="2"
            @keydown.enter.exact.prevent="send"
            :disabled="loading"
          />
          <button class="btn-primary send-btn" :disabled="!draft.trim() || loading" @click="send">
            Send
          </button>
        </div>
      </div>
    </Transition>
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
  bottom: 56px;
  right: 24px;
  z-index: 100;
}

.interrogate-btn {
  background: var(--accent-purple);
  color: #fff;
  font-weight: 600;
  font-size: 13px;
  padding: 10px 18px;
  border-radius: var(--radius);
  box-shadow: 0 2px 8px rgba(139, 92, 246, 0.4);
}

.backdrop {
  position: fixed;
  inset: 0;
  background: rgba(0, 0, 0, 0.4);
  z-index: 200;
}

.drawer {
  position: fixed;
  top: 48px;
  right: 0;
  bottom: 36px;
  width: 380px;
  background: var(--bg-secondary);
  border-left: 1px solid var(--border-color);
  z-index: 300;
  display: flex;
  flex-direction: column;
}

.drawer-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 16px;
  border-bottom: 1px solid var(--border-color);
}

.npc-info {
  display: flex;
  align-items: center;
  gap: 12px;
}

.npc-avatar {
  font-size: 28px;
}

.npc-name {
  font-weight: 700;
  font-size: 14px;
  color: var(--text-primary);
}

.npc-role {
  font-size: 12px;
  color: var(--text-muted);
}

.close-btn {
  background: transparent;
  color: var(--text-secondary);
  font-size: 16px;
  padding: 4px 8px;
  border-radius: var(--radius);
}

.close-btn:hover {
  background: var(--bg-surface);
  opacity: 1;
}

.messages {
  flex: 1;
  overflow-y: auto;
  padding: 16px;
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.empty-chat {
  color: var(--text-muted);
  font-size: 13px;
  font-style: italic;
  text-align: center;
  padding: 24px 0;
}

.message {
  display: flex;
}

.message-user {
  justify-content: flex-end;
}

.message-npc {
  justify-content: flex-start;
}

.bubble {
  max-width: 85%;
  padding: 10px 14px;
  border-radius: var(--radius);
  font-size: 13px;
  line-height: 1.5;
}

.message-user .bubble {
  background: var(--accent-blue);
  color: #fff;
  border-bottom-right-radius: 2px;
}

.message-npc .bubble {
  background: var(--bg-surface);
  border: 1px solid var(--border-color);
  color: var(--text-primary);
  border-bottom-left-radius: 2px;
}

/* Typing indicator */
.typing {
  display: flex;
  gap: 4px;
  align-items: center;
  padding: 10px 14px;
}

.typing span {
  display: inline-block;
  width: 6px;
  height: 6px;
  background: var(--text-muted);
  border-radius: 50%;
  animation: bounce 1.2s ease-in-out infinite;
}

.typing span:nth-child(2) { animation-delay: 0.2s; }
.typing span:nth-child(3) { animation-delay: 0.4s; }

@keyframes bounce {
  0%, 60%, 100% { transform: translateY(0); }
  30% { transform: translateY(-6px); }
}

.input-area {
  display: flex;
  gap: 8px;
  padding: 12px;
  border-top: 1px solid var(--border-color);
}

.input-area textarea {
  flex: 1;
  resize: none;
  font-size: 13px;
}

.send-btn {
  align-self: flex-end;
}

/* Drawer transitions */
.drawer-enter-active, .drawer-leave-active {
  transition: transform 0.25s ease;
}
.drawer-enter-from, .drawer-leave-to {
  transform: translateX(100%);
}

.backdrop-enter-active, .backdrop-leave-active {
  transition: opacity 0.2s ease;
}
.backdrop-enter-from, .backdrop-leave-to {
  opacity: 0;
}
</style>
