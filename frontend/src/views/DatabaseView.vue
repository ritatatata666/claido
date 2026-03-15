<template>
  <RoomLayout>
    <div class="db-view">
      <div class="db-sidebar">
        <div class="sidebar-header nova-glitch" data-text="NOVACRIME DB">NOVACRIME DB</div>
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
          <div class="evidence-actions">
            <span v-if="evidenceStatus" class="evidence-status">{{ evidenceStatus }}</span>
            <button
              class="btn-primary submit-btn"
              @click="submitDatabaseEvidence"
              :disabled="loading || !results.rows.length"
            >
              Submit as evidence
            </button>
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
const evidenceStatus = ref('')

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
  evidenceStatus.value = ''

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

function submitDatabaseEvidence() {
  if (!results.value) {
    evidenceStatus.value = 'Run a query first.'
    return
  }

  const found = checkForClue(results.value.columns, results.value.rows)
  evidenceStatus.value = found
    ? 'Evidence logged. The vault word trail is now complete.'
    : 'No compelling evidence in those results. Focus on incidents or messages.'
}

function checkForClue(columns, values) {
  const culpritId = store.sessionState?.culprit?.id
  if (!culpritId) return false

  const queryText = sqlInput.value?.toLowerCase?.() ?? ''
  const clueTables = ['incidents', 'messages']
  if (!clueTables.some(table => queryText.includes(table))) {
    return false
  }

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
      return true
    }
  }
  return false
}

function isSuspicious(row) {
  const culpritId = store.sessionState?.culprit?.id
  if (!culpritId) return false
  return row.some(cell => String(cell) === String(culpritId))
}
</script>

<style scoped>
.db-view {
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
  gap: 14px;
  padding: 14px;
  border-radius: 10px;
  border: 1px solid var(--paper-edge);
  background: var(--paper-bg);
  box-shadow:
    0 10px 24px rgba(42, 24, 10, 0.26),
    inset 0 1px 0 rgba(255, 255, 255, 0.2);
  font-family: var(--font-mono);
}

.db-view::before {
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

.db-sidebar {
  border: 1px solid var(--paper-edge);
  border-radius: 8px;
  background: linear-gradient(180deg, rgba(255, 248, 235, 0.78), rgba(235, 208, 169, 0.7));
  display: flex;
  flex-direction: column;
  overflow: hidden;
  min-height: 0;
}

.sidebar-header {
  padding: 12px 14px;
  font-size: 12px;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 2px;
  color: var(--ink-strong);
  border-bottom: 1px solid var(--line);
  background: rgba(255, 255, 255, 0.3);
}

.table-list {
  flex: 1;
  overflow-y: auto;
  padding: 8px;
}

.table-item {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 9px 10px;
  margin-bottom: 6px;
  border-radius: 6px;
  border: 1px solid transparent;
  font-size: 12px;
  color: var(--ink-soft);
  cursor: pointer;
  transition: background 0.15s, border-color 0.15s, color 0.15s;
}

.table-item:hover {
  background: rgba(255, 255, 255, 0.35);
  border-color: var(--line-soft);
  color: var(--ink-strong);
}

.table-item.active {
  background: rgba(155, 47, 37, 0.12);
  border-color: rgba(155, 47, 37, 0.35);
  color: #6f2018;
  font-weight: 700;
}

.table-icon {
  color: var(--ink-muted);
  font-size: 11px;
}

.db-hint {
  padding: 12px;
  border-top: 1px dashed var(--line);
  background: rgba(255, 255, 255, 0.28);
}

.db-hint p {
  margin: 0 0 7px;
  font-size: 11px;
  color: var(--ink-muted);
}

.db-hint code {
  display: block;
  padding: 8px;
  border-radius: 6px;
  border: 1px solid var(--line-soft);
  background: rgba(65, 39, 23, 0.06);
  color: #5f3923;
  font-size: 11px;
  line-height: 1.45;
  word-break: break-word;
}

.db-main {
  display: flex;
  flex-direction: column;
  gap: 12px;
  min-height: 0;
}

.query-panel,
.results-panel {
  border: 1px solid var(--paper-edge);
  border-radius: 8px;
  overflow: hidden;
  background: linear-gradient(180deg, rgba(255, 247, 233, 0.85), rgba(237, 210, 172, 0.78));
  box-shadow: inset 0 1px 0 rgba(255, 255, 255, 0.22);
}

.query-panel {
  flex-shrink: 0;
}

.query-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 10px 12px;
  border-bottom: 1px solid var(--line);
  background: rgba(255, 255, 255, 0.35);
}

.query-label {
  font-size: 11px;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 1.4px;
  color: var(--ink-muted);
}

.run-btn,
.submit-btn {
  padding: 7px 12px;
  border: 1px solid rgba(109, 72, 38, 0.45);
  border-radius: 6px;
  background: linear-gradient(180deg, #6e4a2d, #4f3420);
  color: #f4dfc4;
  font-family: var(--font-mono);
  font-size: 12px;
  font-weight: 700;
  letter-spacing: 0.3px;
  cursor: pointer;
  transition: transform 0.12s, opacity 0.12s;
}

.run-btn:disabled,
.submit-btn:disabled {
  opacity: 0.5;
  cursor: not-allowed;
}

.run-btn:not(:disabled):hover,
.submit-btn:not(:disabled):hover {
  transform: translateY(-1px);
}

.sql-input {
  width: 100%;
  min-height: 108px;
  padding: 12px;
  border: none;
  resize: vertical;
  font-size: 13px;
  line-height: 1.6;
  color: #3f2817;
  background: rgba(255, 252, 246, 0.72);
}

.sql-input:focus {
  outline: none;
  box-shadow: inset 0 0 0 2px rgba(155, 47, 37, 0.18);
}

.status-msg {
  display: inline-flex;
  align-items: center;
  gap: 8px;
  padding: 8px 10px;
  border-radius: 6px;
  border: 1px dashed var(--line);
  color: var(--ink-soft);
  font-size: 12px;
  background: rgba(255, 255, 255, 0.3);
}

.error-msg {
  padding: 9px 11px;
  border-radius: 6px;
  border: 1px solid rgba(155, 47, 37, 0.45);
  color: #74241c;
  background: var(--accent-soft);
  font-size: 12px;
}

.results-panel {
  flex: 1;
  min-height: 0;
  display: flex;
  flex-direction: column;
}

.evidence-actions {
  display: flex;
  align-items: center;
  gap: 10px;
  justify-content: flex-end;
  padding: 10px 12px;
  border-top: 1px solid var(--line);
  background: rgba(255, 255, 255, 0.32);
}

.evidence-status {
  margin-right: auto;
  font-size: 11px;
  color: var(--ink-soft);
}

.results-header {
  display: flex;
  justify-content: space-between;
  padding: 8px 12px;
  border-bottom: 1px solid var(--line);
  background: rgba(255, 255, 255, 0.35);
  font-size: 12px;
  color: var(--ink-muted);
}

.query-time {
  font-variant-numeric: tabular-nums;
}

.table-wrapper {
  flex: 1;
  overflow: auto;
  min-height: 0;
}

table {
  width: 100%;
  border-collapse: collapse;
  font-size: 12px;
  color: var(--ink-strong);
}

thead {
  position: sticky;
  top: 0;
  z-index: 1;
  background: rgba(219, 187, 144, 0.94);
}

th {
  text-align: left;
  padding: 8px 11px;
  font-size: 10px;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 1px;
  color: var(--ink-muted);
  border-bottom: 1px solid var(--line);
}

td {
  padding: 7px 11px;
  border-bottom: 1px solid var(--line-soft);
  max-width: 300px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

tbody tr:nth-child(even) td {
  background: rgba(255, 250, 242, 0.42);
}

tbody tr:hover td {
  background: rgba(255, 255, 255, 0.44);
}

.row-highlight td {
  background: rgba(155, 47, 37, 0.12) !important;
  color: #6f2018 !important;
  font-weight: 700;
}

.submit-btn {
  background: linear-gradient(180deg, #8e3024, #6d2118);
  border-color: rgba(109, 33, 24, 0.58);
}

@media (max-width: 980px) {
  .db-view {
    grid-template-columns: 1fr;
    height: auto;
    min-height: 100%;
  }

  .db-sidebar {
    max-height: 230px;
  }
}

@media (max-width: 680px) {
  .query-header,
  .results-header,
  .evidence-actions {
    flex-wrap: wrap;
    gap: 8px;
  }

  .run-btn,
  .submit-btn {
    width: 100%;
    justify-content: center;
  }
}
</style>
