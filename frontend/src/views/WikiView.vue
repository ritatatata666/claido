<template>
  <RoomLayout>
    <div class="wiki-view">
      <!-- Sidebar nav -->
      <aside class="wiki-nav">
        <div class="nav-header nova-glitch" data-text="NOVAWIKI">NOVAWIKI</div>
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
              <span class="nova-glitch" data-text="NovaWiki">NovaWiki</span>
              <span class="crumb-sep">›</span>
              <span>{{ selectedPage.category }}</span>
              <span class="crumb-sep">›</span>
              <span class="crumb-active">{{ selectedPage.title }}</span>
            </div>
            <h1 class="page-title" :class="{ 'nova-glitch': selectedPage.title.toLowerCase().includes('nova') }" :data-text="selectedPage.title">{{ selectedPage.title }}</h1>
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
const activeVaultWord3 = ref('')

onMounted(async () => {
  const vaultWord3 = resolveVaultWord3()
  activeVaultWord3.value = vaultWord3
  try {
    const data = await store.enterRoom('wiki')
    pages.value = normalizeWikiPages(Array.isArray(data) ? data : [], vaultWord3)
  } catch (e) {
    console.error('Failed to load wiki data:', e)
    pages.value = getDefaultPages(vaultWord3)
  } finally {
    loading.value = false
    if (pages.value.length > 0) selectedPage.value = pages.value[0]
  }
})

function resolveVaultWord3() {
  const fromSession = String(store.sessionState?.vaultWord3 || '').toLowerCase().trim()
  if (fromSession) return fromSession
  const fallback = 'identity'
  console.warn('[NovaWiki] Using fallback vaultWord3 because session value is missing.')
  return fallback
}

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

  const vaultWord = activeVaultWord3.value
  if (vaultWord && decoded.toLowerCase().includes(vaultWord.toLowerCase())) {
    store.addClue(
      'wiki-vault-word',
      'NovaWiki',
      `ROT13 decoded text in "${page.title}" contains the keyword: "${vaultWord}".`
    )
    store.markRoomComplete('wiki')
  }
}

function buildImmersiveDecodedNote(vaultWord3, pageTitle = '') {
  const templates = [
    `Incident addendum: witness statements were anonymized under codename ${vaultWord3} before board review.`,
    `Internal errata: archival cross-reference marks ${vaultWord3} as the identity tag used in sealed incident records.`,
    `Security appendix: operators replaced employee names with token ${vaultWord3} in the restricted transcript.`,
  ]
  const titleSeed = [...pageTitle].reduce((acc, ch) => acc + ch.charCodeAt(0), 0)
  return templates[titleSeed % templates.length]
}

function ensureRedactedContainsVaultWord(page, vaultWord3) {
  if (!vaultWord3) return page
  const decoded = rot13(page.redactedSection || '')
  if (decoded.toLowerCase().includes(vaultWord3)) {
    return page
  }
  console.warn('[NovaWiki] Injected fallback clue word into redacted section to keep puzzle solvable.')
  const forcedSentence = buildImmersiveDecodedNote(vaultWord3, page?.title)
  return {
    ...page,
    hasRedacted: true,
    redactedSection: rot13(forcedSentence),
  }
}

function normalizeWikiPages(rawPages, vaultWord3) {
  const basePages = rawPages.map(p => ({ ...p }))
  if (basePages.length === 0) return getDefaultPages(vaultWord3)

  const redactedIndex = basePages.findIndex(p => p?.hasRedacted)
  if (redactedIndex >= 0) {
    basePages[redactedIndex] = ensureRedactedContainsVaultWord(basePages[redactedIndex], vaultWord3)
    return basePages
  }

  console.warn('[NovaWiki] Missing redacted page in room data; creating one to keep puzzle solvable.')
  const fallbackPage = basePages[0]
  basePages[0] = ensureRedactedContainsVaultWord({
    ...fallbackPage,
    hasRedacted: true,
    redactedSection: '',
  }, vaultWord3)
  return basePages
}

function getDefaultPages(vaultWord3) {
  const safeWord = vaultWord3
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
      redactedSection: rot13(buildImmersiveDecodedNote(safeWord, 'Server Room Access Protocol')),
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
.wiki-view {
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
  grid-template-columns: 240px 1fr;
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
  color: var(--ink-strong);
  font-family: var(--font-mono);
}

.wiki-view::before {
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

.wiki-nav {
  border: 1px solid var(--paper-edge);
  border-radius: 8px;
  background: linear-gradient(180deg, rgba(255, 247, 233, 0.86), rgba(237, 210, 172, 0.78));
  box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.22);
  display: flex;
  flex-direction: column;
  overflow: hidden;
  min-height: 0;
}

.nav-header {
  padding: 12px 14px;
  font-size: 12px;
  font-weight: 700;
  border-bottom: 1px solid var(--line);
  background: rgba(255, 255, 255, 0.3);
  letter-spacing: 2px;
  text-transform: uppercase;
  color: var(--ink-strong);
}

.nav-search {
  padding: 12px;
  border-bottom: 1px solid var(--line-soft);
}

.nav-search input {
  width: 100%;
  font-size: 12px;
  padding: 8px 10px;
  border: 1px solid var(--line-soft);
  border-radius: 6px;
  color: var(--ink-strong);
  background: rgba(255, 255, 255, 0.52);
}

.nav-search input:focus {
  outline: none;
  box-shadow: inset 0 0 0 2px rgba(155, 47, 37, 0.18);
}

.nav-section {
  flex: 1;
  overflow-y: auto;
  padding: 8px;
}

.nav-section-label {
  padding: 8px 6px 4px;
  font-size: 10px;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 1px;
  color: var(--ink-muted);
}

.nav-item {
  padding: 8px 10px;
  margin-bottom: 6px;
  cursor: pointer;
  font-size: 12px;
  color: var(--ink-soft);
  transition: background 0.15s, border-color 0.15s, color 0.15s;
  border-radius: 6px;
  border: 1px solid transparent;
}

.nav-item:hover {
  background: rgba(255, 255, 255, 0.35);
  border-color: var(--line-soft);
  color: var(--ink-strong);
}

.nav-item.active {
  background: rgba(155, 47, 37, 0.12);
  border-color: rgba(155, 47, 37, 0.35);
  color: #6f2018;
  font-weight: 700;
}

.wiki-main {
  border: 1px solid var(--paper-edge);
  border-radius: 8px;
  background: linear-gradient(180deg, rgba(255, 247, 233, 0.86), rgba(237, 210, 172, 0.78));
  box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.22);
  overflow-y: auto;
  padding: 22px 26px;
  min-height: 0;
}

.wiki-loading {
  display: flex;
  align-items: center;
  gap: 8px;
  color: var(--ink-soft);
  font-size: 12px;
}

.wiki-spinner {
  display: inline-block;
  width: 16px;
  height: 16px;
  border: 2px solid var(--line-soft);
  border-top-color: #8e3024;
  border-radius: 50%;
  animation: spin 0.6s linear infinite;
}

@keyframes spin { to { transform: rotate(360deg); } }

.wiki-welcome {
  text-align: center;
  padding: 40px 20px;
  color: var(--ink-muted);
}

.wiki-welcome h1 {
  font-size: 30px;
  color: #5c3720;
  margin-bottom: 12px;
}

.page-breadcrumb {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 12px;
  color: var(--ink-muted);
  margin-bottom: 16px;
}

.crumb-sep {
  color: rgba(110, 75, 47, 0.55);
}

.crumb-active {
  color: var(--ink-soft);
}

.page-title {
  font-size: 26px;
  font-weight: 700;
  color: var(--ink-strong);
  margin-bottom: 12px;
  line-height: 1.3;
}

.page-meta {
  display: flex;
  flex-wrap: wrap;
  gap: 24px;
  font-size: 12px;
  color: var(--ink-muted);
  border-bottom: 1px solid var(--line);
  padding-bottom: 12px;
  margin-bottom: 16px;
}

.page-content {
  color: var(--ink-strong);
  line-height: 1.6;
  font-size: 13px;
}

.page-content :deep(h2) {
  font-size: 18px;
  font-weight: 700;
  color: var(--ink-strong);
  margin: 18px 0 10px;
}

.page-content :deep(h3) {
  font-size: 15px;
  font-weight: 600;
  color: var(--ink-soft);
  margin: 14px 0 8px;
}

.page-content :deep(strong) {
  font-weight: 700;
  color: var(--ink-strong);
}

.page-content :deep(em) {
  color: var(--ink-soft);
}

.page-content :deep(p) {
  color: var(--ink-strong);
  margin-bottom: 12px;
}

.redacted-section {
  margin-top: 18px;
  border: 1px dashed rgba(155, 47, 37, 0.5);
  border-radius: 6px;
  padding: 12px;
  background: var(--accent-soft);
}

.redacted-label {
  font-size: 11px;
  font-weight: 700;
  color: #74241c;
  text-transform: uppercase;
  letter-spacing: 0.8px;
  margin-bottom: 10px;
}

.rot13-block {
  display: flex;
  align-items: flex-start;
  gap: 12px;
}

.rot13-text {
  flex: 1;
  font-size: 12px;
  color: var(--ink-soft);
  word-break: break-all;
  line-height: 1.6;
  background: rgba(255, 255, 255, 0.44);
  padding: 10px;
  border-radius: 6px;
  border: 1px solid var(--line-soft);
}

.decode-btn {
  border: 1px solid rgba(109, 72, 38, 0.45);
  border-radius: 6px;
  background: linear-gradient(180deg, #6e4a2d, #4f3420);
  color: #f4dfc4;
  font-family: var(--font-mono);
  font-size: 12px;
  font-weight: 700;
  padding: 8px 12px;
  white-space: nowrap;
  cursor: pointer;
  transition: transform 0.12s;
}

.decode-btn:hover {
  transform: translateY(-1px);
}

.decoded-block {
  margin-top: 10px;
  padding: 12px;
  background: rgba(72, 106, 61, 0.14);
  border-radius: 6px;
  border: 1px solid rgba(72, 106, 61, 0.35);
}

.decoded-label {
  font-size: 11px;
  font-weight: 700;
  color: #314d26;
  margin-bottom: 6px;
  text-transform: uppercase;
}

.decoded-text {
  font-size: 12px;
  color: #2c431f;
  line-height: 1.6;
}

@media (max-width: 980px) {
  .wiki-view {
    grid-template-columns: 1fr;
    height: auto;
    min-height: 100%;
  }

  .wiki-nav {
    max-height: 260px;
  }
}

@media (max-width: 680px) {
  .wiki-main {
    padding: 16px;
  }

  .rot13-block {
    flex-direction: column;
  }

  .decode-btn {
    width: 100%;
  }
}
</style>