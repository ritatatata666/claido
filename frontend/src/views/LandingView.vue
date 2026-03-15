<template>
  <div class="landing">
    <button class="window-corner-btn" @click="router.push('/history')">Recent Cases</button>
    <div class="landing-board">

      <div class="top-bar evidence-strip">
        <span class="classified-badge">● Active Case</span>
        <div class="top-right">
          <span class="case-file">CASE FILE #NC-2025-0303</span>
        <button v-if="store.sessionId" class="top-report-btn" @click="router.push('/report')">📋 Case Report</button>
          <span>Signed in as <strong>{{ auth.user?.username }}</strong></span>
          <button class="top-right__logout" @click="logout">Logout</button>
        </div>
      </div>

      <!-- Stamp heading -->
      <div class="hero-block">
        <div class="stamp-frame">
          <h1 class="claido-heading" data-text="CLAIDO">CLAIDO</h1>
        </div>
        <div class="case-header">
          <span class="case-header__label">NovaCorp Internal Breach</span>
          <span class="case-header__divider">—</span>
          <span class="case-header__tag">Investigator Access Only</span>
        </div>
      </div>

      <!-- Briefing card styled as manila case folder -->
      <div class="folder-wrapper">
        <button
          class="folder-tab folder-tab--toggle"
          :class="{ 'is-open': briefingOpen }"
          type="button"
          :aria-expanded="briefingOpen ? 'true' : 'false'"
          @click="briefingOpen = !briefingOpen"
        >
          <span class="folder-tab__title">CASE REPORT</span>
          <span class="folder-tab__state">{{ briefingOpen ? 'CLOSE FILE' : 'REVEAL FILE' }}</span>
        </button>
        <div class="briefing-card" :class="{ 'is-revealed': briefingOpen }">
          <transition name="file-reveal" mode="out-in">
            <div v-if="briefingOpen" key="open" class="briefing-card__content">
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
            <div v-else key="closed" class="briefing-card__blank" aria-hidden="true">
              <span class="briefing-card__blank-label">SEALED CASE FILE</span>
            </div>
          </transition>
        </div>
      </div>

      <!-- Error -->
      <div v-if="error" class="error-msg">{{ error }}</div>

      <!-- Mode Selection -->
      <div class="mode-card">
        <h3 class="mode-card__title">Select Mode</h3>
        <div class="mode-card__toggle">
          <button
            :class="['mode-card__tab', 'mode-card__tab--standard', { active: selectedMode === 'standard' }]"
            @click="selectedMode = 'standard'"
          >Solo</button>
          <button
            :class="['mode-card__tab', 'mode-card__tab--team', { active: selectedMode === 'team' }]"
            @click="selectedMode = 'team'"
          >Team</button>
        </div>

        <!-- Solo mode -->
        <template v-if="selectedMode === 'standard'">
          <p class="mode-card__desc">Investigate alone. All seven rooms are yours to explore.</p>
          <input v-model="soloName" class="npc-input" placeholder="Investigator name" />
          <button
            class="start-btn"
            :disabled="loading || !soloName.trim()"
            @click="startSoloSession"
          >
            <span v-if="loading">
              <span class="spinner-dot"></span>
              Generating case file...
            </span>
            <span v-else>BEGIN INVESTIGATION</span>
          </button>
        </template>

        <!-- Team mode -->
        <template v-else>
          <p class="mode-card__desc">Play with friends. One hosts, others join with a code.</p>
          <div class="team-lobby-grid">
            <!-- Create -->
            <div class="team-lobby-card">
              <div class="team-lobby-card__inner">
                <span class="team-lobby-card__eyebrow">Host a Room</span>
                <p class="mode-card__team-text">Create a new team session and share the join code with your crew.</p>
                <div class="team-lobby-card__actions">
                  <input v-model="hostName" class="npc-input" placeholder="Investigator name" />
                  <select v-model="hostRole" class="npc-input">
                    <option value="investigator">Investigator</option>
                    <option value="villain">Villain</option>
                  </select>
                  <button class="team-lobby-card__button" :disabled="loading || !hostName.trim()" @click="createTeamRoom">
                    <span v-if="loading"><span class="spinner-dot"></span> Creating...</span>
                    <span v-else>Create Team Room</span>
                  </button>
                </div>
              </div>
            </div>
            <!-- Join -->
            <div class="team-lobby-card">
              <div class="team-lobby-card__inner">
                <span class="team-lobby-card__eyebrow">Join a Room</span>
                <p class="mode-card__team-text">Enter the 6-character code from your host to join their session.</p>
                <div class="team-lobby-card__actions">
                  <input v-model="joinCodeInput" class="npc-input" placeholder="Join code (e.g. ABC123)" maxlength="6" style="text-transform:uppercase" />
                  <input v-model="joinName" class="npc-input" placeholder="Investigator name" />
                  <select v-model="joinRole" class="npc-input">
                    <option value="investigator">Investigator</option>
                    <option value="villain">Villain</option>
                  </select>
                  <button class="team-lobby-card__button" :disabled="joinLoading || !joinCodeInput.trim() || !joinName.trim()" @click="joinExistingSession">
                    <span v-if="joinLoading"><span class="spinner-dot"></span> Joining...</span>
                    <span v-else>Join Session</span>
                  </button>
                  <p v-if="joinError" class="team-lobby-card__error">{{ joinError }}</p>
                </div>
              </div>
            </div>
          </div>
        </template>
      </div>

      <section class="leaderboard-card">
        <div class="leaderboard-card__header">
          <span class="leaderboard-card__eyebrow">Fastest Investigators</span>
          <h3 class="leaderboard-card__title">Top 5 Leaderboard</h3>
        </div>
        <div v-if="leaderboardError" class="leaderboard-card__error">{{ leaderboardError }}</div>
        <div v-else-if="store.leaderboard.length === 0" class="leaderboard-card__empty">
          No completed cases yet. First solved run sets the pace.
        </div>
        <div v-else class="leaderboard-list">
          <div
            v-for="(entry, index) in store.leaderboard"
            :key="`${entry.displayName}-${entry.solveSeconds}-${index}`"
            class="leaderboard-row"
          >
            <span class="leaderboard-rank">#{{ index + 1 }}</span>
            <span class="leaderboard-name">{{ entry.displayName }}</span>
            <span class="leaderboard-time">{{ formatDuration(entry.solveSeconds) }}</span>
          </div>
        </div>
      </section>

      <p class="disclaimer">Session expires when tab closes. Each case is AI-generated.</p>
      <img :src="cautionTapeImg" alt="" class="landing-caution-tape" aria-hidden="true" />

    </div>
  </div>
</template>

<script setup>
import { onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useGameStore } from '../stores/gameStore.js'
import cautionTapeImg from '../../images/caution.webp'
import { useAuthStore } from '../stores/authStore.js'

const router = useRouter()
const store = useGameStore()
const auth = useAuthStore()
const loading = ref(false)
const error = ref('')
const soloName = ref('')
const hostName = ref('')
const hostRole = ref('investigator')
const joinCodeInput = ref('')
const joinName = ref('')
const joinRole = ref('investigator')
const joinLoading = ref(false)
const joinError = ref('')
const selectedMode = ref('standard')
const leaderboardError = ref('')
const briefingOpen = ref(false)

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
  const name = soloName.value.trim()
  if (!name) {
    error.value = 'Enter investigator name.'
    return
  }
  loading.value = true
  error.value = ''
  try {
    store.configureTeamMode('standard')
    await store.createSession(name)
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
  const name = joinName.value.trim()
  if (!name) {
    joinError.value = 'Enter investigator name.'
    return
  }
  joinLoading.value = true
  joinError.value = ''
  try {
    store.configureTeamMode('team')
    await store.joinTeamSession(
      joinCodeInput.value.trim(),
      name,
      joinRole.value
    )
    router.push('/hub')
  } catch (e) {
    joinError.value = e.message || 'Could not join that session.'
  } finally {
    joinLoading.value = false
  }
}

async function createTeamRoom() {
  const name = hostName.value.trim()
  if (!name) {
    error.value = 'Enter investigator name.'
    return
  }
  loading.value = true
  error.value = ''
  joinError.value = ''
  try {
    store.configureTeamMode('team')
    await store.createTeamRoom(
      name,
      hostRole.value
    )
    router.push('/hub')
  } catch (e) {
    error.value = e.message || 'Failed to create a team room. Is the backend running?'
  } finally {
    loading.value = false
  }
}

function formatDuration(totalSeconds) {
  const safe = Math.max(0, Number(totalSeconds) || 0)
  const minutes = Math.floor(safe / 60)
  const seconds = safe % 60
  if (minutes >= 60) {
    const hours = Math.floor(minutes / 60)
    const remMinutes = minutes % 60
    return `${hours}:${String(remMinutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`
  }
  return `${String(minutes).padStart(2, '0')}:${String(seconds).padStart(2, '0')}`
}

onMounted(async () => {
  leaderboardError.value = ''
  try {
    await store.fetchLeaderboard()
  } catch (e) {
    leaderboardError.value = e.message || 'Could not load leaderboard.'
  }
})

async function logout() {
  await auth.logout()
  store.resetState()
  router.push('/login')
}
</script>

<style scoped>
.landing {
  min-height: 100vh;
  display: flex;
  justify-content: center;
  padding: 40px 24px 60px;
  overflow-y: auto;
  position: relative;
  flex-direction: column;
  align-items: center;
  gap: 24px;
  background:
    repeating-linear-gradient(
      0deg,
      rgba(255, 255, 255, 0.02) 0px,
      rgba(255, 255, 255, 0.02) 1px,
      transparent 1px,
      transparent 8px
    ),
    repeating-linear-gradient(
      90deg,
      rgba(255, 255, 255, 0.01) 0px,
      rgba(255, 255, 255, 0.01) 1px,
      transparent 1px,
      transparent 8px
    ),
    linear-gradient(135deg, #392317 0%, #2b1a12 35%, #1f130d 70%, #140b08 100%);
}

.window-corner-btn {
  position: fixed;
  top: 16px;
  left: 18px;
  z-index: 20;
  border: 1px solid rgba(255, 240, 220, 0.4);
  border-radius: 999px;
  background: rgba(0, 0, 0, 0.25);
  color: rgba(255, 240, 220, 0.95);
  font-family: var(--font-mono);
  font-size: 12px;
  text-transform: uppercase;
  letter-spacing: 1px;
  min-height: 42px;
  padding: 0 18px;
}

.window-corner-btn {
  position: fixed;
  top: 16px;
  left: 18px;
  z-index: 20;
  border: 1px solid rgba(255, 240, 220, 0.4);
  border-radius: 999px;
  background: rgba(0, 0, 0, 0.25);
  color: rgba(255, 240, 220, 0.95);
  font-family: var(--font-mono);
  font-size: 12px;
  text-transform: uppercase;
  letter-spacing: 1px;
  min-height: 42px;
  padding: 0 18px;
}

.landing-board {
  width: 100%;
  max-width: 800px;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 28px;
  position: relative;
}

.landing-caution-tape {
  position: absolute;
  right: -18px;
  bottom: 36px;
  width: 190px;
  opacity: 0.72;
  transform: rotate(-13deg);
  pointer-events: none;
  z-index: 2;
  filter: drop-shadow(0 8px 16px rgba(0, 0, 0, 0.42));
}

.board-thread {
  display: none;
}

.evidence-strip,
.evidence-card {
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

.top-right {
  display: flex;
  align-items: center;
  gap: 12px;
  color: rgba(255, 240, 220, 0.9);
  font-family: var(--font-mono);
  font-size: 12px;
}

.top-right__logout {
  border: 1px solid rgba(255, 240, 220, 0.35);
  background: rgba(0, 0, 0, 0.25);
  color: rgba(255, 240, 220, 0.95);
  border-radius: 6px;
  padding: 6px 10px;
  cursor: pointer;
}

.evidence-card {
  border-radius: 8px;
  overflow: hidden;
}

.evidence-card--wide {
  display: flex;
  justify-content: space-between;
  align-items: flex-end;
  gap: 24px;
  padding: 28px 32px;
}

.note-pin {
  position: absolute;
  top: -8px;
  left: 24px;
  width: 16px;
  height: 16px;
  border-radius: 50%;
  box-shadow: 0 2px 6px rgba(0, 0, 0, 0.24), inset 0 1px 1px rgba(255, 255, 255, 0.68);
}

.note-pin--red { background: #ca4b42; }
.note-pin--blue { background: #4d78a5; }
.note-pin--gold { background: #bc8c2c; }

.top-bar {
  z-index: 1;
}

.classified-badge,
.case-file,
.hero-kicker,
.hero-tag,
.card-stamp-label,
.card-date,
.snapshot-label,
.start-label,
.disclaimer {
  font-family: var(--font-mono);
}

.classified-badge {
  color: var(--accent-red);
  font-size: 13px;
  font-weight: 700;
  letter-spacing: 2px;
  text-transform: uppercase;
}

.top-report-btn {
  padding: 5px 12px;
  background: rgba(200, 169, 122, 0.2);
  border: 1px solid rgba(139, 100, 60, 0.45);
  border-radius: 4px;
  color: rgba(255, 220, 180, 0.85);
  font-family: var(--font-mono);
  font-size: 11px;
  letter-spacing: 1.5px;
  text-transform: uppercase;
  cursor: pointer;
  transition: background 0.15s, border-color 0.15s;
}

.top-report-btn:hover {
  background: rgba(200, 169, 122, 0.35);
  border-color: rgba(139, 100, 60, 0.7);
}

.case-file {
  color: rgba(255, 240, 220, 0.5);
  font-size: 12px;
  letter-spacing: 1.2px;
}

.hero-copy {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.hero-kicker {
  font-size: 11px;
  letter-spacing: 3px;
  text-transform: uppercase;
  color: var(--text-muted);
}

.hero-block {
  text-align: center;
  display: flex;
  flex-direction: column;
  align-items: center;
  gap: 12px;
}

.stamp-frame {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  padding: 12px 32px;
  border: 4px solid rgba(183, 11, 11, 0.95);
  border-radius: 6px;
  transform: rotate(-3deg);
  position: relative;
  cursor: default;
  transition: border-color 0.15s, box-shadow 0.15s;
}

.stamp-frame::before {
  content: '';
  position: absolute;
  inset: 3px;
  border: 1.5px solid rgba(183, 11, 11, 0.3);
  border-radius: 3px;
  pointer-events: none;
}

.stamp-frame:hover {
  border-color: rgba(183, 11, 11, 0.95);
  box-shadow:
    0 0 12px rgba(160, 26, 26, 0.5),
    0 0 30px rgba(160, 26, 26, 0.25);
}

.stamp-frame:hover::before {
  border-color: rgba(160, 26, 26, 0.6);
}

.claido-heading {
  margin: 0;
  font-size: clamp(48px, 10vw, 90px);
  letter-spacing: 14px;
  text-transform: uppercase;
  font-family: var(--font-display);
  color: rgba(183, 11, 11, 0.95);
  transition: color 0.1s, text-shadow 0.1s;
}

.stamp-frame:hover .claido-heading {
  color: #a01a1a;
  text-shadow:
    0 0 8px rgba(160, 26, 26, 0.9),
    0 0 20px rgba(160, 26, 26, 0.7),
    0 0 40px rgba(160, 26, 26, 0.5);
  animation: red-flicker 0.15s steps(2) infinite;
}

@keyframes red-flicker {
  0% { opacity: 1; }
  50% { opacity: 0.85; }
  100% { opacity: 1; }
}

.case-header {
  display: flex;
  align-items: center;
  gap: 10px;
  flex-wrap: wrap;
  justify-content: center;
}

.case-header__label {
  font-family: var(--font-mono);
  font-size: clamp(13px, 2vw, 16px);
  font-weight: 700;
  letter-spacing: 2.5px;
  text-transform: uppercase;
  color: rgba(255, 220, 180, 0.7);
}

.case-header__divider {
  color: rgba(255, 220, 180, 0.3);
  font-size: 16px;
}

.case-header__tag {
  font-family: var(--font-mono);
  font-size: clamp(11px, 1.5vw, 13px);
  letter-spacing: 2px;
  text-transform: uppercase;
  color: rgba(220, 180, 140, 0.85);
  border: 1px solid rgba(220, 180, 140, 0.4);
  padding: 3px 10px;
  border-radius: 2px;
}

.hero-tag {
  align-self: flex-start;
  padding: 10px 14px;
  border: 2px solid rgba(185, 70, 54, 0.6);
  color: rgba(185, 70, 54, 0.82);
  text-transform: uppercase;
  letter-spacing: 2px;
  font-size: 12px;
  font-weight: 800;
  transform: rotate(-6deg);
}

.briefing-grid {
  display: grid;
  grid-template-columns: minmax(0, 1.45fr) minmax(280px, 0.8fr);
  gap: 24px;
  align-items: start;
}

.folder-wrapper {
  width: 100%;
  position: relative;
}

.folder-tab {
  position: relative;
  display: inline-flex;
  align-items: center;
  gap: 14px;
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

.folder-tab--toggle {
  border: 1px solid #6a4b33;
  border-bottom: none;
  cursor: pointer;
  transition: transform 0.2s ease, box-shadow 0.2s ease, filter 0.2s ease;
}

.folder-tab--toggle:hover {
  transform: translateY(-1px);
  filter: brightness(1.04);
}

.folder-tab--toggle.is-open {
  box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.24), 0 -3px 8px rgba(0, 0, 0, 0.16);
}

.folder-tab__title {
  letter-spacing: 2px;
}

.folder-tab__state {
  font-size: 9px;
  letter-spacing: 1.3px;
  color: rgba(49, 26, 12, 0.92);
  border-left: 1px solid rgba(90, 61, 36, 0.35);
  padding-left: 10px;
}

.folder-tab--toggle.is-open .folder-tab__state {
  color: #1f0e06;
  font-weight: 800;
}

.file-reveal-enter-active,
.file-reveal-leave-active {
  transition: opacity 0.26s ease, transform 0.28s ease, filter 0.28s ease;
}

.file-reveal-enter-from,
.file-reveal-leave-to {
  opacity: 0;
  transform: translateY(-8px) scale(0.985);
  filter: blur(0.5px);
}

.file-reveal-enter-to,
.file-reveal-leave-from {
  opacity: 1;
  transform: translateY(0) scale(1);
  filter: blur(0);
}

.briefing-card {
  width: 100%;
  position: relative;
  background:
    repeating-linear-gradient(
      180deg,
      transparent 0 28px,
      rgba(92, 67, 47, 0.1) 28px 29px
    ),
    linear-gradient(180deg, #c0ab87, #a68c67);
  border: 1px solid #6a4b33;
  border-radius: 0 6px 6px 6px;
  box-shadow:
    0 12px 30px rgba(18, 10, 6, 0.42),
    inset 0 1px 0 rgba(255, 255, 255, 0.12),
    inset 0 -1px 0 rgba(0, 0, 0, 0.18);
  min-height: 250px;
}

.briefing-card__content {
  position: relative;
}

.briefing-card__blank {
  min-height: 250px;
  display: grid;
  place-items: center;
}

.briefing-card__blank-label {
  font-family: var(--font-mono);
  font-size: 12px;
  letter-spacing: 2.2px;
  text-transform: uppercase;
  color: rgba(68, 46, 29, 0.6);
  border: 1px dashed rgba(68, 46, 29, 0.45);
  padding: 8px 12px;
  transform: rotate(-2deg);
}

.briefing-card::before,
.mode-card::before {
  content: '';
  position: absolute;
  inset: 0;
  background:
    radial-gradient(circle at 12% 10%, rgba(140, 22, 22, 0.09), transparent 34%),
    radial-gradient(circle at 80% 82%, rgba(0, 0, 0, 0.14), transparent 40%);
  pointer-events: none;
}

.briefing-card::after,
.snapshot-card::after,
.start-card::after {
  content: '';
  position: absolute;
  inset: 0;
  background: repeating-linear-gradient(180deg, transparent 0 24px, rgba(121, 92, 67, 0.08) 24px 25px);
  pointer-events: none;
}

.watermark {
  position: absolute;
  top: 48%;
  left: 52%;
  transform: translate(-50%, -50%) rotate(-24deg);
  font-size: clamp(44px, 9vw, 88px);
  letter-spacing: 6px;
  color: rgba(139, 69, 19, 0.1);
  font-weight: 900;
  pointer-events: none;
}

.card-inner {
  position: relative;
  z-index: 1;
  padding: 32px 32px 36px;
}

.card-header {
  margin-bottom: 16px;
  padding-bottom: 12px;
  border-bottom: 2px solid rgba(139, 100, 60, 0.3);
}

.card-title-block {
  display: flex;
  justify-content: space-between;
  align-items: baseline;
  gap: 12px;
}

.card-stamp-label {
  font-size: 14px;
  font-weight: 700;
  letter-spacing: 2px;
  text-transform: uppercase;
  color: #4c3624;
}

.card-date {
  font-size: 13px;
  color: #725b46;
}

.card-summary {
  margin: 0 0 20px;
  color: #4e3a2b;
  line-height: 1.8;
  font-size: 16px;
}

.room-list {
  list-style: none;
  display: grid;
  gap: 10px;
  margin: 0;
  padding: 0;
}

.room-list li {
  display: grid;
  grid-template-columns: auto auto 1fr;
  gap: 10px;
  align-items: baseline;
  color: #7a5c3a;
  font-size: 15px;
}

.bullet {
  color: #a0553a;
}

.room-list strong {
  color: #5a3d24;
}

.declassified-stamp {
  display: inline-flex;
  margin-top: 22px;
  padding: 5px 12px;
  border: 2px solid rgba(185, 70, 54, 0.65);
  color: rgba(185, 70, 54, 0.82);
  font-family: var(--font-mono);
  font-size: 12px;
  font-weight: 800;
  letter-spacing: 2px;
  text-transform: uppercase;
  transform: rotate(-5deg);
}

.side-column {
  display: grid;
  gap: 20px;
}

.snapshot-card,
.start-card {
  padding: 22px 20px 20px;
}

.snapshot-label,
.start-label {
  position: relative;
  z-index: 1;
  font-size: 11px;
  letter-spacing: 2px;
  text-transform: uppercase;
  color: var(--accent-red);
  margin: 0 0 12px;
}

.snapshot-list {
  position: relative;
  z-index: 1;
  margin: 0;
  padding-left: 18px;
  display: grid;
  gap: 8px;
  color: var(--text-secondary);
}

.start-btn {
  width: 100%;
  padding: 18px 24px;
  text-transform: uppercase;
  letter-spacing: 3px;
  font-family: var(--font-mono);
  font-weight: 800;
  font-size: 15px;
}

.spinner-dot {
  display: inline-block;
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: #fffaf0;
  margin-right: 10px;
  vertical-align: middle;
  animation: spinner-pulse 0.8s ease-in-out infinite;
}

@keyframes spinner-pulse {
  0%, 100% { opacity: 1; transform: scale(1); }
  50% { opacity: 0.3; transform: scale(0.6); }
}

.disclaimer {
  position: relative;
  z-index: 1;
  margin: 14px 0 0;
  color: var(--text-muted);
  font-size: 11px;
  line-height: 1.6;
}

.error-msg {
  padding: 14px 16px;
  color: #8f2018;
  border: 1px solid rgba(143, 32, 24, 0.3);
  background: rgba(185, 70, 54, 0.08);
  border-radius: 6px;
}

@media (max-width: 860px) {
  .briefing-grid {
    grid-template-columns: 1fr;
  }

  .evidence-card--wide {
    flex-direction: column;
    align-items: flex-start;
  }
}

@media (max-width: 640px) {
  .landing {
    padding: 64px 14px 40px;
  }

  .window-corner-btn {
    top: 10px;
    left: 10px;
    min-height: 38px;
    padding: 0 14px;
    font-size: 11px;
  }

  .landing-caution-tape {
    width: 140px;
    right: -8px;
    bottom: 22px;
    opacity: 0.64;
  }

  .landing-caution-tape {
    width: 140px;
    right: -8px;
    bottom: 22px;
    opacity: 0.64;
  }

  .top-bar {
    flex-direction: column;
    align-items: flex-start;
    gap: 6px;
  }

  .top-right {
    flex-wrap: wrap;
  }

  .card-title-block,
  .room-list li {
    grid-template-columns: 1fr;
    display: grid;
  }

}

.mode-card {
  width: 100%;
  position: relative;
  background:
    repeating-linear-gradient(
      180deg,
      transparent 0 28px,
      rgba(92, 67, 47, 0.1) 28px 29px
    ),
    linear-gradient(180deg, #c0ab87, #a68c67);
  border: 1px solid #6a4b33;
  border-radius: 6px;
  padding: 24px 28px;
  display: flex;
  flex-direction: column;
  gap: 16px;
  box-shadow:
    0 12px 30px rgba(18, 10, 6, 0.42),
    inset 0 1px 0 rgba(255, 255, 255, 0.12),
    inset 0 -1px 0 rgba(0, 0, 0, 0.18);
}

.leaderboard-card {
  width: 100%;
  position: relative;
  background:
    repeating-linear-gradient(
      180deg,
      transparent 0 28px,
      rgba(160, 130, 95, 0.06) 28px 29px
    ),
    linear-gradient(180deg, #d8bea0, #ccb084);
  border: 1px solid #a88b62;
  border-radius: 6px;
  padding: 24px 28px;
  box-shadow:
    0 8px 24px rgba(80, 50, 20, 0.2),
    inset 0 1px 0 rgba(255, 255, 255, 0.15);
}

.leaderboard-card__header {
  display: flex;
  flex-direction: column;
  gap: 6px;
  margin-bottom: 16px;
}

.leaderboard-card__eyebrow {
  font-family: var(--font-mono);
  font-size: 11px;
  letter-spacing: 2px;
  text-transform: uppercase;
  color: #8b3a2a;
}

.leaderboard-card__title {
  margin: 0;
  font-size: 20px;
  letter-spacing: 1px;
  color: #5a3d24;
}

.leaderboard-list {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.leaderboard-row {
  display: grid;
  grid-template-columns: 52px 1fr auto;
  gap: 12px;
  align-items: center;
  padding: 12px 14px;
  border: 1px solid rgba(139, 100, 60, 0.18);
  border-radius: 6px;
  background: rgba(255, 248, 236, 0.45);
}

.leaderboard-rank,
.leaderboard-time,
.leaderboard-name {
  font-family: var(--font-mono);
}

.leaderboard-rank {
  font-size: 12px;
  font-weight: 700;
  color: #8b3a2a;
}

.leaderboard-name {
  font-size: 14px;
  color: #5a3d24;
}

.leaderboard-time {
  font-size: 14px;
  font-weight: 700;
  color: #7a2f26;
}

.leaderboard-card__empty,
.leaderboard-card__error {
  font-size: 13px;
  color: #7a5c3a;
}

.leaderboard-card__error {
  color: #8f2018;
}

.leaderboard-card {
  width: 100%;
  position: relative;
  background:
    repeating-linear-gradient(
      180deg,
      transparent 0 28px,
      rgba(160, 130, 95, 0.06) 28px 29px
    ),
    linear-gradient(180deg, #d8bea0, #ccb084);
  border: 1px solid #a88b62;
  border-radius: 6px;
  padding: 24px 28px;
  box-shadow:
    0 8px 24px rgba(80, 50, 20, 0.2),
    inset 0 1px 0 rgba(255, 255, 255, 0.15);
}

.leaderboard-card__header {
  display: flex;
  flex-direction: column;
  gap: 6px;
  margin-bottom: 16px;
}

.leaderboard-card__eyebrow {
  font-family: var(--font-mono);
  font-size: 11px;
  letter-spacing: 2px;
  text-transform: uppercase;
  color: #8b3a2a;
}

.leaderboard-card__title {
  margin: 0;
  font-size: 20px;
  letter-spacing: 1px;
  color: #5a3d24;
}

.leaderboard-list {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.leaderboard-row {
  display: grid;
  grid-template-columns: 52px 1fr auto;
  gap: 12px;
  align-items: center;
  padding: 12px 14px;
  border: 1px solid rgba(139, 100, 60, 0.18);
  border-radius: 6px;
  background: rgba(255, 248, 236, 0.45);
}

.leaderboard-rank,
.leaderboard-time,
.leaderboard-name {
  font-family: var(--font-mono);
}

.leaderboard-rank {
  font-size: 12px;
  font-weight: 700;
  color: #8b3a2a;
}

.leaderboard-name {
  font-size: 14px;
  color: #5a3d24;
}

.leaderboard-time {
  font-size: 14px;
  font-weight: 700;
  color: #7a2f26;
}

.leaderboard-card__empty,
.leaderboard-card__error {
  font-size: 13px;
  color: #7a5c3a;
}

.leaderboard-card__error {
  color: #8f2018;
}

.leaderboard-card__title {
  margin: 0;
  font-size: 20px;
  letter-spacing: 1px;
  color: #5a3d24;
}

.leaderboard-list {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.leaderboard-row {
  display: grid;
  grid-template-columns: 52px 1fr auto;
  gap: 12px;
  align-items: center;
  padding: 12px 14px;
  border: 1px solid rgba(139, 100, 60, 0.18);
  border-radius: 6px;
  background: rgba(255, 248, 236, 0.45);
}

.leaderboard-rank,
.leaderboard-time,
.leaderboard-name {
  font-family: var(--font-mono);
}

.leaderboard-rank {
  font-size: 12px;
  font-weight: 700;
  color: #8b3a2a;
}

.leaderboard-name {
  font-size: 14px;
  color: #5a3d24;
}

.leaderboard-time {
  font-size: 14px;
  font-weight: 700;
  color: #7a2f26;
}

.leaderboard-card__empty,
.leaderboard-card__error {
  font-size: 13px;
  color: #7a5c3a;
}

.leaderboard-card__error {
  color: #8f2018;
}

.leaderboard-card {
  width: 100%;
  position: relative;
  background:
    repeating-linear-gradient(
      180deg,
      transparent 0 28px,
      rgba(160, 130, 95, 0.06) 28px 29px
    ),
    linear-gradient(180deg, #d8bea0, #ccb084);
  border: 1px solid #a88b62;
  border-radius: 6px;
  padding: 24px 28px;
  box-shadow:
    0 8px 24px rgba(80, 50, 20, 0.2),
    inset 0 1px 0 rgba(255, 255, 255, 0.15);
}

.leaderboard-card__header {
  display: flex;
  flex-direction: column;
  gap: 6px;
  margin-bottom: 16px;
}

.leaderboard-card__eyebrow {
  font-family: var(--font-mono);
  font-size: 11px;
  letter-spacing: 2px;
  text-transform: uppercase;
  color: #4b3523;
  font-weight: 700;
}

.mode-card__toggle {
  display: flex;
  gap: 12px;
}

.mode-card__tab {
  flex: 1;
  border: 1px solid rgba(139, 100, 60, 0.3);
  border-radius: 6px;
  background: rgba(200, 169, 122, 0.5);
  color: #5a3d24;
  font-size: 14px;
  padding: 14px 18px;
  cursor: pointer;
  font-weight: 700;
  letter-spacing: 1px;
  text-transform: uppercase;
  transition: transform 0.2s ease, box-shadow 0.2s ease, background 0.3s ease;
}

.mode-card__tab--standard {
  background: rgba(200, 169, 122, 0.5);
}

.mode-card__tab--team {
  background: rgba(200, 169, 122, 0.4);
}

.mode-card__tab:not(.active) {
  opacity: 0.65;
}

.mode-card__tab.active {
  border-color: #8b3a2a;
  box-shadow: 0 6px 16px rgba(139, 58, 42, 0.25);
  transform: translateY(-2px);
  background: linear-gradient(180deg, #c45144, #a83c30);
  color: #fffaf0;
}

.mode-card__desc {
  margin: 0;
  font-size: 14px;
  color: #7a5c3a;
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
  font-size: 14px;
  color: #7a5c3a;
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
  background:
    repeating-linear-gradient(
      180deg,
      transparent 0 28px,
      rgba(160, 130, 95, 0.06) 28px 29px
    ),
    linear-gradient(180deg, #d4b896, #c8a97a);
  border: 1px solid #a88b62;
  border-radius: 6px;
  padding: 20px;
  box-shadow:
    0 8px 24px rgba(80, 50, 20, 0.25),
    inset 0 1px 0 rgba(255, 255, 255, 0.15);
  min-height: 220px;
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
  border-radius: 6px;
  padding: 14px;
  background: linear-gradient(180deg, #c45144, #a83c30);
  color: #fffaf0;
  font-weight: 700;
  cursor: pointer;
  text-transform: uppercase;
  font-size: 13px;
  letter-spacing: 1px;
  transition: transform 0.2s ease, box-shadow 0.2s ease;
  box-shadow: 0 8px 18px rgba(137, 43, 31, 0.2);
}

.team-lobby-card__button:disabled {
  opacity: 0.5;
  cursor: not-allowed;
  box-shadow: none;
  transform: none;
}

.team-lobby-card__button:not(:disabled):hover {
  transform: translateY(-1px);
  box-shadow: 0 10px 24px rgba(185, 70, 54, 0.3);
}

.team-lobby-card__hint {
  font-size: 12px;
  color: #a08868;
}

.team-lobby-card__error {
  font-size: 12px;
  color: #8f2018;
}

.team-lobby-card__eyebrow {
  font-size: 11px;
  letter-spacing: 1.5px;
  text-transform: uppercase;
  color: #8b3a2a;
}

.team-lobby-card__title {
  margin: 0;
  font-size: 18px;
  letter-spacing: 1px;
  color: #5a3d24;
}

.team-lobby-card__body {
  margin: 0;
  font-size: 13px;
  color: #7a5c3a;
  line-height: 1.5;
}

.team-lobby-card__list {
  margin: 0;
  padding-left: 18px;
  display: flex;
  flex-direction: column;
  gap: 6px;
  color: #7a5c3a;
}

.team-lobby-card__list li {
  font-size: 13px;
  line-height: 1.4;
}

.join-card {
  width: 100%;
  background: linear-gradient(180deg, rgba(255, 249, 238, 0.96), rgba(230, 215, 192, 0.94));
  border: 1px solid rgba(90, 65, 42, 0.2);
  border-radius: 6px;
  padding: 18px 22px;
  display: flex;
  flex-direction: column;
  gap: 12px;
  min-height: 280px;
  box-shadow: var(--paper-shadow);
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
  color: var(--text-muted);
}

.join-card__input {
  background: var(--bg-surface);
  border: 1px solid rgba(90, 65, 42, 0.2);
  border-radius: 6px;
  padding: 10px 12px;
  color: var(--text-primary);
  font-size: 13px;
}

.join-card__button {
  border: none;
  border-radius: 6px;
  padding: 12px;
  background: linear-gradient(180deg, #c45144, #a83c30);
  color: #fffaf0;
  font-weight: 700;
  cursor: pointer;
  transition: transform 0.2s ease;
  box-shadow: 0 8px 18px rgba(137, 43, 31, 0.2);
}

.join-card__button:disabled {
  opacity: 0.6;
  cursor: not-allowed;
  transform: none;
}

.join-card__error {
  color: #8f2018;
  font-size: 12px;
  margin: 0;
}
</style>
