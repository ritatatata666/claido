<template>
  <div class="hub" :class="{ 'is-team': store.teamMode === 'team' }">
    <div v-if="introVisible" class="hub-intro-backdrop" @click.self="dismissIntro">
      <div class="hub-intro-file" role="dialog" aria-modal="true" aria-label="Investigation briefing">
        <div class="hub-intro-file__topline">INTERNAL ACCESS MEMO</div>
        <h2 class="hub-intro-title">Welcome, Detective {{ detectiveName }}</h2>
        <p class="hub-intro-copy">
          NovaCorp was breached overnight and someone inside staged the cover-up. Every room on this board contains part of the truth.
        </p>
        <p class="hub-intro-copy">
          Your objective is simple: extract four key clues, identify the culprit, and unlock the vault before the trail goes cold.
        </p>
        <div class="hub-intro-tags">
          <span class="hub-intro-tag">READ SIGNALS</span>
          <span class="hub-intro-tag">CONNECT EVIDENCE</span>
          <span class="hub-intro-tag">CRACK VAULT</span>
        </div>
        <button class="hub-intro-cta" type="button" @click="dismissIntro">OPEN CASEBOARD</button>
      </div>
    </div>
    <div class="hub-board">

      <header class="hub-topbar evidence-strip">
        <div class="hub-logo">
          <span class="logo-icon">🔐</span>
          <div class="logo-copy">
            <span class="logo-text">CLAIDO</span>
            <span class="logo-sub">Caseboard</span>
          </div>
        </div>

        <div class="hub-progress evidence-mini-note">
          <div class="progress-label">Investigation progress</div>
          <div class="progress-bar-wrap">
            <div class="progress-bar-fill" :style="{ width: progressPercent + '%' }"></div>
          </div>
          <div class="progress-count">
            <span class="count-done">{{ clearedCount }}</span>
            <span class="count-sep"> / </span>
            <span class="count-total">6</span>
            <span class="count-label"> rooms cleared</span>
          </div>
        </div>

        <div class="hub-timer evidence-mini-note">
          <span class="timer-label">Elapsed</span>
          <span class="timer-val">{{ formattedTime }}</span>
        </div>
      </header>

    <TeamModePanel />

      <div class="hub-content-area">
      <!-- Red Sticky Note - moved to front -->
      <div class="sticky-note sticky-note--medium sticky-note--right sticky-note--front">
        <img :src="redPinImg" alt="" class="sticky-pin" />
        <img :src="stickyNoteImg" alt="" class="sticky-image" />
        <div class="sticky-text">VAULT<br/>4 WORDS</div>
      </div>

        <!-- New Sticky Note -->
        <div class="sticky-note sticky-note--small sticky-note--new sticky-note--front">
          <img :src="goldPinImg" alt="" class="sticky-pin" />
          <img :src="stickyNoteImg" alt="" class="sticky-image" />
          <div class="sticky-text">FOLLOW<br/>LEADS</div>
        </div>

        <!-- Missing Person Poster - top left -->
        <aside class="corner-prop corner-prop--left-top missing-poster">
          <img :src="redPinImg" alt="" class="prop-pin-image prop-pin-image--red" />
          <img :src="missingPosterImg" alt="" class="prop-image prop-image--poster-full" />
        </aside>

        <!-- Skull Evidence - left bottom -->
        <aside class="corner-prop corner-prop--left-bottom evidence-photo">
          <img :src="bluePinImg" alt="" class="prop-pin-image prop-pin-image--blue" />
          <div class="evidence-photo__frame">
            <img :src="skullEvidenceImg" alt="" class="prop-image prop-image--photo" />
            <div class="evidence-caption">SCENE 03 · UNRESOLVED</div>
          </div>
        </aside>

        <!-- Unknown Portrait - right top -->
        <aside class="corner-prop corner-prop--right-top evidence-photo evidence-photo--portrait">
          <img :src="pinkPinImg" alt="" class="prop-pin-image prop-pin-image--pink" />
          <div class="evidence-photo__frame evidence-photo__frame--portrait">
            <img :src="unknownPortraitImg" alt="" class="prop-image prop-image--portrait" />
            <div class="evidence-caption">PERSON OF INTEREST</div>
          </div>
        </aside>

        <!-- Error Image - right bottom, bigger -->
        <aside class="corner-prop corner-prop--right-bottom error-evidence">
          <img :src="goldPinImg" alt="" class="prop-pin-image prop-pin-image--gold" />
          <div class="evidence-photo__frame evidence-photo__frame--large">
            <img :src="errorPosterImg" alt="" class="prop-image prop-image--large" />
            <div class="evidence-caption">ERROR LOG EXTRACT</div>
          </div>
        </aside>

        <!-- Missing Image as normal photo - center left -->
        <aside class="corner-prop corner-prop--center-left missing-normal">
          <img :src="redPinLeftImg" alt="" class="prop-pin-image prop-pin-image--red-left" />
          <img :src="missingPosterImg" alt="" class="prop-image prop-image--poster-full" />
        </aside>

        <!-- Sticky Notes -->
        <div class="sticky-note sticky-note--small sticky-note--top">
          <img :src="bluePinLeftImg" alt="" class="sticky-pin" />
          <img :src="stickyNoteImg" alt="" class="sticky-image" />
          <div class="sticky-text">CHECK<br/>LOGS</div>
        </div>

        <div class="sticky-note sticky-note--large sticky-note--bottom sticky-note--front">
          <img :src="pinkPinImg" alt="" class="sticky-pin" />
          <img :src="stickyNoteImg" alt="" class="sticky-image" />
          <div class="sticky-text">INVESTIGATE<br/>ALL ROOMS<br/>FOR CLUES</div>
        </div>

      <div class="board-overlay" aria-hidden="true">
        <span class="overlay-thread overlay-thread--shell-db"></span>
        <span class="overlay-thread overlay-thread--shell-mail vertical-line"></span>
        <span class="overlay-thread overlay-thread--mail-wiki"></span>
        <span class="overlay-thread overlay-thread--wiki-onion"></span>
        <span class="overlay-thread overlay-thread--search-onion"></span>
        <span class="overlay-thread overlay-thread--left-search"></span>
        <span class="overlay-thread overlay-thread--mail-db"></span>
      </div>

        <main class="hub-main">
        <section class="summary-row">
          <article class="summary-note evidence-card">
            <p class="section-label">Board Summary</p>
            <h1 class="summary-title">Track every lead like an evidence wall.</h1>
            <p class="summary-copy">
              Each room is a pinned lead. Cleared rooms stay on the board, in-progress rooms retain their cached frontend state, and the vault remains locked until all four words are collected.
            </p>
          </article>

          <article class="summary-note summary-note--small evidence-card">
            <p class="section-label">Collected Clues</p>
            <div class="clue-stat">{{ store.discoveredClues.length }} / 4</div>
            <p class="summary-copy">Evidence from rooms feeds the vault phrase. Backend room/session behavior is unchanged.</p>
          </article>
        </section>

        <section>
          <div class="section-label section-label--board">Pinned Leads</div>
          <div class="room-grid">
            <div
              v-for="(room, index) in mainRooms"
              :key="room.id"
              :class="['room-panel', `room-panel--${room.color}`, `room-panel--tilt-${index + 1}`, { 'room-panel--cleared': getRoomStatus(room.id) === 'CLEARED' }]"
              @click="enterRoom(room)"
            >
              <div class="panel-body">
                <div class="panel-top">
                  <span class="panel-icon">{{ room.icon }}</span>
                  <div class="panel-name-wrap">
                    <div class="panel-name">{{ room.name }}</div>
                    <div class="panel-desc">{{ room.desc }}</div>
                  </div>
                  <div :class="['panel-badge', `badge--${getRoomStatus(room.id).toLowerCase().replace(' ', '-')}`]">
                    {{ getRoomStatus(room.id) }}
                  </div>
                </div>
              </div>
            </div>
          </div>
        </section>

        <section class="vault-section">
          <div class="section-label section-label--board">Final Objective</div>
          <div class="room-panel room-panel--vault room-panel--tilt-vault" @click="enterRoom(vaultRoom)">
            <div class="panel-body">
              <div class="panel-top">
                <span class="panel-icon">{{ vaultRoom.icon }}</span>
                <div class="panel-name-wrap">
                  <div class="panel-name">{{ vaultRoom.name }}</div>
                  <div class="panel-desc">{{ vaultRoom.desc }}</div>
                </div>
                <div class="vault-clues-hint">
                  {{ store.discoveredClues.length }} / 4 clues collected
                </div>
              </div>
            </div>
          </div>
        </section>
      </main>
      </div>
    </div>
  </div>
</template>

<script setup>
import { computed, onMounted, onUnmounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import { useGameStore } from '../stores/gameStore.js'
import missingPosterImg from '../../images/Missing.png'
import unknownPortraitImg from '../../images/unknown.png'
import skullEvidenceImg from '../../images/skull.png'
import errorPosterImg from '../../images/error.png'
import stickyNoteImg from '../../images/stickyNote.webp'
import redPinImg from '../../images/redPin.png'
import bluePinImg from '../../images/bluePin.png'
import goldPinImg from '../../images/goldPin.png'
import redPinLeftImg from '../../images/redPinLeft.png'
import bluePinLeftImg from '../../images/bluePinLeft.png'
import pinkPinImg from '../../images/pinkPin.png'
import corkboardImg from '../../images/corkboard.avif'
import TeamModePanel from '../components/TeamModePanel.vue'

const router = useRouter()
const store = useGameStore()
const menuOpen = ref(false)
const introVisible = ref(false)

const detectiveName = computed(() => store.currentPlayerName || store.investigatorName || 'Investigator')

const INTRO_SEEN_PREFIX = 'claido_hub_intro_seen_'

const mainRooms = [
  {
    id: 'shell',
    name: 'NovaShell',
    icon: '>_',
    desc: 'Explore the internal filesystem and decode hidden secrets',
    route: '/shell',
    color: 'green',
  },
  {
    id: 'database',
    name: 'NovaCrime DB',
    icon: '⬡',
    desc: 'Query employee records and access logs via SQL',
    route: '/database',
    color: 'blue',
  },
  {
    id: 'mail',
    name: 'NovaMail',
    icon: '✉',
    desc: 'Read intercepted corporate emails for suspicious activity',
    route: '/mail',
    color: 'cyan',
  },
  {
    id: 'wiki',
    name: 'NovaWiki',
    icon: '◈',
    desc: 'Browse classified internal documents and decode redacted sections',
    route: '/wiki',
    color: 'yellow',
  },
  {
    id: 'search',
    name: 'NovaSearch',
    icon: '⊕',
    desc: 'Analyse 50,000 system log entries for anomalies',
    route: '/search',
    color: 'teal',
  },
  {
    id: 'onion',
    name: 'The Onion',
    icon: '⊗',
    desc: 'Browse dark web channels for leaked intelligence',
    route: '/onion',
    color: 'purple',
  },
]

const vaultRoom = {
  id: 'vault',
  name: 'Vault',
  icon: '🔒',
  desc: 'Enter the four-word passphrase to complete the investigation',
  route: '/vault',
}

function getRoomStatus(roomId) {
  if (store.completedRooms.includes(roomId)) return 'CLEARED'
  if (store.roomCache[roomId]) return 'IN PROGRESS'
  return 'ACTIVE'
}

function enterRoom(room) {
  router.push(room.route)
}

const clearedCount = computed(() =>
  mainRooms.filter(r => store.completedRooms.includes(r.id)).length
)

const progressPercent = computed(() => Math.round((clearedCount.value / 6) * 100))

// Timer
const elapsed = ref(0)
let timerInterval = null
let teamRefreshInterval = null

function introStorageKey() {
  return `${INTRO_SEEN_PREFIX}${store.sessionId || 'no-session'}`
}

function dismissIntro() {
  introVisible.value = false
  try {
    localStorage.setItem(introStorageKey(), '1')
  } catch {}
}

function onHubKeydown(event) {
  if (event.key === 'Escape' && introVisible.value) {
    dismissIntro()
  }
}

onMounted(() => {
  if (!store.sessionId) {
    router.replace('/')
    return
  }

  try {
    introVisible.value = localStorage.getItem(introStorageKey()) !== '1'
  } catch {
    introVisible.value = true
  }

  window.addEventListener('keydown', onHubKeydown)

  if (store.teamMode === 'team') {
    store.refreshTeamState().catch(() => {})
    teamRefreshInterval = setInterval(() => {
      if (store.teamMode === 'team') {
        store.refreshTeamState().catch(() => {})
      }
    }, 5000)
  }
  timerInterval = setInterval(() => {
    if (store.gameStartTime) {
      elapsed.value = Math.floor((Date.now() - store.gameStartTime) / 1000)
    }
  }, 1000)
})

onUnmounted(() => {
  clearInterval(timerInterval)
  clearInterval(teamRefreshInterval)
  window.removeEventListener('keydown', onHubKeydown)
})

const formattedTime = computed(() => {
  const s = elapsed.value
  const h = Math.floor(s / 3600)
  const m = Math.floor((s % 3600) / 60)
  const sec = s % 60
  if (h > 0) return `${h}:${String(m).padStart(2, '0')}:${String(sec).padStart(2, '0')}`
  return `${String(m).padStart(2, '0')}:${String(sec).padStart(2, '0')}`
})
</script>

<style scoped>
.hub {
  min-height: 100vh;
  padding: 20px 16px 30px;
  overflow-y: auto;
  position: relative;
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

.hub::before,
.hub::after {
  content: '';
  position: fixed;
  inset: 0;
  pointer-events: none;
}

.hub::before {
  background:
    repeating-linear-gradient(180deg, rgba(255, 255, 255, 0.01) 0 1px, transparent 1px 3px),
    radial-gradient(circle at 34% 22%, rgba(255, 255, 255, 0.05) 0 1px, transparent 1px),
    radial-gradient(circle at 76% 68%, rgba(255, 255, 255, 0.04) 0 1px, transparent 1px);
  background-size: 100% 3px, 180px 180px, 210px 210px;
  mix-blend-mode: soft-light;
  opacity: 0.28;
  z-index: 0;
}

.hub::after {
  background: radial-gradient(circle at center, transparent 56%, rgba(0, 0, 0, 0.42) 100%);
  opacity: 0.5;
  z-index: 0;
}

.hub-board {
  position: relative;
  z-index: 1;
  width: min(1000px, 96%);
  margin: 0 auto;
  display: flex;
  flex-direction: column;
  gap: 20px;
}

.hub-intro-backdrop {
  position: fixed;
  inset: 0;
  z-index: 1200;
  background: rgba(5, 4, 4, 0.74);
  backdrop-filter: blur(2px);
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 20px;
}

.hub-intro-file {
  width: min(680px, 94vw);
  background:
    repeating-linear-gradient(
      180deg,
      rgba(92, 67, 46, 0.08) 0px,
      rgba(92, 67, 46, 0.08) 2px,
      rgba(214, 191, 156, 0.06) 2px,
      rgba(214, 191, 156, 0.06) 4px
    ),
    linear-gradient(165deg, rgba(227, 206, 173, 0.96) 0%, rgba(196, 171, 131, 0.95) 54%, rgba(171, 143, 104, 0.94) 100%);
  border: 2px solid rgba(84, 55, 35, 0.72);
  box-shadow: 0 24px 56px rgba(0, 0, 0, 0.6), inset 0 0 0 1px rgba(255, 236, 200, 0.35);
  padding: 28px 28px 26px;
  transform: rotate(-0.35deg);
}

.hub-intro-file__topline {
  display: inline-block;
  font-size: 11px;
  letter-spacing: 1.9px;
  font-weight: 800;
  color: rgba(99, 47, 35, 0.88);
  border: 1px solid rgba(99, 47, 35, 0.45);
  padding: 4px 10px;
  margin-bottom: 14px;
}

.hub-intro-title {
  margin: 0;
  color: #2f140f;
  font-size: clamp(22px, 4vw, 34px);
  letter-spacing: 0.8px;
  text-transform: uppercase;
  text-shadow: 0 1px 0 rgba(255, 245, 220, 0.45);
}

.hub-intro-copy {
  margin: 14px 0 0;
  color: rgba(40, 20, 14, 0.9);
  font-size: 15px;
  line-height: 1.6;
  max-width: 62ch;
}

.hub-intro-tags {
  margin-top: 16px;
  display: flex;
  flex-wrap: wrap;
  gap: 8px;
}

.hub-intro-tag {
  font-size: 11px;
  font-weight: 800;
  letter-spacing: 1px;
  color: #5e1d1d;
  border: 1px solid rgba(94, 29, 29, 0.36);
  background: rgba(255, 247, 227, 0.36);
  padding: 5px 10px;
}

.hub-intro-cta {
  margin-top: 18px;
  border: 1px solid rgba(78, 29, 27, 0.72);
  background: linear-gradient(180deg, #813532 0%, #5d1e1d 100%);
  color: #f8e5cf;
  font-family: var(--font-mono);
  font-size: 12px;
  letter-spacing: 1.2px;
  font-weight: 800;
  padding: 11px 16px;
  cursor: pointer;
  transition: transform 0.14s ease, box-shadow 0.14s ease;
}

.hub-intro-cta:hover {
  transform: translateY(-1px);
  box-shadow: 0 8px 16px rgba(70, 21, 21, 0.34);
}

.hub-content-area {
  position: relative;
}

.board-overlay {
  position: absolute;
  inset: 0;
  pointer-events: none;
  z-index: 8;
  overflow: visible;
}

.corner-prop {
  position: absolute;
  z-index: 10;
  pointer-events: none;
}

.corner-prop--left-top {
  top: 66px;
  left: -120px;
  transform: rotate(-3deg) scale(0.9);
}

.corner-prop--left-bottom {
  top: 512px;
  left: -110px;
  transform: rotate(-12deg) scale(0.9);
}

.corner-prop--right-top {
  top: 178px;
  right: -130px;
  transform: rotate(6deg) scale(0.9);
}

.corner-prop--right-bottom {
  top: 566px;
  right: -120px;
  transform: rotate(-2deg) scale(0.9);
}

.corner-prop--center-left {
  top: 320px;
  left: -90px;
  transform: rotate(8deg);
  z-index: 30;
}

.prop-pin-image {
  position: absolute;
  top: -12px;
  left: 18px;
  width: 20px;
  height: 20px;
  z-index: 2;
  filter: drop-shadow(0 2px 4px rgba(0, 0, 0, 0.3));
}

.prop-pin-image--red-left {
  left: 16px;
}

.prop-pin-image--pink {
  top: -10px;
  left: 20px;
}

.prop-pin-image--gold {
  top: -14px;
  left: 22px;
}

.missing-poster,
.missing-normal {
  background: transparent;
  border: none;
  box-shadow: none;
}

.evidence-photo__frame {
  background: linear-gradient(180deg, rgba(255, 249, 238, 0.98), rgba(236, 223, 203, 0.96));
  border: 1px solid rgba(89, 65, 42, 0.2);
  box-shadow: var(--paper-shadow);
}

.missing-poster {
  width: auto;
  max-width: 120px;
  padding: 0;
  border-radius: 4px;
  background: transparent;
  border: none;
  box-shadow: none;
}

.missing-normal {
  width: auto;
  max-width: 140px;
  padding: 0;
  border-radius: 3px;
  background: transparent;
  border: none;
  box-shadow: none;
}

.evidence-photo__frame {
  width: 96px;
  height: 142px;
  border-radius: 4px;
  padding: 8px;
  display: flex;
  flex-direction: column;
  justify-content: space-between;
}

.evidence-photo__frame--portrait {
  width: 108px;
  height: 152px;
  align-items: center;
  justify-content: center;
  gap: 12px;
}

.evidence-photo__frame--large {
  width: 140px;
  height: 180px;
  align-items: center;
  justify-content: center;
  gap: 12px;
}

.prop-image {
  display: block;
  width: 100%;
  height: auto;
  border-radius: 2px;
  border: 1px solid rgba(108, 81, 55, 0.2);
  filter: saturate(0.86) contrast(1.03);
}

.prop-image--poster {
  min-height: 120px;
  object-fit: cover;
}

.prop-image--poster-full {
  width: 100%;
  height: auto;
  min-height: unset;
  object-fit: contain;
  border-radius: 4px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.25), 0 2px 6px rgba(0, 0, 0, 0.15);
}

.prop-image--normal {
  min-height: 100px;
  object-fit: cover;
}

.prop-image--photo {
  width: 100%;
  height: 100px;
  object-fit: cover;
}

.prop-image--portrait {
  width: 88px;
  height: 106px;
  object-fit: cover;
  margin: 0 auto;
}

.prop-image--large {
  width: 120px;
  height: 140px;
  object-fit: cover;
  margin: 0 auto;
}

/* Sticky Notes */
.sticky-note {
  position: absolute;
  z-index: 3;
  pointer-events: none;
}

.sticky-note--small {
  width: 60px;
  height: 60px;
}

.sticky-note--medium {
  width: 80px;
  height: 80px;
}

.sticky-note--large {
  width: 100px;
  height: 100px;
}

.sticky-note--front {
  z-index: 15;
}

.sticky-note--new {
  top: -10px;
  left: -46px;
  transform: rotate(-6deg);
}

.sticky-note--top {
  top: -200px;
  left: -200px;
  transform: rotate(-8deg);
  opacity: 0;
}

.sticky-note--right {
  top: 300px;
  right: 8%;
  transform: rotate(12deg);
}

.sticky-note--bottom {
  bottom: 120px;
  left: 36%;
  transform: rotate(-4deg);
}

.sticky-image {
  width: 100%;
  height: 100%;
  object-fit: cover;
  filter: drop-shadow(0 2px 6px rgba(0, 0, 0, 0.2));
}

.sticky-pin {
  position: absolute;
  top: 25%;
  left: 50%;
  transform: translateX(-50%);
  width: 16px;
  height: 16px;
  z-index: 1;
  filter: drop-shadow(0 1px 3px rgba(0, 0, 0, 0.3));
}

.sticky-text {
  position: absolute;
  top: 65%;
  left: 50%;
  transform: translate(-50%, -50%);
  font-family: var(--font-mono);
  font-size: 8px;
  font-weight: 700;
  color: #2d4a3b;
  text-align: center;
  line-height: 1.2;
  letter-spacing: 0.5px;
}

.sticky-note--medium .sticky-text {
  font-size: 9px;
}

.sticky-note--large .sticky-text {
  font-size: 10px;
}

.evidence-caption {
  font-family: var(--font-mono);
  font-size: 8px;
  letter-spacing: 1px;
  color: var(--text-muted);
  text-align: center;
}

.overlay-thread {
  position: absolute;
  background: #9b1f24;
  box-shadow: 0 1px 4px rgba(54, 10, 12, 0.55);
  pointer-events: none;
  z-index: 8;
  opacity: 0.94;
}

/* Horizontal lines */
.overlay-thread:not(.vertical-line) {
  height: 3px;
  border-radius: 1px;
}

/* Vertical lines */
.overlay-thread.vertical-line {
  width: 3px;
  border-radius: 1px;
}

.overlay-thread::before,
.overlay-thread::after {
  content: '';
  position: absolute;
  width: 8px;
  height: 8px;
  background: #6e1418;
  border-radius: 50%;
  top: -3px;
  box-shadow: 0 1px 2px rgba(0, 0, 0, 0.3), inset 0 1px 1px rgba(255, 255, 255, 0.2);
}

.overlay-thread::before {
  left: -4px;
}

.overlay-thread::after {
  right: -4px;
}

/* Fix vertical thread pins: bottom pin at the bottom end */
.overlay-thread.vertical-line::before {
  left: -2.5px;
}

.overlay-thread.vertical-line::after {
  top: auto;
  bottom: -3px;
  left: -2.5px;
  right: auto;
}

/* Summary area -> NovaShell pin (diagonal) */
.overlay-thread--shell-db {
  top: 258px;
  left: 6%;
  width: 20%;
  transform: rotate(-22deg);
}

/* Summary bottom -> row 1 center (straight vertical) */
.overlay-thread--shell-mail {
  top: 226px;
  left: 39%;
  height: 70px;
}

/* Row 1 connector (near bottom edge to avoid card text) */
.overlay-thread--db-search {
  top: 327px;
  left: 39%;
  width: 24%;
  transform: rotate(2deg);
}

/* Collected clues card -> NovaWiki status badge (diagonal right edge) */
.overlay-thread--mail-wiki {
  top: 308px;
  left: 87%;
  width: 20%;
  transform: rotate(74deg);
}

/* Row 2 center -> The Onion top edge (diagonal) */
.overlay-thread--wiki-onion {
  top: 395px;
  left: 41%;
  width: 19%;
  transform: rotate(52deg);
}

/* The Onion right edge -> error prop (diagonal) */
.overlay-thread--search-onion {
  top: 486px;
  left: 74%;
  width: 31%;
  transform: rotate(10deg);
}

/* Left board edge -> NovaSearch icon area (diagonal) */
.overlay-thread--left-search {
  top: 414px;
  left: -9%;
  width: 24%;
  transform: rotate(26deg);
}

/* NovaMail -> NovaCrime DB across center gap (diagonal) */
.overlay-thread--mail-db {
  top: 397px;
  left: 28%;
  width: 44%;
  transform: rotate(-17deg);
}

.hub-topbar.evidence-strip {
  position: relative;
  left: 0;
}

.overlay-pin--db {
  top: 414px;
  left: 62%;
}

.evidence-strip,
.evidence-card,
.evidence-mini-note,
.room-panel {
  position: relative;
  z-index: 1;
  background: linear-gradient(180deg, rgba(231, 219, 196, 0.96), rgba(204, 187, 157, 0.94));
  border: 1px solid rgba(66, 46, 31, 0.35);
  box-shadow: var(--paper-shadow);
}

.prop-pin-image {
  position: absolute;
  top: -12px;
  left: 18px;
  width: 20px;
  height: 20px;
  z-index: 2;
  filter: drop-shadow(0 2px 4px rgba(0, 0, 0, 0.3));
}

.prop-pin-image--red-left {
  left: 16px;
}

.prop-pin-image--pink {
  top: -10px;
  left: 20px;
}

.prop-pin-image--gold {
  top: -14px;
  left: 22px;
}

.missing-poster,
.missing-normal {
  background: transparent;
  border: none;
  box-shadow: none;
}

.evidence-photo__frame {
  background: linear-gradient(180deg, rgba(255, 249, 238, 0.98), rgba(236, 223, 203, 0.96));
  border: 1px solid rgba(89, 65, 42, 0.2);
  box-shadow: var(--paper-shadow);
}

.missing-poster {
  width: auto;
  max-width: 120px;
  padding: 0;
  border-radius: 4px;
  background: transparent;
  border: none;
  box-shadow: none;
}

.missing-normal {
  width: auto;
  max-width: 140px;
  padding: 0;
  border-radius: 3px;
  background: transparent;
  border: none;
  box-shadow: none;
}

.evidence-photo__frame {
  width: 96px;
  height: 142px;
  border-radius: 4px;
  padding: 8px;
  display: flex;
  flex-direction: column;
  justify-content: space-between;
}

.evidence-photo__frame--portrait {
  width: 108px;
  height: 152px;
  align-items: center;
  justify-content: center;
  gap: 12px;
}

.evidence-photo__frame--large {
  width: 140px;
  height: 180px;
  align-items: center;
  justify-content: center;
  gap: 12px;
}

.prop-image {
  display: block;
  width: 100%;
  height: auto;
  border-radius: 2px;
  border: 1px solid rgba(108, 81, 55, 0.2);
  filter: saturate(0.86) contrast(1.03);
}

.prop-image--poster {
  min-height: 120px;
  object-fit: cover;
}

.prop-image--poster-full {
  width: 100%;
  height: auto;
  min-height: unset;
  object-fit: contain;
  border-radius: 4px;
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.25), 0 2px 6px rgba(0, 0, 0, 0.15);
}

.prop-image--normal {
  min-height: 100px;
  object-fit: cover;
}

.prop-image--photo {
  width: 100%;
  height: 100px;
  object-fit: cover;
}

.prop-image--portrait {
  width: 88px;
  height: 106px;
  object-fit: cover;
  margin: 0 auto;
}

.prop-image--large {
  width: 120px;
  height: 140px;
  object-fit: cover;
  margin: 0 auto;
}

/* Sticky Notes */
.sticky-note {
  position: absolute;
  z-index: 3;
  pointer-events: none;
}

.sticky-note--small {
  width: 60px;
  height: 60px;
}

.sticky-note--medium {
  width: 80px;
  height: 80px;
}

.sticky-note--large {
  width: 100px;
  height: 100px;
}

.sticky-note--front {
  z-index: 15;
}

.sticky-note--top {
  top: -200px;
  left: -200px;
  transform: rotate(-8deg);
  opacity: 0;
}

.sticky-note--right {
  top: 300px;
  right: 8%;
  transform: rotate(12deg);
}

.sticky-note--bottom {
  bottom: 120px;
  left: 36%;
  transform: rotate(-4deg);
}

.sticky-image {
  width: 100%;
  height: 100%;
  object-fit: cover;
  filter: drop-shadow(0 2px 6px rgba(0, 0, 0, 0.2));
}

.sticky-pin {
  position: absolute;
  top: 25%;
  left: 50%;
  transform: translateX(-50%);
  width: 16px;
  height: 16px;
  z-index: 1;
  filter: drop-shadow(0 1px 3px rgba(0, 0, 0, 0.3));
}

.sticky-text {
  position: absolute;
  top: 65%;
  left: 50%;
  transform: translate(-50%, -50%);
  font-family: var(--font-mono);
  font-size: 8px;
  font-weight: 700;
  color: #2d4a3b;
  text-align: center;
  line-height: 1.2;
  letter-spacing: 0.5px;
}

.sticky-note--medium .sticky-text {
  font-size: 9px;
}

.sticky-note--large .sticky-text {
  font-size: 10px;
}

.evidence-caption {
  font-family: var(--font-mono);
  font-size: 8px;
  letter-spacing: 1px;
  color: var(--text-muted);
  text-align: center;
}

.evidence-strip {
  border-radius: 12px;
  padding: 18px 20px;
  display: grid;
  grid-template-columns: 1fr minmax(240px, 340px) auto;
  gap: 18px;
  align-items: center;
}

.hub-logo {
  display: flex;
  align-items: center;
  gap: 12px;
  position: relative;
  cursor: pointer;
  user-select: none;
}

.logo-arrow {
  font-size: 9px;
  opacity: 0.6;
  margin-left: 4px;
}

.hub-logo-menu {
  position: absolute;
  top: calc(100% + 8px);
  left: 0;
  min-width: 160px;
  background: linear-gradient(180deg, #d4b896, #c8a97a);
  border: 1px solid #a88b62;
  border-radius: 6px;
  box-shadow: 0 8px 24px rgba(80, 50, 20, 0.35);
  z-index: 100;
  overflow: hidden;
}

.hub-menu-item {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 10px 16px;
  font-family: var(--font-mono);
  font-size: 12px;
  letter-spacing: 1px;
  text-transform: uppercase;
  color: #5a3d24;
  cursor: pointer;
  transition: background 0.12s;
}

.hub-menu-item:hover {
  background: rgba(139, 100, 60, 0.2);
}

.hub-menu-icon {
  font-size: 14px;
}

.hub-dropdown-enter-active,
.hub-dropdown-leave-active {
  transition: opacity 0.15s, transform 0.15s;
}
.hub-dropdown-enter-from,
.hub-dropdown-leave-to {
  opacity: 0;
  transform: translateY(-6px);
}

.logo-copy {
  display: flex;
  flex-direction: column;
}

.logo-icon {
  font-size: 24px;
}

.logo-text {
  font-size: 22px;
  letter-spacing: 4px;
  font-weight: 800;
  color: #2f1d11;
}

.logo-sub,
.progress-label,
.count-label,
.timer-label,
.section-label {
  font-family: var(--font-mono);
  text-transform: uppercase;
}

.logo-sub {
  font-size: 10px;
  letter-spacing: 2.4px;
  color: var(--text-muted);
}

.evidence-mini-note {
  border-radius: 8px;
  padding: 10px 12px;
}

.hub-progress {
  display: flex;
  flex-direction: column;
  gap: 6px;
}

.progress-label,
.timer-label,
.section-label {
  font-size: 10px;
  letter-spacing: 2px;
  color: var(--text-muted);
}

.progress-bar-wrap {
  width: 100%;
  height: 8px;
  background: rgba(109, 78, 49, 0.14);
  border-radius: 999px;
  overflow: hidden;
}

.progress-bar-fill {
  height: 100%;
  background: linear-gradient(90deg, #b94636, #d16957);
  border-radius: 999px;
  transition: width 0.4s ease;
}

.progress-count {
  color: var(--text-secondary);
  font-size: 13px;
}

.count-done {
  font-weight: 800;
  color: var(--accent-red);
}

.count-total {
  color: var(--text-primary);
}

.hub-timer {
  display: flex;
  flex-direction: column;
  align-items: flex-end;
  gap: 4px;
}

.timer-val {
  font-family: var(--font-mono);
  font-size: 20px;
  color: var(--accent-orange);
  letter-spacing: 2px;
}

.hub-main {
  display: flex;
  flex-direction: column;
  gap: 22px;
}

.summary-row {
  display: grid;
  grid-template-columns: minmax(0, 1.3fr) minmax(240px, 0.7fr);
  gap: 22px;
}

.evidence-card {
  border-radius: 10px;
  padding: 24px 22px 22px;
  overflow: hidden;
}

.evidence-card::after,
.room-panel::after {
  content: '';
  position: absolute;
  inset: 0;
  background: repeating-linear-gradient(180deg, transparent 0 23px, rgba(121, 92, 67, 0.08) 23px 24px);
  pointer-events: none;
}

.evidence-card::before,
.room-panel::before {
  content: '';
  position: absolute;
  inset: 0;
  background:
    radial-gradient(circle at 14% 10%, rgba(127, 22, 22, 0.08), transparent 35%),
    radial-gradient(circle at 82% 86%, rgba(0, 0, 0, 0.12), transparent 42%);
  pointer-events: none;
}

.summary-title {
  position: relative;
  z-index: 1;
  margin: 6px 0 10px;
  font-size: clamp(28px, 3vw, 40px);
  line-height: 1.15;
  color: var(--text-primary);
}

.summary-copy {
  position: relative;
  z-index: 1;
  margin: 0;
  color: var(--text-secondary);
  line-height: 1.7;
}

.summary-note--small {
  display: flex;
  flex-direction: column;
  justify-content: center;
}

.clue-stat {
  position: relative;
  z-index: 1;
  font-family: var(--font-mono);
  font-size: 34px;
  font-weight: 800;
  color: var(--accent-red);
  margin: 8px 0 10px;
}

.section-label--board {
  margin-bottom: 10px;
}

.room-grid {
  display: grid;
  grid-template-columns: repeat(2, minmax(0, 1fr));
  gap: 18px;
}

.room-panel {
  border-radius: 8px;
  cursor: pointer;
  transition: transform 0.18s ease, box-shadow 0.18s ease, border-color 0.18s ease;
  overflow: hidden;
  transform: rotate(var(--panel-tilt, 0deg));
}

.room-panel:hover {
  transform: translateY(-3px) rotate(calc(var(--panel-tilt, 0deg) * 0.45));
  box-shadow: 0 18px 28px rgba(61, 37, 20, 0.2);
}

.room-grid .room-panel:nth-child(1) {
  --panel-tilt: -1.1deg;
}

.room-grid .room-panel:nth-child(2) {
  --panel-tilt: 0.95deg;
}

.room-grid .room-panel:nth-child(3) {
  --panel-tilt: -0.55deg;
}

.room-grid .room-panel:nth-child(4) {
  --panel-tilt: 1.15deg;
}

.room-grid .room-panel:nth-child(5) {
  --panel-tilt: -0.85deg;
}

.room-grid .room-panel:nth-child(6) {
  --panel-tilt: 0.7deg;
}

.room-panel--vault {
  --panel-tilt: -0.4deg;
}

.panel-body {
  position: relative;
  z-index: 1;
  padding: 22px 20px 18px;
}

.panel-top {
  display: flex;
  align-items: flex-start;
  gap: 14px;
}

.panel-icon {
  width: 34px;
  text-align: center;
  font-size: 24px;
  color: var(--text-muted);
  flex-shrink: 0;
}

.panel-name-wrap {
  flex: 1;
  min-width: 0;
}

.panel-name {
  margin-bottom: 4px;
  font-size: 16px;
  font-weight: 800;
  color: var(--text-primary);
  letter-spacing: 1px;
  text-transform: uppercase;
}

.panel-desc {
  color: var(--text-secondary);
  font-size: 13px;
  line-height: 1.5;
}

.panel-badge {
  flex-shrink: 0;
  padding: 4px 8px;
  border-radius: 999px;
  font-size: 10px;
  font-weight: 800;
  letter-spacing: 1.5px;
  text-transform: uppercase;
  border: 1px solid;
  background: rgba(255, 250, 241, 0.75);
}

.badge--active {
  color: var(--accent-green);
  border-color: rgba(95, 112, 65, 0.5);
}

.badge--in-progress {
  color: var(--accent-orange);
  border-color: rgba(164, 99, 47, 0.5);
}

.badge--cleared {
  color: var(--accent-blue);
  border-color: rgba(86, 120, 140, 0.45);
}

.room-panel--green .panel-icon { color: #5f7041; }
.room-panel--blue .panel-icon { color: #56788c; }
.room-panel--cyan .panel-icon { color: #4d8f96; }
.room-panel--yellow .panel-icon { color: #bc8c2c; }
.room-panel--teal .panel-icon { color: #4e7d75; }
.room-panel--purple .panel-icon { color: #76557e; }

.room-panel--green:hover { border-color: rgba(95, 112, 65, 0.45); }
.room-panel--blue:hover { border-color: rgba(86, 120, 140, 0.45); }
.room-panel--cyan:hover { border-color: rgba(77, 143, 150, 0.45); }
.room-panel--yellow:hover { border-color: rgba(188, 140, 44, 0.45); }
.room-panel--teal:hover { border-color: rgba(78, 125, 117, 0.45); }
.room-panel--purple:hover { border-color: rgba(118, 85, 126, 0.45); }

/* Room Card Rotations for Natural Crime Board Look */
.room-panel--tilt-1 {
  transform: rotate(-3.2deg);
  margin-right: -12px;
  z-index: 5;
}

.room-panel--tilt-2 {
  transform: rotate(2.8deg);
  margin-left: -8px;
  z-index: 4;
}

.room-panel--tilt-3 {
  transform: rotate(-1.6deg);
  margin-right: -10px;
  z-index: 6;
}

.room-panel--tilt-4 {
  transform: rotate(3.4deg);
  margin-left: -14px;
  z-index: 3;
}

.room-panel--tilt-5 {
  transform: rotate(-2.9deg);
  margin-right: -8px;
  z-index: 5;
}

.room-panel--tilt-6 {
  transform: rotate(1.4deg);
  margin-left: -6px;
  z-index: 4;
}

.room-panel--tilt-vault {
  transform: rotate(0deg);
}

.room-panel:hover {
  transform: scale(1.02) rotate(0deg) !important;
  transition: transform 0.2s ease;
}

.room-panel--cleared {
  opacity: 0.76;
}

.vault-section {
  display: flex;
  flex-direction: column;
}

.room-panel--vault {
  background: linear-gradient(180deg, rgba(253, 244, 220, 0.98), rgba(238, 220, 176, 0.92));
}

.room-panel--vault .panel-icon,
.room-panel--vault .panel-name {
  color: #8f6317;
}

.vault-clues-hint {
  flex-shrink: 0;
  align-self: center;
  padding: 6px 10px;
  border-radius: 999px;
  background: rgba(255, 248, 226, 0.75);
  color: #8f6317;
  font-size: 11px;
  font-weight: 700;
  white-space: nowrap;
}

@media (max-width: 900px) {
  .evidence-strip,
  .summary-row,
  .room-grid {
    grid-template-columns: 1fr;
  }

  .hub-timer {
    align-items: flex-start;
  }

  .overlay-thread--one,
  .overlay-thread--four,
  .overlay-thread--seven {
    width: 320px;
  }

  .overlay-thread--five,
  .overlay-thread--nine {
    width: 420px;
  }

  .corner-prop {
    opacity: 0.5;
    transform: scale(0.82);
  }

  .corner-prop--left-top,
  .corner-prop--left-bottom {
    left: -90px;
  }

  .corner-prop--right-top,
  .corner-prop--right-bottom {
    right: -100px;
  }

  .overlay-pin--summary-right {
    right: 18%;
  }

  .overlay-pin--db,
  .overlay-pin--wiki,
  .overlay-pin--onion {
    left: 78%;
  }
}

@media (max-width: 640px) {
  .hub {
    padding: 20px 14px 28px;
  }

  .panel-top {
    flex-wrap: wrap;
  }

  .vault-clues-hint {
    white-space: normal;
  }

  .board-overlay {
    display: none;
  }

  .overlay-thread,
  .overlay-pin {
    opacity: 0.78;
  }

  .corner-prop {
    display: none;
  }
}
</style>
