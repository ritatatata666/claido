<template>
  <RoomLayout>
    <div class="db-view">
      <div class="db-sidebar">
        <div class="sidebar-header">NovaCrime DB</div>
        <div class="table-list">
          <div
            v-for="t in tables"
            :key="t"
            :class="['table-item', { active: activeTable === t }]"
            @click="quickQuery(t)"
          >
            <span class="table-icon">▤</span> {{ t }}
          </div>
        </div>
        <div class="db-hint">
          <p>Run SQL queries to investigate. Try:</p>
          <code>SELECT * FROM access_logs WHERE success = 1 ORDER BY timestamp DESC;</code>
        </div>
      </div>

      <div class="db-main">
        <div class="query-panel card">
          <div class="query-header">
            <span class="query-label">SQL Query</span>
            <button class="btn-primary run-btn" @click="runQuery" :disabled="loading || !db">
              {{ loading ? 'Running...' : '▶  Run' }}
            </button>
          </div>
          <textarea
            v-model="sqlInput"
            class="sql-input"
            spellcheck="false"
            placeholder="SELECT * FROM employees;"
            @keydown.ctrl.enter.prevent="runQuery"
          />
        </div>

        <div v-if="loadingDb" class="status-msg">
          <span class="spinner"></span> Loading database...
        </div>

        <div v-if="queryError" class="error-msg">
          {{ queryError }}
        </div>

        <div v-if="results" class="results-panel card">
          <div class="results-header">
            <span>{{ results.rows.length }} row{{ results.rows.length !== 1 ? 's' : '' }}</span>
            <span class="query-time">{{ queryTime }}ms</span>
          </div>
          <div class="table-wrapper">
            <table>
              <thead>
                <tr>
                  <th v-for="col in results.columns" :key="col">{{ col }}</th>
                </tr>
              </thead>
              <tbody>
                <tr
                  v-for="(row, i) in results.rows"
                  :key="i"
                >
                  <td v-for="(cell, j) in row" :key="j">{{ cell }}</td>
                </tr>
              </tbody>
            </table>
          </div>
        </div>

        <div class="flag-panel card">
          <div class="flag-header">Submit Flag</div>
          <div class="flag-body">
            <input
              v-model="flagInput"
              class="flag-input"
              placeholder="Enter the culprit's employee ID..."
              @keydown.enter="submitFlag"
            />
            <button class="btn-primary flag-btn" @click="submitFlag">Submit Flag</button>
          </div>
          <div v-if="flagResult === 'correct'" class="flag-result flag-correct">
            ✓ Correct! Flag accepted.
          </div>
          <div v-else-if="flagResult === 'wrong'" class="flag-result flag-wrong">
            ✗ Incorrect employee ID.
          </div>
        </div>
      </div>
    </div>
    <NpcChat npc-id="archivist" npc-name="Dr. Patricia Wells" npc-role="Corporate Archivist" />
  </RoomLayout>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import RoomLayout from '../components/RoomLayout.vue'
import NpcChat from '../components/NpcChat.vue'
import { useGameStore } from '../stores/gameStore.js'

const store = useGameStore()
const sqlInput = ref('SELECT * FROM employees;')
const results = ref(null)
const queryError = ref('')
const queryTime = ref(0)
const loading = ref(false)
const loadingDb = ref(true)
const db = ref(null)
const tables = ref(['employees', 'access_logs', 'incidents', 'messages'])
const activeTable = ref('')
const flagInput = ref('')
const flagResult = ref(null)

function loadSqlJs() {
  return new Promise((resolve, reject) => {
    if (window.initSqlJs) { resolve(window.initSqlJs); return }
    const script = document.createElement('script')
    script.src = 'https://cdnjs.cloudflare.com/ajax/libs/sql.js/1.12.0/sql-wasm.js'
    script.onload = () => resolve(window.initSqlJs)
    script.onerror = () => reject(new Error('Failed to load sql.js from CDN'))
    document.head.appendChild(script)
  })
}

onMounted(async () => {
  try {
    const data = await store.enterRoom('database')
    const bytes = Uint8Array.from(atob(data.dbBase64), c => c.charCodeAt(0))

    const initSqlJs = await loadSqlJs()
    const SQL = await initSqlJs({
      locateFile: file => `https://cdnjs.cloudflare.com/ajax/libs/sql.js/1.12.0/${file}`,
    })
    db.value = new SQL.Database(bytes)
  } catch (e) {
    console.error('DB load error:', e)
    queryError.value = 'Failed to load database: ' + e.message
  } finally {
    loadingDb.value = false
  }
})

function quickQuery(table) {
  activeTable.value = table
  sqlInput.value = `SELECT * FROM ${table} LIMIT 50;`
}

function runQuery() {
  if (!db.value) return
  queryError.value = ''
  results.value = null
  loading.value = true

  const start = performance.now()
  try {
    const res = db.value.exec(sqlInput.value.trim())
    queryTime.value = Math.round(performance.now() - start)

    if (res.length === 0) {
      results.value = { columns: [], rows: [] }
      return
    }

    const { columns, values } = res[0]
    results.value = { columns, rows: values }
  } catch (e) {
    queryError.value = e.message
  } finally {
    loading.value = false
  }
}

function submitFlag() {
  const culpritId = store.sessionState?.culprit?.id
  if (!culpritId) {
    flagResult.value = 'wrong'
    return
  }
  if (flagInput.value.trim() === String(culpritId)) {
    flagResult.value = 'correct'
    store.addClue(
      'db-culprit-found',
      'NovaCrime DB',
      `Confirmed culprit: Employee ID ${culpritId} identified from database investigation.`
    )
    store.markRoomComplete('database')
  } else {
    flagResult.value = 'wrong'
  }
}

</script>

<style scoped>
.db-view {
  display: grid;
  grid-template-columns: 220px 1fr;
  height: 100%;
  overflow: hidden;
}

.db-sidebar {
  background: var(--bg-secondary);
  border-right: 1px solid var(--border-color);
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.sidebar-header {
  padding: 12px 16px;
  font-size: 11px;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 1px;
  color: var(--text-muted);
  border-bottom: 1px solid var(--border-color);
}

.table-list {
  flex: 1;
  overflow-y: auto;
  padding: 8px 0;
}

.table-item {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 16px;
  font-size: 13px;
  color: var(--text-secondary);
  cursor: pointer;
  transition: background var(--transition);
  font-family: var(--font-mono);
}

.table-item:hover {
  background: var(--bg-surface);
  color: var(--text-primary);
}

.table-item.active {
  background: var(--bg-surface);
  color: #58a6ff;
}

.table-icon {
  color: var(--text-muted);
  font-size: 12px;
}

.db-hint {
  padding: 16px;
  border-top: 1px solid var(--border-color);
}

.db-hint p {
  font-size: 11px;
  color: var(--text-muted);
  margin-bottom: 8px;
}

.db-hint code {
  display: block;
  font-family: var(--font-mono);
  font-size: 11px;
  color: var(--accent-green);
  line-height: 1.5;
  word-break: break-all;
}

.db-main {
  display: flex;
  flex-direction: column;
  padding: 16px;
  gap: 16px;
  overflow: hidden;
}

.query-panel {
  flex-shrink: 0;
  padding: 0;
  overflow: hidden;
}

.query-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 10px 14px;
  border-bottom: 1px solid var(--border-color);
}

.query-label {
  font-size: 11px;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 1px;
  color: var(--text-muted);
}

.run-btn {
  padding: 6px 14px;
  font-size: 13px;
  font-weight: 600;
}

.sql-input {
  width: 100%;
  min-height: 100px;
  font-family: var(--font-mono);
  font-size: 13px;
  border: none;
  border-radius: 0;
  resize: vertical;
  padding: 14px;
  line-height: 1.6;
}

.status-msg {
  display: flex;
  align-items: center;
  gap: 10px;
  color: var(--text-muted);
  font-size: 13px;
}

.error-msg {
  background: rgba(248, 81, 73, 0.1);
  border: 1px solid var(--accent-red);
  border-radius: var(--radius);
  color: var(--accent-red);
  padding: 10px 14px;
  font-family: var(--font-mono);
  font-size: 13px;
}

.results-panel {
  flex: 1;
  min-height: 0;
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.results-header {
  display: flex;
  justify-content: space-between;
  padding: 8px 14px;
  font-size: 12px;
  color: var(--text-muted);
  border-bottom: 1px solid var(--border-color);
}

.query-time {
  font-family: var(--font-mono);
}

.table-wrapper {
  flex: 1;
  overflow: auto;
}

table {
  width: 100%;
  border-collapse: collapse;
  font-size: 13px;
  font-family: var(--font-mono);
}

thead {
  position: sticky;
  top: 0;
  background: var(--bg-surface);
  z-index: 1;
}

th {
  text-align: left;
  padding: 8px 12px;
  font-size: 11px;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  color: var(--text-muted);
  border-bottom: 1px solid var(--border-color);
}

td {
  padding: 7px 12px;
  color: var(--text-secondary);
  border-bottom: 1px solid rgba(48, 54, 61, 0.5);
  max-width: 300px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

tbody tr:hover td {
  background: rgba(255, 255, 255, 0.03);
  color: var(--text-primary);
}

tbody tr:nth-child(even) td {
  background: rgba(255, 255, 255, 0.015);
}

.flag-panel {
  flex-shrink: 0;
  padding: 0;
  overflow: hidden;
}

.flag-header {
  padding: 10px 14px;
  font-size: 11px;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 1px;
  color: var(--text-muted);
  border-bottom: 1px solid var(--border-color);
}

.flag-body {
  display: flex;
  gap: 8px;
  padding: 12px 14px;
}

.flag-input {
  flex: 1;
  font-family: var(--font-mono);
  font-size: 13px;
  padding: 7px 10px;
  background: var(--bg-primary);
  border: 1px solid var(--border-color);
  border-radius: var(--radius);
  color: var(--text-primary);
  outline: none;
}

.flag-input:focus {
  border-color: var(--accent-blue);
}

.flag-btn {
  padding: 7px 16px;
  font-size: 13px;
  font-weight: 600;
  white-space: nowrap;
}

.flag-result {
  padding: 8px 14px 12px;
  font-size: 13px;
  font-weight: 600;
  font-family: var(--font-mono);
}

.flag-correct {
  color: #3fb950;
}

.flag-wrong {
  color: var(--accent-red);
}
</style>
