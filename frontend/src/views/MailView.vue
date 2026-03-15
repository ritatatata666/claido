<template>
  <RoomLayout>
    <div class="mail-view">
      <!-- Sidebar -->
      <div class="mail-sidebar">
        <div class="mail-logo nova-glitch" data-text="NOVAMAIL">NOVAMAIL</div>
        <div class="folder-list">
          <div
            v-for="folder in folders"
            :key="folder"
            :class="['folder-item', { active: activeFolder === folder }]"
            @click="activeFolder = folder"
          >
            <span class="folder-icon">{{ folderIcons[folder] }}</span>
            {{ folder }}
            <span class="folder-count">{{ countFolder(folder) }}</span>
          </div>
        </div>
      </div>

      <!-- Email list -->
      <div class="email-list">
        <div v-if="loading" class="loading-state">
          <span class="spinner"></span> Loading emails...
        </div>
        <div
          v-for="email in filteredEmails"
          :key="email.id"
          :class="['email-row', { unread: !email.isRead, selected: selectedEmail?.id === email.id }]"
          @click="selectEmail(email)"
        >
          <div class="email-row-top">
            <div class="email-from">{{ email.from }}</div>
            <button
              :class="['flag-btn', { flagged: email.isFlagged }]"
              :title="email.isFlagged ? 'Unflag' : 'Flag'"
              @click.stop="toggleFlag(email)"
            >🚩</button>
          </div>
          <div class="email-subject">{{ email.subject }}</div>
          <div class="email-snippet">{{ snippetText(email.body) }}</div>
          <div class="email-date">{{ formatDate(email.date) }}</div>
        </div>
        <div v-if="!loading && filteredEmails.length === 0" class="empty-folder">
          No messages in {{ activeFolder }}.
        </div>
      </div>

      <!-- Email viewer -->
      <div class="email-viewer">
        <div v-if="!selectedEmail" class="no-selection">
          <span>Select an email to read</span>
        </div>
        <div v-else class="email-content">
          <div class="email-header">
            <h2 class="email-subject-large">{{ selectedEmail.subject }}</h2>
            <div class="email-meta">
              <span><strong>From:</strong> {{ selectedEmail.from }}</span>
              <span><strong>To:</strong> {{ selectedEmail.to }}</span>
              <span><strong>Date:</strong> {{ formatDateFull(selectedEmail.date) }}</span>
            </div>
          </div>
          <div class="email-body">{{ selectedEmail.body }}</div>
          <div class="email-actions">
            <button
              class="btn-evidence"
              :class="{ submitted: evidenceResult === 'correct', wrong: evidenceResult === 'wrong' || evidenceResult === 'locked' }"
              @click="submitEvidence(selectedEmail)"
            >
              <span v-if="evidenceResult === 'correct'">✓ Evidence logged</span>
              <span v-else-if="evidenceResult === 'locked'">🔒 Room locked after too many wrong attempts</span>
              <span v-else-if="evidenceResult === 'wrong'">✗ Nothing suspicious</span>
              <span v-else>⚑ Submit as evidence</span>
            </button>
          </div>
        </div>
      </div>
    </div>
    <NpcChat npc-id="receptionist" npc-name="Maya Chen" npc-role="Receptionist" />
  </RoomLayout>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import RoomLayout from '../components/RoomLayout.vue'
import NpcChat from '../components/NpcChat.vue'
import { useGameStore } from '../stores/gameStore.js'

const store = useGameStore()
const emails = ref([])
const loading = ref(true)
const selectedEmail = ref(null)
const activeFolder = ref('inbox')
const evidenceResult = ref(null)

const folders = ['inbox', 'sent', 'flagged']
const folderIcons = { inbox: '📥', sent: '📤', flagged: '🚩' }

onMounted(async () => {
  const vaultWord2 = resolveVaultWord2()
  try {
    const data = await store.enterRoom('mail')
    const raw = Array.isArray(data) ? data : []
    emails.value = normalizeEmails(raw, vaultWord2)
  } catch (e) {
    emails.value = getDefaultEmails(vaultWord2)
  } finally {
    loading.value = false
  }
})

function resolveVaultWord2() {
  const fromSession = String(store.sessionState?.vaultWord2 || '').toLowerCase().trim()
  if (fromSession) return fromSession
  const fallback = 'midnight'
  console.warn('[NovaMail] Using fallback vaultWord2 because session value is missing.')
  return fallback
}

function normalizeEmails(rawEmails, vaultWord2) {
  const normalized = rawEmails.map(e => ({ ...e, isFlagged: false }))
  const lowerVault = vaultWord2.toLowerCase()
  const hasClue = normalized.some(e => String(e.body || '').toLowerCase().includes(lowerVault))
  if (hasClue) return normalized

  if (normalized.length > 0) {
    const idx = normalized.findIndex(e => String(e.from || '').toLowerCase().includes('unknown'))
    const target = idx >= 0 ? idx : 0
    normalized[target] = {
      ...normalized[target],
      body: `${String(normalized[target].body || '').trim()} The word you need is "${vaultWord2}".`.trim(),
    }
    console.warn('[NovaMail] Injected fallback clue word into room data to keep puzzle solvable.')
    return normalized
  }

  console.warn('[NovaMail] Mail room data empty; using default emails with fallback clue word.')
  return getDefaultEmails(vaultWord2)
}

const filteredEmails = computed(() => {
  if (activeFolder.value === 'flagged') return emails.value.filter(e => e.isFlagged)
  return emails.value.filter(e => e.folder === activeFolder.value)
})

function countFolder(folder) {
  if (folder === 'flagged') return emails.value.filter(e => e.isFlagged).length
  return emails.value.filter(e => e.folder === folder).length
}

function snippetText(body) {
  return body?.slice(0, 80) + (body?.length > 80 ? '...' : '')
}

function selectEmail(email) {
  email.isRead = true
  selectedEmail.value = email
  evidenceResult.value = null
}

function toggleFlag(email) {
  email.isFlagged = !email.isFlagged
}

async function submitEvidence(email) {
  if (store.isRoomLocked('mail')) {
    evidenceResult.value = 'locked'
    return
  }
  const vaultWord = resolveVaultWord2()
  if (vaultWord && email.body?.toLowerCase().includes(vaultWord.toLowerCase())) {
    evidenceResult.value = 'correct'
    store.addClue(
      'mail-vault-word',
      'NovaMail',
      `Suspicious email from ${email.from}: contains the keyword "${vaultWord}".`
    )
    store.markRoomComplete('mail')
  } else {
    const penalty = await store.registerWrongAttempt('mail')
    evidenceResult.value = penalty.locked ? 'locked' : 'wrong'
  }
}

function formatDate(iso) {
  if (!iso) return ''
  const d = new Date(iso)
  return d.toLocaleDateString('en-AU', { month: 'short', day: 'numeric' })
}

function formatDateFull(iso) {
  if (!iso) return ''
  return new Date(iso).toLocaleString('en-AU')
}

function getDefaultEmails(vaultWord2) {
  return [
    {
      id: 'msg-001',
      from: 'ceo@novacorp.com',
      to: 'analyst@novacorp.com',
      subject: 'Project Nova — Confidential',
      date: '2025-03-02T09:00:00',
      body: 'The board has approved the midnight handover. Do not document this in the usual channels.',
      isRead: false,
      folder: 'inbox',
      isFlagged: false,
    },
    {
      id: 'msg-002',
      from: 'unknown@protonmail.com',
      to: 'analyst@novacorp.com',
      subject: 'Re: Tonight',
      date: '2025-03-02T20:11:00',
      body: `Everything is ready for the handover. Come alone. The word you need is "${vaultWord2}". Delete this.`,
      isRead: false,
      folder: 'inbox',
      isFlagged: false,
    },
    {
      id: 'msg-003',
      from: 'hr@novacorp.com',
      to: 'all@novacorp.com',
      subject: 'Reminder: Annual Security Audit',
      date: '2025-02-28T10:30:00',
      body: 'All employees must complete the security awareness training by end of month.',
      isRead: true,
      folder: 'inbox',
      isFlagged: false,
    },
  ]
}
</script>

<style scoped>
.mail-view {
  --paper-bg: linear-gradient(180deg, #d4b58a 0%, #c8a476 100%);
  --paper-edge: #ab8659;
  --ink-strong: #3e2615;
  --ink-soft: #6e4b2f;
  --ink-muted: #846244;
  --line: rgba(110, 75, 47, 0.28);
  --line-soft: rgba(110, 75, 47, 0.16);
  --accent: #9b2f25;
  --accent-soft: rgba(155, 47, 37, 0.12);

  display: grid;
  grid-template-columns: 180px 280px 1fr;
  height: 100%;
  min-height: 0;
  overflow: auto;
  position: relative;
  gap: 12px;
  padding: 14px;
  border-radius: 10px;
  border: 1px solid var(--paper-edge);
  background: var(--paper-bg);
  box-shadow:
    0 10px 24px rgba(42, 24, 10, 0.26),
    inset 0 1px 0 rgba(255, 255, 255, 0.2);
  font-family: var(--font-mono);
}

.mail-view::before {
  content: '';
  position: absolute;
  inset: 0;
  border-radius: 10px;
  background:
    repeating-linear-gradient(
      180deg,
      transparent 0 26px,
      rgba(110, 75, 47, 0.06) 26px 27px
    ),
    repeating-linear-gradient(
      90deg,
      transparent 0 32px,
      rgba(110, 75, 47, 0.035) 32px 33px
    );
  pointer-events: none;
}

.mail-sidebar,
.email-list,
.email-viewer {
  border: 1px solid var(--paper-edge);
  border-radius: 8px;
  background: linear-gradient(180deg, rgba(255, 247, 233, 0.86), rgba(237, 210, 172, 0.78));
  box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.22);
}

.mail-sidebar {
  display: flex;
  flex-direction: column;
  overflow: hidden;
  min-height: 0;
}

.mail-logo {
  padding: 12px 14px;
  font-size: 12px;
  font-weight: 700;
  color: var(--ink-strong);
  border-bottom: 1px solid var(--line);
  background: rgba(255, 255, 255, 0.3);
  letter-spacing: 2px;
  text-transform: uppercase;
}

.folder-list {
  padding: 8px;
  overflow-y: auto;
}

.folder-item {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 10px;
  margin-bottom: 6px;
  border-radius: 6px;
  border: 1px solid transparent;
  cursor: pointer;
  font-size: 12px;
  color: var(--ink-soft);
  transition: background 0.15s, border-color 0.15s, color 0.15s;
}

.folder-item:hover {
  background: rgba(255, 255, 255, 0.35);
  border-color: var(--line-soft);
  color: var(--ink-strong);
}

.folder-item.active {
  background: rgba(155, 47, 37, 0.12);
  border-color: rgba(155, 47, 37, 0.35);
  color: #6f2018;
  font-weight: 700;
}

.folder-icon { font-size: 14px; }

.folder-count {
  margin-left: auto;
  min-width: 20px;
  text-align: center;
  font-size: 10px;
  border-radius: 999px;
  padding: 2px 6px;
  border: 1px solid var(--line-soft);
  background: rgba(255, 255, 255, 0.4);
  color: var(--ink-muted);
}

.email-list {
  overflow-y: auto;
  display: flex;
  flex-direction: column;
  min-height: 0;
}

.loading-state {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 14px;
  color: var(--ink-soft);
  font-size: 12px;
}

.empty-folder {
  padding: 18px;
  text-align: center;
  color: var(--ink-muted);
  font-size: 12px;
  font-style: italic;
}

.email-row {
  padding: 10px 12px;
  border-bottom: 1px solid var(--line-soft);
  cursor: pointer;
  transition: background 0.15s, border-color 0.15s;
}

.email-row:hover { background: rgba(255, 255, 255, 0.42); }

.email-row.selected {
  background: rgba(155, 47, 37, 0.12);
  box-shadow: inset 3px 0 0 rgba(155, 47, 37, 0.75);
}

.email-row.unread .email-from,
.email-row.unread .email-subject { font-weight: 700; color: var(--ink-strong); }

.email-row-top {
  display: flex;
  align-items: center;
  gap: 4px;
  margin-bottom: 3px;
}

.flag-btn {
  border: 1px solid transparent;
  background: rgba(255, 255, 255, 0.34);
  border-radius: 6px;
  padding: 1px 5px;
  cursor: pointer;
  font-size: 12px;
  opacity: 0.45;
  flex-shrink: 0;
  line-height: 1;
  transition: opacity 0.12s, border-color 0.12s;
}

.flag-btn:hover {
  opacity: 0.75;
  border-color: var(--line-soft);
}

.flag-btn.flagged {
  opacity: 1;
  border-color: rgba(155, 47, 37, 0.45);
  background: var(--accent-soft);
}

.email-from {
  font-size: 12px;
  color: var(--ink-soft);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  flex: 1;
  min-width: 0;
}

.email-subject {
  font-size: 13px;
  color: var(--ink-strong);
  margin-bottom: 3px;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.email-snippet {
  font-size: 11px;
  color: var(--ink-muted);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.email-date {
  font-size: 10px;
  color: var(--ink-muted);
  margin-top: 3px;
}

.email-viewer {
  overflow-y: auto;
  padding: 18px;
  min-height: 0;
}

.no-selection {
  display: flex;
  align-items: center;
  justify-content: center;
  height: 100%;
  color: var(--ink-muted);
  font-size: 13px;
  text-transform: uppercase;
  letter-spacing: 0.8px;
}

.email-header {
  border-bottom: 1px solid var(--line);
  padding-bottom: 12px;
  margin-bottom: 14px;
}

.email-subject-large {
  font-size: 17px;
  font-weight: 700;
  color: var(--ink-strong);
  margin-bottom: 10px;
  line-height: 1.4;
}

.email-meta {
  display: flex;
  flex-direction: column;
  gap: 4px;
  font-size: 12px;
  color: var(--ink-soft);
}

.email-body {
  font-size: 13px;
  color: var(--ink-strong);
  line-height: 1.7;
  white-space: pre-wrap;
}

.email-actions {
  margin-top: 16px;
  padding-top: 12px;
  border-top: 1px solid var(--line);
}

.btn-evidence {
  padding: 8px 14px;
  font-size: 12px;
  font-weight: 700;
  font-family: var(--font-mono);
  letter-spacing: 0.3px;
  border: 1px solid rgba(109, 72, 38, 0.45);
  border-radius: 6px;
  background: linear-gradient(180deg, #6e4a2d, #4f3420);
  color: #f4dfc4;
  cursor: pointer;
  transition: transform 0.12s, opacity 0.12s;
}

.btn-evidence:hover { transform: translateY(-1px); }

.btn-evidence.submitted {
  background: rgba(72, 106, 61, 0.18);
  border-color: rgba(72, 106, 61, 0.45);
  color: #314d26;
}

.btn-evidence.wrong {
  background: var(--accent-soft);
  border-color: rgba(155, 47, 37, 0.45);
  color: #74241c;
}

@media (max-width: 1100px) {
  .mail-view {
    grid-template-columns: 180px 1fr;
    grid-template-rows: auto minmax(0, 1fr);
  }

  .email-viewer {
    grid-column: 1 / -1;
  }
}

@media (max-width: 760px) {
  .mail-view {
    grid-template-columns: 1fr;
    height: auto;
    min-height: 100%;
  }

  .mail-sidebar,
  .email-list {
    max-height: 260px;
  }
}
</style>
