<template>
  <RoomLayout>
    <div class="wiki-view">
      <!-- Sidebar nav -->
      <aside class="wiki-nav">
        <div class="nav-header">NovaWiki</div>
        <div class="nav-search">
          <input v-model="searchTerm" placeholder="Search pages..." />
        </div>
        <div class="nav-section">
          <div class="nav-section-label">Pages</div>
          <div
            v-for="page in filteredPages"
            :key="page.id"
            :class="['nav-item', { active: selectedPage?.id === page.id }]"
            @click="selectedPage = page"
          >
            {{ page.title }}
          </div>
        </div>
      </aside>

      <!-- Page content -->
      <main class="wiki-main">
        <div v-if="loading" class="wiki-loading">
          <span class="wiki-spinner"></span> Loading wiki...
        </div>

        <div v-else-if="!selectedPage" class="wiki-welcome">
          <h1>NovaWiki</h1>
          <p>Select a page from the sidebar to begin your investigation.</p>
        </div>

        <div v-else class="wiki-page">
          <div class="page-header">
            <div class="page-breadcrumb">
              <span>NovaWiki</span>
              <span class="crumb-sep">›</span>
              <span>{{ selectedPage.category }}</span>
              <span class="crumb-sep">›</span>
              <span class="crumb-active">{{ selectedPage.title }}</span>
            </div>
            <h1 class="page-title">{{ selectedPage.title }}</h1>
            <div class="page-meta">
              <span>Last modified: {{ selectedPage.lastModified }}</span>
              <span>Author: {{ selectedPage.author }}</span>
            </div>
          </div>

          <div class="page-body">
            <div v-if="!selectedPage.hasRedacted" class="page-content" v-html="renderContent(selectedPage.content)"></div>

            <template v-else>
              <div class="page-content" v-html="renderContent(selectedPage.content)"></div>
              <div class="redacted-section">
                <div class="redacted-label">
                  🔒 [REDACTED — Security Classification Level 4]
                </div>
                <div class="rot13-block">
                  <div class="rot13-text">{{ selectedPage.redactedSection }}</div>
                  <button class="decode-btn" @click="decodeRot13(selectedPage)">
                    Decode ROT13
                  </button>
                </div>
                <div v-if="decodedContent[selectedPage.id]" class="decoded-block">
                  <div class="decoded-label">✓ Decoded:</div>
                  <div class="decoded-text">{{ decodedContent[selectedPage.id] }}</div>
                </div>
              </div>
            </template>
          </div>
        </div>
      </main>
    </div>
    <NpcChat npc-id="archivist" npc-name="Dr. Patricia Wells" npc-role="Corporate Archivist" />
  </RoomLayout>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import RoomLayout from '../components/RoomLayout.vue'
import NpcChat from '../components/NpcChat.vue'
import { useGameStore } from '../stores/gameStore.js'

const store = useGameStore()
const pages = ref([])
const loading = ref(true)
const selectedPage = ref(null)
const searchTerm = ref('')
const decodedContent = ref({})

onMounted(async () => {
  try {
    const data = await store.enterRoom('wiki')
    pages.value = Array.isArray(data) ? data : []
  } catch (e) {
    pages.value = getDefaultPages()
  } finally {
    loading.value = false
    if (pages.value.length > 0) selectedPage.value = pages.value[0]
  }
})

const filteredPages = computed(() => {
  if (!searchTerm.value) return pages.value
  const t = searchTerm.value.toLowerCase()
  return pages.value.filter(p =>
    p.title.toLowerCase().includes(t) || p.content?.toLowerCase().includes(t)
  )
})

function renderContent(content) {
  if (!content) return ''
  // Simple markdown-ish: bold, italic, headers
  return content
    .replace(/^## (.+)$/gm, '<h2>$1</h2>')
    .replace(/^### (.+)$/gm, '<h3>$1</h3>')
    .replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>')
    .replace(/\*(.+?)\*/g, '<em>$1</em>')
    .replace(/\n/g, '<br/>')
}

function rot13(str) {
  return str.replace(/[a-zA-Z]/g, c => {
    const base = c <= 'Z' ? 65 : 97
    return String.fromCharCode(((c.charCodeAt(0) - base + 13) % 26) + base)
  })
}

function decodeRot13(page) {
  const decoded = rot13(page.redactedSection || '')
  decodedContent.value[page.id] = decoded

  const vaultWord = store.sessionState?.vaultWord3
  if (vaultWord && decoded.toLowerCase().includes(vaultWord.toLowerCase())) {
    store.addClue(
      'wiki-vault-word',
      'NovaWiki',
      `ROT13 decoded text in "${page.title}" contains the keyword: "${vaultWord}".`
    )
    store.markRoomComplete('wiki')
  }
}

function getDefaultPages() {
  return [
    {
      id: 'page-001',
      title: 'Employee Handbook',
      category: 'HR',
      lastModified: '2025-02-01',
      author: 'HR Team',
      content: '## Welcome to NovaCorp\n\nThis handbook outlines your rights and responsibilities as a NovaCorp employee.\n\n### Security Policy\n\nAll employees must badge in/out at all times. Server rooms require Level 3+ clearance.',
      hasRedacted: false,
    },
    {
      id: 'page-002',
      title: 'Server Room Access Protocol',
      category: 'Security',
      lastModified: '2025-01-15',
      author: 'Alex Torres',
      content: '## Access Protocol\n\nServer Room B contains critical infrastructure. Access is restricted to Level 4+ clearance.\n\n### After-Hours Access\n\nAll after-hours access must be pre-approved by the CISO.',
      hasRedacted: true,
      redactedSection: 'Gur inyg pbqr vf cneg bs gur vqragvgl senzrjbex. Gur guveq jbeq vf: vqragvgl',
    },
    {
      id: 'page-003',
      title: 'Project Nova Overview',
      category: 'Executive',
      lastModified: '2025-02-28',
      author: 'Victoria Stone',
      content: '## Project Nova\n\nProject Nova is a classified corporate restructuring initiative.\n\n**Status:** Active\n\nAll documentation is restricted to Executive clearance.',
      hasRedacted: false,
    },
  ]
}
</script>

<style scoped>
/* Wiki uses light mode */
.wiki-view {
  --bg-primary: #ffffff;
  --bg-secondary: #f4f5f7;
  --text-primary: #172b4d;
  --text-secondary: #42526e;
  --text-muted: #6b778c;
  --accent-blue: #0052cc;
  --border-color: #dfe1e6;

  display: grid;
  grid-template-columns: 240px 1fr;
  height: 100%;
  overflow: hidden;
  background: var(--bg-primary);
  color: var(--text-primary);
}

.wiki-nav {
  background: var(--bg-secondary);
  border-right: 1px solid var(--border-color);
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.nav-header {
  padding: 16px;
  font-size: 16px;
  font-weight: 700;
  color: var(--accent-blue);
  border-bottom: 1px solid var(--border-color);
}

.nav-search {
  padding: 12px;
  border-bottom: 1px solid var(--border-color);
}

.nav-search input {
  width: 100%;
  background: #fff;
  border: 1px solid var(--border-color);
  color: var(--text-primary);
  font-size: 13px;
  padding: 6px 10px;
}

.nav-section {
  flex: 1;
  overflow-y: auto;
  padding: 8px 0;
}

.nav-section-label {
  padding: 8px 16px 4px;
  font-size: 11px;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  color: var(--text-muted);
}

.nav-item {
  padding: 8px 16px;
  cursor: pointer;
  font-size: 13px;
  color: var(--text-secondary);
  transition: background var(--transition);
  border-radius: 0;
}

.nav-item:hover {
  background: rgba(0, 82, 204, 0.08);
  color: var(--accent-blue);
}

.nav-item.active {
  background: rgba(0, 82, 204, 0.12);
  color: var(--accent-blue);
  font-weight: 600;
  border-left: 3px solid var(--accent-blue);
}

/* Wiki main */
.wiki-main {
  overflow-y: auto;
  padding: 32px 48px;
}

.wiki-loading {
  display: flex;
  align-items: center;
  gap: 10px;
  color: var(--text-muted);
}

.wiki-spinner {
  display: inline-block;
  width: 18px;
  height: 18px;
  border: 2px solid var(--border-color);
  border-top-color: var(--accent-blue);
  border-radius: 50%;
  animation: spin 0.6s linear infinite;
}

@keyframes spin { to { transform: rotate(360deg); } }

.wiki-welcome {
  text-align: center;
  padding: 48px;
  color: var(--text-muted);
}

.wiki-welcome h1 {
  font-size: 32px;
  color: var(--accent-blue);
  margin-bottom: 12px;
}

.page-breadcrumb {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 12px;
  color: var(--text-muted);
  margin-bottom: 16px;
}

.crumb-sep {
  color: var(--border-color);
}

.crumb-active {
  color: var(--text-secondary);
}

.page-title {
  font-size: 28px;
  font-weight: 700;
  color: var(--text-primary);
  margin-bottom: 12px;
  line-height: 1.3;
}

.page-meta {
  display: flex;
  gap: 24px;
  font-size: 12px;
  color: var(--text-muted);
  border-bottom: 1px solid var(--border-color);
  padding-bottom: 16px;
  margin-bottom: 24px;
}

.page-content :deep(h2) {
  font-size: 20px;
  font-weight: 700;
  color: var(--text-primary);
  margin: 20px 0 12px;
}

.page-content :deep(h3) {
  font-size: 16px;
  font-weight: 600;
  color: var(--text-primary);
  margin: 16px 0 8px;
}

.page-content :deep(strong) {
  font-weight: 700;
}

.redacted-section {
  margin-top: 24px;
  border: 2px dashed #d4a017;
  border-radius: var(--radius);
  padding: 16px;
  background: #fff9e6;
}

.redacted-label {
  font-size: 12px;
  font-weight: 700;
  color: #b8860b;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  margin-bottom: 12px;
}

.rot13-block {
  display: flex;
  align-items: flex-start;
  gap: 12px;
}

.rot13-text {
  flex: 1;
  font-family: var(--font-mono);
  font-size: 13px;
  color: #6b778c;
  word-break: break-all;
  line-height: 1.6;
  background: #f0e6c8;
  padding: 10px;
  border-radius: 4px;
}

.decode-btn {
  background: #d4a017;
  color: #fff;
  font-size: 12px;
  font-weight: 600;
  padding: 6px 12px;
  border-radius: var(--radius);
  white-space: nowrap;
}

.decoded-block {
  margin-top: 12px;
  padding: 12px;
  background: #e6f3ff;
  border-radius: 4px;
  border: 1px solid #b3d4ff;
}

.decoded-label {
  font-size: 11px;
  font-weight: 700;
  color: var(--accent-blue);
  margin-bottom: 6px;
  text-transform: uppercase;
}

.decoded-text {
  font-family: var(--font-mono);
  font-size: 13px;
  color: var(--text-primary);
  line-height: 1.6;
}
</style>
