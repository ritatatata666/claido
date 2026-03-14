<template>
  <RoomLayout>
    <div class="search-view">
      <!-- Left sidebar -->
      <aside class="search-sidebar">
        <div class="sidebar-logo">NovaSearch</div>
        <div class="sidebar-section">
          <div class="sidebar-label">Indices</div>
          <div
            v-for="idx in indices"
            :key="idx"
            :class="['index-item', { active: activeIndex === idx }]"
            @click="activeIndex = idx"
          >
            <span class="index-dot"></span>{{ idx }}
          </div>
        </div>
        <div class="sidebar-section">
          <div class="sidebar-label">Log Level</div>
          <label v-for="level in levels" :key="level" class="level-filter">
            <input type="checkbox" v-model="selectedLevels" :value="level" />
            <span :class="['level-badge', `level-${level.toLowerCase()}`]">{{ level }}</span>
          </label>
        </div>
      </aside>

      <!-- Main -->
      <div class="search-main">
        <!-- Search bar -->
        <div class="search-topbar">
          <div class="search-input-wrap">
            <span class="search-icon">🔍</span>
            <input
              v-model="freeText"
              class="search-input"
              placeholder="Search logs... (e.g. employee_id, service, message)"
            />
          </div>
          <select v-model="timeRange" class="time-select">
            <option value="all">All time</option>
            <option value="incident">Incident window (00:00–03:00)</option>
            <option value="day">March 3rd</option>
          </select>
          <button class="btn-primary search-btn" @click="applyFilter">Search</button>
        </div>

        <!-- Stats row -->
        <div class="stats-row">
          <span class="hit-count">{{ filteredLogs.length }} hits</span>
          <span class="total-count">of {{ logs.length }} total</span>
          <span v-if="loading"><span class="spinner"></span></span>
        </div>

        <!-- Log rows -->
        <div class="log-container">
          <div v-if="filteredLogs.length === 0" class="no-results">
            No matching log entries.
          </div>
          <div
            v-for="log in filteredLogs"
            :key="log.id"
            :class="['log-row', `log-${log.level?.toLowerCase()}`]"
            @click="selectedLog = log"
          >
            <span class="log-ts">{{ log.timestamp }}</span>
            <span :class="['log-level-tag', `tag-${log.level?.toLowerCase()}`]">{{ log.level }}</span>
            <span class="log-service">{{ log.service }}</span>
            <span class="log-user">{{ log.user }}</span>
            <span class="log-msg">{{ log.message }}</span>
          </div>
        </div>

        <!-- Detail panel -->
        <Transition name="fade">
          <div v-if="selectedLog" class="log-detail card">
            <div class="detail-header">
              <span class="detail-title">Log Detail</span>
              <button class="close-detail" @click="selectedLog = null">✕</button>
            </div>
            <div class="detail-fields">
              <div v-for="(val, key) in selectedLog" :key="key" class="detail-field">
                <span class="field-key">{{ key }}</span>
                <span class="field-val">{{ val }}</span>
              </div>
            </div>
          </div>
        </Transition>
      </div>
    </div>
    <NpcChat npc-id="sysadmin" npc-name="Alex Torres" npc-role="Senior Sysadmin" />
  </RoomLayout>
</template>

<script setup>
import { ref, computed, onMounted } from 'vue'
import RoomLayout from '../components/RoomLayout.vue'
import NpcChat from '../components/NpcChat.vue'
import { useGameStore } from '../stores/gameStore.js'

const store = useGameStore()
const logs = ref([])
const loading = ref(true)
const freeText = ref('')
const timeRange = ref('all')
const selectedLevels = ref(['INFO', 'WARN', 'ERROR', 'DEBUG'])
const activeIndex = ref('novacorp-*')
const selectedLog = ref(null)

const indices = ['novacorp-*', 'auth-logs', 'badge-events', 'vault-access']
const levels = ['INFO', 'WARN', 'ERROR', 'DEBUG']

onMounted(async () => {
  try {
    const data = await store.enterRoom('search')
    logs.value = Array.isArray(data) ? data : []
  } catch (e) {
    logs.value = getDefaultLogs()
  } finally {
    loading.value = false
    checkForWhistleblower()
  }
})

const filteredLogs = computed(() => {
  let result = logs.value

  if (!selectedLevels.value.includes('ALL')) {
    result = result.filter(l => selectedLevels.value.includes(l.level))
  }

  if (freeText.value.trim()) {
    const t = freeText.value.trim().toLowerCase()
    result = result.filter(l =>
      Object.values(l).some(v => String(v).toLowerCase().includes(t))
    )
  }

  if (timeRange.value === 'incident') {
    result = result.filter(l => {
      const ts = l.timestamp || ''
      const hour = parseInt(ts.split('T')[1]?.split(':')[0] || '12')
      return hour >= 0 && hour < 3
    })
  }

  if (timeRange.value === 'day') {
    result = result.filter(l => (l.timestamp || '').includes('2025-03-03'))
  }

  return result
})

function applyFilter() {
  // reactivity handles it, but check clue
  checkForWhistleblower()
}

function checkForWhistleblower() {
  const visible = filteredLogs.value
  const whistle = visible.find(l =>
    l.level === 'ERROR' && (l.user === 'whistleblower' || l.message?.toLowerCase().includes('whistleblower'))
  )
  if (whistle) {
    const vaultWord = store.sessionState?.vaultWord4
    if (vaultWord && (whistle.message || '').toLowerCase().includes(vaultWord.toLowerCase())) {
      store.addClue(
        'search-vault-word',
        'NovaSearch',
        `Whistleblower ERROR log contains keyword: "${vaultWord}".`
      )
      store.markRoomComplete('search')
    }
  }
}

function getDefaultLogs() {
  const base = []
  const services = ['auth', 'api', 'db', 'badge', 'mail']
  const lvls = ['INFO', 'INFO', 'INFO', 'WARN', 'DEBUG']
  for (let i = 0; i < 49; i++) {
    base.push({
      id: `log-${String(i + 1).padStart(3, '0')}`,
      timestamp: `2025-03-03T${String(Math.floor(Math.random() * 24)).padStart(2, '0')}:${String(Math.floor(Math.random() * 60)).padStart(2, '0')}:00`,
      level: lvls[Math.floor(Math.random() * lvls.length)],
      service: services[Math.floor(Math.random() * services.length)],
      user: `emp-${1002 + Math.floor(Math.random() * 7)}`,
      message: 'Normal system operation',
      ip: `192.168.1.${Math.floor(Math.random() * 200) + 10}`,
    })
  }
  base.splice(35, 0, {
    id: 'log-036',
    timestamp: '2025-03-03T01:33:00',
    level: 'ERROR',
    service: 'vault',
    user: 'whistleblower',
    message: 'ALERT: Unauthorized vault access by employee 1001 at 01:17. Motive: greed. Keyword: greed',
    ip: '192.168.1.99',
  })
  return base
}
</script>

<style scoped>
.search-view {
  --bg-primary: #0d1117;
  --bg-secondary: #13171e;
  --accent: #00bfb3;
  --accent-secondary: #f04e98;
  --border-color: #1e2530;

  display: grid;
  grid-template-columns: 200px 1fr;
  height: 100%;
  overflow: hidden;
  background: var(--bg-primary);
}

.search-sidebar {
  background: var(--bg-secondary);
  border-right: 1px solid var(--border-color);
  display: flex;
  flex-direction: column;
  overflow-y: auto;
}

.sidebar-logo {
  padding: 14px 16px;
  font-size: 14px;
  font-weight: 700;
  color: #00bfb3;
  border-bottom: 1px solid var(--border-color);
  font-family: var(--font-mono);
}

.sidebar-section {
  padding: 12px 0;
  border-bottom: 1px solid var(--border-color);
}

.sidebar-label {
  padding: 4px 16px 8px;
  font-size: 10px;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 1px;
  color: var(--text-muted);
}

.index-item {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 6px 16px;
  font-size: 12px;
  font-family: var(--font-mono);
  color: var(--text-secondary);
  cursor: pointer;
  transition: background var(--transition);
}

.index-item:hover { background: rgba(0, 191, 179, 0.08); }
.index-item.active { color: #00bfb3; background: rgba(0, 191, 179, 0.1); }

.index-dot {
  width: 6px;
  height: 6px;
  border-radius: 50%;
  background: #00bfb3;
  flex-shrink: 0;
}

.level-filter {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 4px 16px;
  cursor: pointer;
  font-size: 12px;
}

.level-badge {
  font-size: 11px;
  font-weight: 700;
  padding: 1px 6px;
  border-radius: 3px;
}

.level-info  { background: rgba(0, 191, 179, 0.15); color: #00bfb3; }
.level-warn  { background: rgba(210, 153, 34, 0.15); color: var(--accent-orange); }
.level-error { background: rgba(248, 81, 73, 0.15); color: var(--accent-red); }
.level-debug { background: rgba(139, 92, 246, 0.15); color: var(--accent-purple); }

/* Search main */
.search-main {
  display: flex;
  flex-direction: column;
  overflow: hidden;
  min-height: 0;
  position: relative;
}

.search-topbar {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 10px 16px;
  background: var(--bg-secondary);
  border-bottom: 1px solid var(--border-color);
}

.search-input-wrap {
  flex: 1;
  display: flex;
  align-items: center;
  gap: 8px;
  background: var(--bg-primary);
  border: 1px solid var(--border-color);
  border-radius: var(--radius);
  padding: 0 12px;
}

.search-icon {
  font-size: 14px;
}

.search-input {
  flex: 1;
  border: none;
  background: transparent;
  font-family: var(--font-mono);
  font-size: 13px;
  padding: 8px 0;
}

.time-select {
  font-size: 12px;
  padding: 6px 10px;
}

.search-btn {
  background: #00bfb3;
  font-size: 13px;
  padding: 7px 14px;
  white-space: nowrap;
}

.stats-row {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 16px;
  font-size: 12px;
  background: var(--bg-secondary);
  border-bottom: 1px solid var(--border-color);
}

.hit-count {
  font-weight: 700;
  color: #00bfb3;
}

.total-count {
  color: var(--text-muted);
}

/* Log container */
.log-container {
  flex: 1;
  overflow-y: auto;
  font-family: var(--font-mono);
  font-size: 12px;
}

.no-results {
  padding: 24px;
  text-align: center;
  color: var(--text-muted);
  font-style: italic;
}

.log-row {
  display: grid;
  grid-template-columns: 180px 60px 80px 80px 1fr;
  gap: 12px;
  padding: 6px 16px;
  border-bottom: 1px solid rgba(30, 37, 48, 0.8);
  cursor: pointer;
  transition: background var(--transition);
  align-items: center;
}

.log-row:hover {
  background: rgba(255, 255, 255, 0.03);
}

.log-row.log-error {
  background: rgba(248, 81, 73, 0.05);
}

.log-row.log-warn {
  background: rgba(210, 153, 34, 0.04);
}

.log-ts { color: var(--text-muted); }
.log-service { color: #00bfb3; }
.log-user { color: var(--text-secondary); }
.log-msg { color: var(--text-primary); white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }

.log-level-tag {
  font-size: 10px;
  font-weight: 700;
  padding: 2px 5px;
  border-radius: 3px;
  text-align: center;
}

.tag-info  { background: rgba(0, 191, 179, 0.15); color: #00bfb3; }
.tag-warn  { background: rgba(210, 153, 34, 0.15); color: var(--accent-orange); }
.tag-error { background: rgba(248, 81, 73, 0.15); color: var(--accent-red); }
.tag-debug { background: rgba(139, 92, 246, 0.15); color: var(--accent-purple); }

/* Detail panel */
.log-detail {
  position: absolute;
  bottom: 0;
  left: 0;
  right: 0;
  max-height: 40%;
  overflow-y: auto;
  background: var(--bg-secondary);
  border-top: 2px solid #00bfb3;
  z-index: 10;
}

.detail-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 10px 16px;
  border-bottom: 1px solid var(--border-color);
}

.detail-title {
  font-size: 12px;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 1px;
  color: #00bfb3;
}

.close-detail {
  background: transparent;
  color: var(--text-secondary);
  font-size: 14px;
  padding: 2px 6px;
}

.detail-fields {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(280px, 1fr));
  gap: 1px;
  padding: 8px 16px;
}

.detail-field {
  display: flex;
  gap: 12px;
  padding: 6px 0;
  border-bottom: 1px solid rgba(30, 37, 48, 0.5);
  font-family: var(--font-mono);
  font-size: 12px;
}

.field-key {
  color: #00bfb3;
  flex-shrink: 0;
  min-width: 100px;
}

.field-val {
  color: var(--text-secondary);
  word-break: break-all;
}
</style>
