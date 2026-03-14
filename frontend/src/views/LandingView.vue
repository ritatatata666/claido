<template>
  <div class="landing">
    <div class="scanlines"></div>

    <div class="landing-inner">
      <div class="landing-scroll">
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
      <div v-if="selectedMode === 'standard'" class="briefing-card">
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

      <div class="mode-card">
        <div class="mode-card__title">Mode Selection</div>
        <div class="mode-card__toggle">
          <button
            class="mode-card__tab mode-card__tab--standard"
            :class="{ active: selectedMode === 'standard' }"
            @click="selectedMode = 'standard'"
            type="button"
          >
            Standard Mode
          </button>
          <button
            class="mode-card__tab mode-card__tab--team"
            :class="{ active: selectedMode === 'team' }"
            @click="selectedMode = 'team'"
            type="button"
          >
            Team Mode
          </button>
        </div>
        <p class="mode-card__desc" v-if="selectedMode === 'standard'">
          Standard mode keeps the classic solo investigation. Grab your own workstation, decode the clues, and unlock the vault without worrying about other players.
        </p>
        <p class="mode-card__desc" v-else>
          Team Mode spins up a shared lobby. Create a room, let everyone join via the invite code, and let one villain try to sabotage while investigators work together.
        </p>

          <div v-if="selectedMode === 'team'" class="mode-card__team">
          <p class="mode-card__subheading">Team Mode requires a shared code</p>
          <p class="mode-card__team-text">
            Everyone who opens the link or enters the invite code is placed on a random faction—one scheming villain and a squad of investigators. Use the console inside the hub to see everyone in the session and how the sabotage tokens are spent.
          </p>
        </div>
      </div>

      <div v-if="selectedMode === 'team'" class="team-lobby-grid">
        <section class="team-lobby-card team-lobby-card--create">
          <div class="team-lobby-card__inner">
            <p class="team-lobby-card__eyebrow">Team mode — Lobby ready</p>
            <h3 class="team-lobby-card__title">Spin up a Kahoot-style room</h3>
            <p class="team-lobby-card__body">
              Tap “Create Team Room” to mint a short invite code. Share the code, let everyone join from their own device, and the roles lock in automatically—one villain, the rest investigators.
            </p>
            <ul class="team-lobby-card__list">
              <li>Host creates the room and keeps this panel open for quick sharing.</li>
              <li>Team members paste the code on the right, pick an alias, and instantly show up in the roster.</li>
              <li>Tokens and clues stay synced across machines so sabotage feels tense.</li>
            </ul>
            <div class="team-lobby-card__actions">
              <button
                class="team-lobby-card__button"
                :disabled="loading"
                @click="createTeamRoom"
              >
                <span v-if="loading">
                  <span class="spinner-dot"></span>
                  Creating team room...
                </span>
                <span v-else>CREATE TEAM ROOM</span>
              </button>
              <p class="team-lobby-card__hint">Need a room? This button generates the code, registers the host, and keeps the door open for teammates.</p>
              <p v-if="error" class="team-lobby-card__error">{{ error }}</p>
            </div>
          </div>
        </section>

        <section class="join-card">
          <div class="join-card__row">
            <label class="join-card__label">Join code</label>
            <input
              v-model="joinCodeInput"
              class="join-card__input"
              type="text"
              placeholder="Enter code (e.g. NOVA12)"
            />
          </div>
          <div class="join-card__row">
            <label class="join-card__label">Nickname (optional)</label>
            <input
              v-model="joinName"
              class="join-card__input"
              type="text"
              placeholder="Agent alias"
            />
          </div>
          <button
            class="join-card__button"
            :disabled="joinLoading || !joinCodeInput.trim()"
            @click="joinExistingSession"
          >
            <span v-if="joinLoading">Joining…</span>
            <span v-else>Join existing session</span>
          </button>
          <p v-if="joinError" class="join-card__error">{{ joinError }}</p>
        </section>
      </div>

      <div v-if="selectedMode === 'standard'" class="cta-section">
        <div v-if="error" class="error-msg">{{ error }}</div>
        <button
          class="start-btn"
          :disabled="loading"
          @click="startSoloSession"
        >
          <span v-if="loading">
            <span class="spinner-dot"></span>
            Generating case file...
          </span>
          <span v-else>BEGIN SOLO INVESTIGATION</span>
        </button>
      </div>

      <p class="disclaimer">Session expires when tab closes. Each case is AI-generated.</p>
      </div>
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
const joinCodeInput = ref('')
const joinName = ref('')
const joinLoading = ref(false)
const joinError = ref('')
const selectedMode = ref('standard')

const rooms = [
  { id: 'shell', label: 'NovaShell', desc: 'Explore the internal filesystem' },
  { id: 'database', label: 'NovaCrime DB', desc: 'Query employee and access records' },
  { id: 'mail', label: 'NovaMail', desc: 'Read intercepted corporate emails' },
  { id: 'wiki', label: 'NovaWiki', desc: 'Browse classified internal documents' },
  { id: 'search', label: 'NovaSearch', desc: 'Analyse 50,000 system log entries' },
  { id: 'onion', label: 'The Onion', desc: 'Browse the dark web for leads' },
  { id: 'vault', label: 'Vault', desc: 'Enter the four-word passphrase to win' },
]

async function startSoloSession() {
  loading.value = true
  error.value = ''
  try {
    store.configureTeamMode('standard')
    await store.createSession()
    router.push('/hub')
  } catch (e) {
    error.value = e.message || 'Failed to connect to backend. Is it running?'
  } finally {
    loading.value = false
  }
}

async function joinExistingSession() {
  if (!joinCodeInput.value.trim()) {
    joinError.value = 'Enter a join code first.'
    return
  }
  joinLoading.value = true
  joinError.value = ''
  try {
    store.configureTeamMode('team')
    await store.joinTeamSession(joinCodeInput.value.trim(), joinName.value.trim() || 'Investigator')
    router.push('/hub')
  } catch (e) {
    joinError.value = e.message || 'Could not join that session.'
  } finally {
    joinLoading.value = false
  }
}

async function createTeamRoom() {
  loading.value = true
  error.value = ''
  joinError.value = ''
  try {
    store.configureTeamMode('team')
    await store.createTeamRoom('Host Investigator')
    router.push('/hub')
  } catch (e) {
    error.value = e.message || 'Failed to create a team room. Is the backend running?'
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
/* ── Base ─────────────────────────────────────────────── */
.landing {
  min-height: 100vh;
  max-height: 100vh;
  background: #0a0a0a;
  display: flex;
  align-items: flex-start;
  justify-content: center;
  padding: 40px 24px 60px;
  overflow-y: overlay;
  scrollbar-gutter: stable;
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
  max-height: calc(100vh - 48px);
}

.landing-scroll {
  width: 100%;
  overflow-y: auto;
  padding-right: 12px;
  display: flex;
  flex-direction: column;
  gap: 28px;
  box-sizing: border-box;
  scrollbar-gutter: stable;
}

.landing-scroll > * {
  flex: 0 0 auto;
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

.mode-card {
  width: 100%;
  background: rgba(6, 6, 6, 0.78);
  border: 1px solid rgba(255, 255, 255, 0.08);
  border-radius: 16px;
  padding: 20px 24px;
  display: flex;
  flex-direction: column;
  gap: 14px;
  box-shadow: 0 20px 50px rgba(0, 0, 0, 0.65);
}

.mode-card__title {
  margin: 0;
  font-size: 13px;
  letter-spacing: 1.2px;
  text-transform: uppercase;
  color: #9999a3;
}

.mode-card__toggle {
  display: flex;
  gap: 12px;
}

.mode-card__tab {
  flex: 1;
  border: 1px solid rgba(255, 255, 255, 0.08);
  border-radius: 14px;
  background: rgba(8, 10, 20, 0.95);
  color: rgba(255, 255, 255, 0.85);
  font-size: 13px;
  padding: 12px 16px;
  cursor: pointer;
  font-weight: 700;
  letter-spacing: 1px;
  text-transform: uppercase;
  transition: transform 0.2s ease, box-shadow 0.2s ease, background 0.3s ease;
  box-shadow: inset 0 -2px 0 rgba(255, 255, 255, 0.08);
}

.mode-card__tab--standard {
  background: linear-gradient(135deg, rgba(24, 10, 37, 0.9), rgba(39, 18, 64, 0.9));
}

.mode-card__tab--team {
  background: linear-gradient(135deg, rgba(7, 8, 24, 0.95), rgba(101, 24, 120, 0.9));
}

.mode-card__tab:not(.active) {
  opacity: 0.75;
}

.mode-card__tab.active {
  border-color: #c48bff;
  box-shadow: 0 12px 28px rgba(196, 139, 255, 0.35), inset 0 1px 0 rgba(255, 255, 255, 0.3);
  transform: translateY(-2px);
  color: #fff;
}

.mode-card__desc {
  margin: 0;
  font-size: 13px;
  color: #c3c3cd;
}

.mode-card__team {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.mode-card__subheading {
  margin: 0;
  font-size: 11px;
  letter-spacing: 1px;
  text-transform: uppercase;
  color: #8b8b99;
}

.mode-card__team-option {
  border: 1px solid rgba(255, 255, 255, 0.08);
  border-radius: 12px;
  padding: 12px;
  display: flex;
  gap: 10px;
  align-items: center;
  cursor: pointer;
  color: #f5f5f7;
  font-size: 13px;
}

.mode-card__team-option input {
  margin: 0;
}

.mode-card__team-option strong {
  font-size: 14px;
}

.mode-card__team-option span {
  display: block;
  font-size: 12px;
  color: #aaaab5;
}

.mode-card__team-text {
  margin: 0;
  font-size: 13px;
  color: #c0c0c9;
  line-height: 1.4;
}

.team-lobby-grid {
  width: 100%;
  display: grid;
  gap: 18px;
}

@media (min-width: 780px) {
  .team-lobby-grid {
    grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
    align-items: stretch;
  }
}

.team-lobby-card {
  width: 100%;
  background: linear-gradient(135deg, rgba(14, 15, 30, 0.95), rgba(48, 22, 63, 0.95));
  border: 1px solid rgba(255, 255, 255, 0.1);
  border-radius: 18px;
  padding: 20px;
  box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.08), 0 18px 40px rgba(0, 0, 0, 0.6);
  min-height: 280px;
}

.team-lobby-card__inner {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.team-lobby-card__actions {
  margin-top: 12px;
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.team-lobby-card__button {
  border: none;
  border-radius: 12px;
  padding: 14px;
  background: linear-gradient(135deg, #8c3bff, #d22766);
  color: #fff;
  font-weight: 700;
  cursor: pointer;
  text-transform: uppercase;
  font-size: 13px;
  letter-spacing: 1px;
  transition: transform 0.2s ease, box-shadow 0.2s ease;
}

.team-lobby-card__button:disabled {
  opacity: 0.5;
  cursor: not-allowed;
  box-shadow: none;
  transform: none;
}

.team-lobby-card__button:not(:disabled):hover {
  transform: translateY(-1px);
  box-shadow: 0 10px 24px rgba(210, 39, 102, 0.4);
}

.team-lobby-card__hint {
  font-size: 12px;
  color: #b4b1c7;
}

.team-lobby-card__error {
  font-size: 12px;
  color: #ff8b8b;
}

.team-lobby-card__eyebrow {
  font-size: 11px;
  letter-spacing: 1.5px;
  text-transform: uppercase;
  color: rgba(208, 183, 255, 0.9);
}

.team-lobby-card__title {
  margin: 0;
  font-size: 18px;
  letter-spacing: 1px;
  color: #f5f5ff;
}

.team-lobby-card__body {
  margin: 0;
  font-size: 13px;
  color: rgba(220, 220, 255, 0.9);
  line-height: 1.5;
}

.team-lobby-card__list {
  margin: 0;
  padding-left: 18px;
  display: flex;
  flex-direction: column;
  gap: 6px;
  color: rgba(195, 195, 255, 0.9);
}

.team-lobby-card__list li {
  font-size: 13px;
  line-height: 1.4;
}

.join-card {
  width: 100%;
  background: rgba(4, 4, 6, 0.85);
  border: 1px solid rgba(255, 255, 255, 0.08);
  border-radius: 16px;
  padding: 18px 22px;
  display: flex;
  flex-direction: column;
  gap: 12px;
  min-height: 280px;
}

.join-card__row {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.join-card__label {
  font-size: 11px;
  text-transform: uppercase;
  letter-spacing: 1px;
  color: #8d8d95;
}

.join-card__input {
  background: #0b0b11;
  border: 1px solid rgba(255, 255, 255, 0.08);
  border-radius: 10px;
  padding: 10px 12px;
  color: #fff;
  font-size: 13px;
}

.join-card__button {
  border: none;
  border-radius: 12px;
  padding: 12px;
  background: linear-gradient(135deg, #ff6b80, #ffb347);
  color: #0b0b0f;
  font-weight: 700;
  cursor: pointer;
  transition: transform 0.2s ease;
}

.join-card__button:disabled {
  opacity: 0.6;
  cursor: not-allowed;
  transform: none;
}

.join-card__error {
  color: #ff7b7b;
  font-size: 12px;
  margin: 0;
}
</style>
