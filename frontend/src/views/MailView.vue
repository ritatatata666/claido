<template>
  <RoomLayout>
    <div class="mail-view">
      <!-- Sidebar -->
      <div class="mail-sidebar">
        <div class="mail-logo">NovaMail</div>
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
              :class="{ submitted: evidenceResult === 'correct', wrong: evidenceResult === 'wrong' }"
              @click="submitEvidence(selectedEmail)"
            >
              <span v-if="evidenceResult === 'correct'">✓ Evidence logged</span>
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
  try {
    const data = await store.enterRoom('mail')
    const raw = Array.isArray(data) ? data : []
    emails.value = raw.map(e => ({ ...e, isFlagged: false }))
  } catch (e) {
    emails.value = getDefaultEmails()
  } finally {
    loading.value = false
  }
})

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

function submitEvidence(email) {
  const vaultWord = store.sessionState?.vaultWord2
  if (vaultWord && email.body?.toLowerCase().includes(vaultWord.toLowerCase())) {
    evidenceResult.value = 'correct'
    store.addClue(
      'mail-vault-word',
      'NovaMail',
      `Suspicious email from ${email.from}: contains the keyword "${vaultWord}".`
    )
    store.markRoomComplete('mail')
  } else {
    evidenceResult.value = 'wrong'
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

function getDefaultEmails() {
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
      body: 'Everything is ready for midnight. Come alone. The word you need is "midnight". Delete this.',
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
  display: grid;
  grid-template-columns: 180px 280px 1fr;
  height: 100%;
  overflow: hidden;
  position: relative;
  background: #0a0a0f;
  font-family: 'Courier New', Courier, monospace;
}

.mail-view::before {
  content: '';
  position: absolute;
  inset: 0;
  background: repeating-linear-gradient(
    0deg,
    transparent,
    transparent 2px,
    rgba(0, 255, 65, 0.015) 2px,
    rgba(0, 255, 65, 0.015) 4px
  );
  pointer-events: none;
  z-index: 999;
}

.mail-sidebar {
  background: #0d0d14;
  border-right: 1px solid var(--border-color);
  display: flex;
  flex-direction: column;
}

.mail-logo {
  padding: 16px;
  font-size: 14px;
  font-weight: 700;
  color: #00ff41;
  border-bottom: 1px solid var(--border-color);
  text-shadow: 0 0 8px rgba(0, 255, 65, 0.4);
  letter-spacing: 2px;
  text-transform: uppercase;
}

.folder-list { padding: 8px 0; }

.folder-item {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 16px;
  cursor: pointer;
  font-size: 13px;
  color: var(--text-secondary);
  transition: background var(--transition);
}

.folder-item:hover { background: rgba(0, 255, 65, 0.05); }
.folder-item.active { background: rgba(0, 255, 65, 0.08); color: #00ff41; border-left: 2px solid #00ff41; }
.folder-icon { font-size: 14px; }

.folder-count {
  margin-left: auto;
  font-size: 11px;
  background: var(--bg-primary);
  color: var(--text-muted);
  padding: 1px 6px;
  border-radius: 10px;
}

.email-list {
  border-right: 1px solid var(--border-color);
  overflow-y: auto;
  display: flex;
  flex-direction: column;
}

.loading-state {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 20px;
  color: var(--text-muted);
  font-size: 13px;
}

.empty-folder {
  padding: 24px;
  text-align: center;
  color: var(--text-muted);
  font-size: 13px;
  font-style: italic;
}

.email-row {
  padding: 12px 16px;
  border-bottom: 1px solid var(--border-color);
  cursor: pointer;
  transition: background var(--transition);
}

.email-row:hover { background: rgba(0, 255, 65, 0.04); }
.email-row.selected { background: rgba(0, 255, 65, 0.08); border-left: 3px solid #00ff41; }

.email-row.unread .email-from,
.email-row.unread .email-subject { font-weight: 700; color: var(--text-primary); }

.email-row-top {
  display: flex;
  align-items: center;
  gap: 4px;
  margin-bottom: 2px;
}

.flag-btn {
  background: none;
  border: none;
  padding: 0 2px;
  cursor: pointer;
  font-size: 12px;
  opacity: 0.3;
  flex-shrink: 0;
  line-height: 1;
}

.flag-btn:hover { opacity: 0.7; }
.flag-btn.flagged { opacity: 1; }

.email-from {
  font-size: 12px;
  color: var(--text-secondary);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
  flex: 1;
  min-width: 0;
}

.email-subject {
  font-size: 13px;
  color: var(--text-secondary);
  margin-bottom: 4px;
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.email-snippet {
  font-size: 12px;
  color: var(--text-muted);
  white-space: nowrap;
  overflow: hidden;
  text-overflow: ellipsis;
}

.email-date { font-size: 11px; color: var(--text-muted); margin-top: 4px; }

.email-viewer { overflow-y: auto; padding: 24px; }

.no-selection {
  display: flex;
  align-items: center;
  justify-content: center;
  height: 100%;
  color: var(--text-muted);
  font-size: 14px;
}

.email-header {
  border-bottom: 1px solid var(--border-color);
  padding-bottom: 16px;
  margin-bottom: 20px;
}

.email-subject-large {
  font-size: 18px;
  font-weight: 700;
  color: var(--text-primary);
  margin-bottom: 12px;
  line-height: 1.4;
}

.email-meta {
  display: flex;
  flex-direction: column;
  gap: 4px;
  font-size: 13px;
  color: var(--text-secondary);
}

.email-body {
  font-size: 14px;
  color: var(--text-primary);
  line-height: 1.8;
  white-space: pre-wrap;
}

.email-actions {
  margin-top: 20px;
  padding-top: 16px;
  border-top: 1px solid var(--border-color);
}

.btn-evidence {
  padding: 8px 18px;
  font-size: 13px;
  font-weight: 600;
  font-family: 'Courier New', Courier, monospace;
  background: rgba(0, 255, 65, 0.08);
  border: 1px solid #00ff41;
  color: #00ff41;
  border-radius: var(--radius);
  cursor: pointer;
  transition: background 0.15s, box-shadow 0.15s;
  letter-spacing: 1px;
}

.btn-evidence:hover { background: rgba(0, 255, 65, 0.16); box-shadow: 0 0 8px rgba(0, 255, 65, 0.3); }
.btn-evidence.submitted { background: rgba(63, 185, 80, 0.1); border-color: var(--accent-green); color: var(--accent-green); }
.btn-evidence.wrong { background: rgba(248, 81, 73, 0.1); border-color: var(--accent-red); color: var(--accent-red); }
</style>
