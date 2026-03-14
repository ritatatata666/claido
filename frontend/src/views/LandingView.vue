<template>
  <div class="landing">
    <div class="scanlines"></div>

    <div class="landing-inner">
      <!-- Top bar -->
      <div class="top-bar">
        <span class="classified-badge">● CLASSIFIED</span>
        <span class="case-file">CASE FILE #NC-2025-0303</span>
      </div>

      <!-- Glitchy heading -->
      <div class="hero-block">
        <h1 class="claido-heading" data-text="CLAIDO">CLAIDO</h1>
        <p class="hero-subtitle">NovaCorp Internal Breach — Investigator Access Only</p>
      </div>

      <!-- Briefing card styled as classified document -->
      <div class="briefing-card">
        <div class="watermark">TOP SECRET</div>
        <div class="card-inner">
          <div class="card-header">
            <div class="card-title-block">
              <span class="card-stamp-label">INCIDENT REPORT</span>
              <span class="card-date">2025-03-03</span>
            </div>
          </div>
          <p class="card-summary">
            A corporate breach occurred overnight at NovaCorp headquarters.
            Sensitive vault data was compromised. The culprit is still at large.
            You have been deployed as a forensic investigator with access to
            seven internal systems. Find the culprit. Unlock the vault.
          </p>
          <ul class="room-list">
            <li v-for="room in rooms" :key="room.id">
              <span class="bullet">▪</span>
              <strong>{{ room.label }}</strong> — {{ room.desc }}
            </li>
          </ul>
          <div class="declassified-stamp">DECLASSIFIED</div>
        </div>
      </div>

      <!-- Error -->
      <div v-if="error" class="error-msg">{{ error }}</div>

      <!-- CTA button -->
      <button
        class="start-btn"
        :disabled="loading"
        @click="startGame"
      >
        <span v-if="loading">
          <span class="spinner-dot"></span>
          Generating case file...
        </span>
        <span v-else>BEGIN INVESTIGATION</span>
      </button>

      <p class="disclaimer">Session expires when tab closes. Each case is AI-generated.</p>
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
    router.push('/hub')
  } catch (e) {
    error.value = e.message || 'Failed to connect to backend. Is it running?'
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
/* ── Base ─────────────────────────────────────────────── */
.landing {
  min-height: 100vh;
  background: #0a0a0a;
  display: flex;
  align-items: flex-start;
  justify-content: center;
  padding: 40px 24px 60px;
  overflow-y: auto;
  position: relative;
  font-family: 'Courier New', Courier, monospace;
}

/* Scanline overlay */
.scanlines {
  position: fixed;
  inset: 0;
  pointer-events: none;
  z-index: 0;
  background: repeating-linear-gradient(
    to bottom,
    transparent,
    transparent 3px,
    rgba(0, 0, 0, 0.08) 3px,
    rgba(0, 0, 0, 0.08) 4px
  );
}

.landing-inner {
  position: relative;
  z-index: 1;
  width: 100%;
  max-width: 660px;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 28px;
}

/* ── Top bar ──────────────────────────────────────────── */
.top-bar {
  width: 100%;
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.classified-badge {
  color: #e53935;
  font-size: 12px;
  font-weight: 700;
  letter-spacing: 2px;
  text-transform: uppercase;
  animation: blink-badge 1.2s step-start infinite;
}

@keyframes blink-badge {
  0%, 100% { opacity: 1; }
  50% { opacity: 0; }
}

.case-file {
  color: #666;
  font-size: 11px;
  letter-spacing: 1px;
}

/* ── Hero heading ─────────────────────────────────────── */
.hero-block {
  text-align: center;
}

.claido-heading {
  font-size: clamp(64px, 12vw, 96px);
  font-weight: 900;
  letter-spacing: 18px;
  color: #00ff41;
  margin: 0 0 12px;
  position: relative;
  animation: flicker 4s infinite;
  text-shadow:
    0 0 8px #00ff41,
    0 0 24px rgba(0, 255, 65, 0.4);
}

.claido-heading::before,
.claido-heading::after {
  content: attr(data-text);
  position: absolute;
  left: 0;
  right: 0;
  top: 0;
  overflow: hidden;
}

.claido-heading::before {
  color: #ff004c;
  clip-path: polygon(0 30%, 100% 30%, 100% 50%, 0 50%);
  transform: translateX(-3px);
  animation: glitch-1 3.5s infinite;
  opacity: 0;
}

.claido-heading::after {
  color: #00f0ff;
  clip-path: polygon(0 55%, 100% 55%, 100% 75%, 0 75%);
  transform: translateX(3px);
  animation: glitch-2 3.5s infinite;
  opacity: 0;
}

@keyframes flicker {
  0%, 95%, 100% { opacity: 1; }
  96% { opacity: 0.4; }
  97% { opacity: 1; }
  98% { opacity: 0.2; }
  99% { opacity: 1; }
}

@keyframes glitch-1 {
  0%, 89%, 100% { opacity: 0; transform: translateX(0); }
  90% { opacity: 0.8; transform: translateX(-4px); }
  91% { opacity: 0; }
  92% { opacity: 0.6; transform: translateX(3px); }
  93% { opacity: 0; }
}

@keyframes glitch-2 {
  0%, 89%, 100% { opacity: 0; transform: translateX(0); }
  90% { opacity: 0; }
  91% { opacity: 0.7; transform: translateX(4px); }
  92% { opacity: 0; }
  93% { opacity: 0.5; transform: translateX(-2px); }
  94% { opacity: 0; }
}

.hero-subtitle {
  color: #888;
  font-size: 13px;
  letter-spacing: 2px;
  text-transform: uppercase;
  margin: 0;
}

/* ── Briefing card ────────────────────────────────────── */
.briefing-card {
  width: 100%;
  position: relative;
  background: #f5f0e8;
  border: 2px solid #d4c9b0;
  border-radius: 2px;
  box-shadow:
    0 4px 20px rgba(0, 0, 0, 0.6),
    inset 0 0 0 1px rgba(0,0,0,0.05);
  overflow: hidden;
}

/* Diagonal "TOP SECRET" watermark */
.watermark {
  position: absolute;
  top: 50%;
  left: 50%;
  transform: translate(-50%, -50%) rotate(-35deg);
  font-size: 72px;
  font-weight: 900;
  letter-spacing: 8px;
  color: rgba(200, 30, 30, 0.09);
  white-space: nowrap;
  pointer-events: none;
  user-select: none;
  font-family: 'Courier New', Courier, monospace;
}

.card-inner {
  position: relative;
  padding: 24px 28px 28px;
}

.card-header {
  margin-bottom: 14px;
  padding-bottom: 12px;
  border-bottom: 2px solid #c8b89a;
}

.card-title-block {
  display: flex;
  justify-content: space-between;
  align-items: baseline;
}

.card-stamp-label {
  font-size: 13px;
  font-weight: 700;
  letter-spacing: 2px;
  text-transform: uppercase;
  color: #2c1a0e;
}

.card-date {
  font-size: 12px;
  color: #7a6a55;
  font-family: 'Courier New', Courier, monospace;
}

.card-summary {
  color: #3a2e1e;
  font-size: 13px;
  line-height: 1.75;
  margin: 0 0 16px;
  font-family: Georgia, serif;
}

.room-list {
  list-style: none;
  margin: 0 0 20px;
  padding: 0;
  display: flex;
  flex-direction: column;
  gap: 7px;
}

.room-list li {
  display: flex;
  align-items: baseline;
  gap: 8px;
  font-size: 13px;
  color: #2c1a0e;
  font-family: 'Courier New', Courier, monospace;
}

.bullet {
  color: #a0522d;
  flex-shrink: 0;
}

.room-list strong {
  color: #1a0f00;
}

/* Red rubber "DECLASSIFIED" stamp in corner */
.declassified-stamp {
  position: absolute;
  bottom: 18px;
  right: 20px;
  border: 3px solid rgba(180, 20, 20, 0.75);
  color: rgba(180, 20, 20, 0.75);
  font-size: 16px;
  font-weight: 900;
  letter-spacing: 3px;
  text-transform: uppercase;
  padding: 4px 10px;
  border-radius: 3px;
  font-family: 'Courier New', Courier, monospace;
  transform: rotate(-8deg);
  pointer-events: none;
  user-select: none;
}

/* ── Error ────────────────────────────────────────────── */
.error-msg {
  width: 100%;
  background: rgba(200, 30, 30, 0.12);
  border: 1px solid #e53935;
  border-radius: 2px;
  color: #ff6b6b;
  padding: 12px 16px;
  font-size: 13px;
}

/* ── Start button ─────────────────────────────────────── */
.start-btn {
  width: 100%;
  padding: 16px 24px;
  font-size: 15px;
  font-weight: 700;
  letter-spacing: 3px;
  text-transform: uppercase;
  font-family: 'Courier New', Courier, monospace;
  color: #fff;
  background: #b71c1c;
  border: 2px solid #e53935;
  border-radius: 2px;
  cursor: pointer;
  position: relative;
  animation: pulse-glow 2.4s ease-in-out infinite;
  transition: background 0.15s, transform 0.1s;
}

.start-btn:hover:not(:disabled) {
  background: #c62828;
  transform: translateY(-1px);
}

.start-btn:active:not(:disabled) {
  transform: translateY(0);
}

.start-btn:disabled {
  opacity: 0.6;
  cursor: not-allowed;
  animation: none;
}

@keyframes pulse-glow {
  0%, 100% {
    box-shadow:
      0 0 8px rgba(229, 57, 53, 0.4),
      0 0 20px rgba(229, 57, 53, 0.2);
  }
  50% {
    box-shadow:
      0 0 16px rgba(229, 57, 53, 0.8),
      0 0 40px rgba(229, 57, 53, 0.4),
      0 0 60px rgba(229, 57, 53, 0.1);
  }
}

/* Spinner for loading state */
.spinner-dot {
  display: inline-block;
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: #fff;
  margin-right: 10px;
  vertical-align: middle;
  animation: spinner-pulse 0.8s ease-in-out infinite;
}

@keyframes spinner-pulse {
  0%, 100% { opacity: 1; transform: scale(1); }
  50% { opacity: 0.3; transform: scale(0.6); }
}

/* ── Disclaimer ───────────────────────────────────────── */
.disclaimer {
  color: #444;
  font-size: 11px;
  text-align: center;
  letter-spacing: 0.5px;
  margin: 0;
}
</style>
