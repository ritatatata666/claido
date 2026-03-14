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
                  :class="{ 'row-highlight': isSuspicious(row) }"
                >
                  <td v-for="(cell, j) in row" :key="j">{{ cell }}</td>
                </tr>
              </tbody>
            </table>
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
import wasmUrl from '/sql-wasm.wasm?url'

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

onMounted(async () => {
  try {
    const data = await store.enterRoom('database')
    const bytes = Uint8Array.from(atob(data.dbBase64), c => c.charCodeAt(0))

    // Dynamically load sql.js
    const initSqlJs = (await import('sql.js')).default
    const SQL = await initSqlJs({
      locateFile: () => wasmUrl,
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
  runQuery()
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

    // Check if culprit is visible in results
    checkForClue(columns, values)
  } catch (e) {
    queryError.value = e.message
  } finally {
    loading.value = false
  }
}

function checkForClue(columns, values) {
  const culpritId = store.sessionState?.culprit?.id
  if (!culpritId) return

  const flat = values.flat().map(String)
  if (flat.includes(String(culpritId))) {
    const idCol = columns.findIndex(c =>
      c === 'id' || c === 'employee_id' || c === 'related_employee_id'
    )
    if (idCol >= 0) {
      store.addClue(
        'db-culprit-found',
        'NovaCrime DB',
        `Employee ID ${culpritId} appears in query results — cross-reference with incident logs.`
      )
      store.markRoomComplete('database')
    }
  }
}

function isSuspicious(row) {
  const culpritId = store.sessionState?.culprit?.id
  if (!culpritId) return false
  return row.some(cell => String(cell) === String(culpritId))
}
</script>

<style scoped>
.db-view {
  display: grid;
  grid-template-columns: 220px 1fr;
  height: 100%;
  overflow: hidden;
  position: relative;
  background: #0a0a0f;
  font-family: 'Courier New', Courier, monospace;
}

.db-view::before {
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

.db-sidebar {
  background: #0d0d14;
  border-right: 1px solid var(--border-color);
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.sidebar-header {
  padding: 12px 16px;
  font-size: 13px;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 2px;
  color: #00ff41;
  border-bottom: 1px solid var(--border-color);
  text-shadow: 0 0 8px rgba(0, 255, 65, 0.4);
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
  background: rgba(0, 255, 65, 0.05);
  color: var(--text-primary);
}

.table-item.active {
  background: rgba(0, 255, 65, 0.08);
  color: #00ff41;
  border-left: 2px solid #00ff41;
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

.row-highlight td {
  background: rgba(248, 81, 73, 0.08) !important;
  color: var(--accent-red) !important;
}
</style>
