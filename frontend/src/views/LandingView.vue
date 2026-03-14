<template>
  <div class="landing">
    <div class="landing-inner">
      <div class="logo-block">
        <div class="logo-icon">🔐</div>
        <h1 class="logo-text">CLAIDO</h1>
        <p class="logo-sub">Corporate Escape — AI-Powered CTF</p>
      </div>

      <div class="brief card">
        <div class="brief-header">
          <span class="badge badge-red">CLASSIFIED</span>
          <span class="brief-date">2025-03-03</span>
        </div>
        <h2>INCIDENT REPORT — PROJECT NOVA</h2>
        <p>
          A corporate breach occurred overnight at NovaCorp headquarters.
          Sensitive vault data was compromised. The culprit is still at large.
          You have been deployed as a forensic investigator with access to
          seven internal systems. Find the culprit. Unlock the vault.
        </p>
        <ul class="brief-rooms">
          <li v-for="room in rooms" :key="room.id">
            <span class="room-dot"></span>
            <strong>{{ room.label }}</strong> — {{ room.desc }}
          </li>
        </ul>
      </div>

      <div v-if="error" class="error-msg">{{ error }}</div>

      <button
        class="btn-primary start-btn"
        :disabled="loading"
        @click="startGame"
      >
        <span v-if="loading">
          <span class="spinner"></span>
          Generating case file...
        </span>
        <span v-else>Begin Investigation</span>
      </button>

      <p class="disclaimer">Each session is AI-generated. No two cases are the same.</p>
    </div>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { useGameStore } from '../stores/gameStore.js'

const router = useRouter()
const store = useGameStore()
const loading = ref(false)
const error = ref('')

const rooms = [
  { id: 'shell', label: 'NovaShell', desc: 'Explore the internal filesystem' },
  { id: 'database', label: 'NovaCrime DB', desc: 'Query employee and access records' },
  { id: 'mail', label: 'NovaMail', desc: 'Read intercepted corporate emails' },
  { id: 'wiki', label: 'NovaWiki', desc: 'Browse classified internal documents' },
  { id: 'search', label: 'NovaSearch', desc: 'Analyse 50,000 system log entries' },
  { id: 'onion', label: 'The Onion', desc: 'Browse the dark web for leads' },
  { id: 'vault', label: 'Vault', desc: 'Enter the four-word passphrase to win' },
]

async function startGame() {
  loading.value = true
  error.value = ''
  try {
    await store.createSession()
    router.push('/shell')
  } catch (e) {
    error.value = e.message || 'Failed to connect to backend. Is it running?'
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.landing {
  min-height: 100vh;
  background: var(--bg-primary);
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 24px;
  overflow-y: auto;
}

.landing-inner {
  width: 100%;
  max-width: 620px;
  display: flex;
  flex-direction: column;
  gap: 24px;
}

.logo-block {
  text-align: center;
}

.logo-icon {
  font-size: 48px;
  margin-bottom: 8px;
}

.logo-text {
  font-size: 48px;
  font-weight: 900;
  letter-spacing: 12px;
  color: var(--text-primary);
  font-family: var(--font-mono);
  margin: 0;
}

.logo-sub {
  color: var(--text-muted);
  font-size: 13px;
  text-transform: uppercase;
  letter-spacing: 2px;
  margin-top: 8px;
}

.brief {
  padding: 24px;
}

.brief-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 16px;
}

.brief-date {
  font-family: var(--font-mono);
  font-size: 12px;
  color: var(--text-muted);
}

.brief h2 {
  font-size: 14px;
  font-weight: 700;
  font-family: var(--font-mono);
  color: var(--text-secondary);
  letter-spacing: 1px;
  margin-bottom: 12px;
}

.brief p {
  color: var(--text-secondary);
  font-size: 13px;
  line-height: 1.7;
  margin-bottom: 16px;
}

.brief-rooms {
  list-style: none;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.brief-rooms li {
  display: flex;
  align-items: center;
  gap: 10px;
  font-size: 13px;
  color: var(--text-secondary);
}

.room-dot {
  display: inline-block;
  width: 6px;
  height: 6px;
  border-radius: 50%;
  background: var(--accent-blue);
  flex-shrink: 0;
}

.error-msg {
  background: rgba(248, 81, 73, 0.1);
  border: 1px solid var(--accent-red);
  border-radius: var(--radius);
  color: var(--accent-red);
  padding: 12px 16px;
  font-size: 13px;
}

.start-btn {
  width: 100%;
  padding: 14px;
  font-size: 16px;
  font-weight: 700;
  letter-spacing: 1px;
  text-transform: uppercase;
}

.start-btn .spinner {
  margin-right: 8px;
  vertical-align: middle;
}

.disclaimer {
  text-align: center;
  font-size: 11px;
  color: var(--text-muted);
}
</style>
