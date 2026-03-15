<template>
  <RoomLayout>
    <div class="search-view">
      <!-- Left sidebar -->
      <aside class="search-sidebar">
        <div class="sidebar-logo">
          <span class="logo-diamond">◆</span> <span class="nova-glitch" data-text="NOVASEARCH">NOVASEARCH</span>
        </div>

        <!-- Selected Fields -->
        <div class="sidebar-section" v-if="visibleColumns.length">
          <div class="sidebar-label">Selected Fields</div>
          <div
            v-for="col in visibleColumns"
            :key="'sel-' + col"
            class="field-item selected-field"
          >
            <span class="field-type-icon">{{ getFieldIcon(col) }}</span>
            <span class="field-name">{{ col }}</span>
            <button class="field-action-btn remove-col-btn" title="Remove column" @click="toggleColumn(col)">✕</button>
          </div>
        </div>

        <!-- Available Fields -->
        <div class="sidebar-section">
          <div class="sidebar-label">Available Fields</div>
          <div
            v-for="field in fieldMeta"
            :key="field.name"
            class="field-item-wrap"
          >
            <div
              :class="['field-item', { expanded: expandedField === field.name }]"
              @click="expandedField = expandedField === field.name ? null : field.name"
            >
              <span class="field-type-icon">{{ field.icon }}</span>
              <span class="field-name">{{ field.name }}</span>
              <span class="field-unique-count">{{ getFieldUniqueCount(field.name) }}</span>
              <button
                v-if="!visibleColumns.includes(field.name)"
                class="field-action-btn add-col-btn"
                title="Toggle column in table"
                @click.stop="toggleColumn(field.name)"
              >⊞</button>
            </div>
            <Transition name="slide">
              <div v-if="expandedField === field.name" class="field-values">
                <div
                  v-for="v in getTopValues(field.name)"
                  :key="v.value"
                  class="field-value-row"
                >
                  <div class="value-bar-bg">
                    <div class="value-bar" :style="{ width: v.pct + '%' }"></div>
                  </div>
                  <span class="value-label" :title="v.value">{{ v.value }}</span>
                  <span class="value-count">{{ v.count }}</span>
                  <span class="value-actions">
                    <button class="val-action-btn val-plus" title="Filter for value" @click.stop="addPill(field.name, String(v.value), false)">+</button>
                    <button class="val-action-btn val-minus" title="Filter out value" @click.stop="addPill(field.name, String(v.value), true)">−</button>
                  </span>
                </div>
              </div>
            </Transition>
          </div>
        </div>
      </aside>

      <!-- Main -->
      <div class="search-main">
        <!-- Search bar -->
        <div class="search-topbar">
          <div class="search-input-wrap">
            <span class="kql-badge">KQL</span>
            <input
              ref="searchInputEl"
              v-model="freeText"
              class="search-input"
              placeholder="Search… e.g. level:ERROR AND service:vault"
              @keydown.enter.prevent="acceptSuggestionOrSearch"
              @keydown.escape="showAutocomplete = false"
              @keydown.down.prevent="moveAutocomplete(1)"
              @keydown.up.prevent="moveAutocomplete(-1)"
              @keydown.tab.prevent="tabCompleteSuggestion"
              @input="onSearchInput"
              @focus="onSearchFocus"
              @blur="hideAutocompleteDelayed"
              autocomplete="off"
            />
            <Transition name="fade">
              <div v-if="showAutocomplete && autocompleteSuggestions.length" class="autocomplete-dropdown">
                <div class="ac-section-label" v-if="acSectionLabel">{{ acSectionLabel }}</div>
                <div
                  v-for="(s, i) in autocompleteSuggestions"
                  :key="i"
                  :class="['ac-item', { active: autocompleteIndex === i }]"
                  @mousedown.prevent="acceptSuggestion(s)"
                >
                  <span class="ac-icon">{{ s.icon }}</span>
                  <span class="ac-label">{{ s.label }}</span>
                  <span class="ac-type" v-if="s.type">{{ s.type }}</span>
                  <span v-if="s.count" class="ac-count">{{ s.count }}</span>
                </div>
              </div>
            </Transition>
          </div>
          <select v-model="timeRange" class="time-select">
            <option value="all">Last 15 minutes</option>
            <option value="incident">00:00 – 03:00</option>
            <option value="day">March 3rd</option>
            <option value="all">All time</option>
          </select>
          <button class="search-btn" @click="applyFilter">
            <span class="search-btn-icon">↻</span> Refresh
          </button>
        </div>

        <!-- Filter pills bar -->
        <div v-if="filterPills.length" class="filter-pills-bar">
          <span
            v-for="pill in filterPills"
            :key="pill.id"
            :class="['filter-pill', { disabled: !pill.enabled, negated: pill.negated }]"
          >
            <button class="pill-toggle" :title="pill.enabled ? 'Disable filter' : 'Enable filter'" @click="togglePill(pill.id)">
              <span :class="['pill-indicator', { on: pill.enabled }]"></span>
            </button>
            <span class="pill-body" @click="togglePillNegate(pill.id)">
              <span class="pill-field">{{ pill.field }}</span>
              <span class="pill-operator">{{ pill.negated ? 'is not' : 'is' }}</span>
              <span class="pill-value">{{ pill.value }}</span>
            </span>
            <button class="pill-remove" title="Remove" @click="removePill(pill.id)">✕</button>
          </span>
          <button class="pills-clear" @click="filterPills = []">Clear all</button>
        </div>

        <!-- Time histogram -->
        <div class="histogram-wrap">
          <div class="histogram">
            <div
              v-for="bar in histogramData"
              :key="bar.hour"
              :class="['hist-bar-wrap', { active: activeHistogramHour === bar.hour }]"
              :title="`${String(bar.hour).padStart(2,'0')}:00 — ${bar.count} logs`"
              @click="toggleHistogramHour(bar.hour)"
            >
              <div class="hist-bar" :style="{ height: bar.pct + '%' }"></div>
            </div>
          </div>
          <div class="histogram-labels">
            <span>00:00</span>
            <span>06:00</span>
            <span>12:00</span>
            <span>18:00</span>
            <span>23:00</span>
          </div>
        </div>

        <!-- Stats row + column picker -->
        <div class="stats-row">
          <span class="hit-count">{{ filteredLogs.length }} hits</span>
          <span class="total-count">of {{ logs.length }} total</span>
          <span v-if="activeHistogramHour !== null" class="active-filter-tag" @click="activeHistogramHour = null">
            hour:{{ String(activeHistogramHour).padStart(2, '0') }} ✕
          </span>
          <span v-if="loading"><span class="spinner"></span></span>
          <div class="spacer"></div>
          <div class="column-picker-wrap">
            <button class="column-picker-btn" @click="showColumnPicker = !showColumnPicker">
              Columns ▾
            </button>
            <Transition name="fade">
              <div v-if="showColumnPicker" class="column-picker-dropdown">
                <label v-for="col in allColumns" :key="col" class="col-option">
                  <input type="checkbox" v-model="visibleColumns" :value="col" />
                  <span>{{ col }}</span>
                </label>
              </div>
            </Transition>
          </div>
        </div>

        <!-- Table header -->
        <div class="log-header-row" :style="{ gridTemplateColumns: gridColumns }">
          <span class="log-expand-col"></span>
          <span v-for="col in visibleColumns" :key="col" class="header-cell">{{ col }}</span>
        </div>

        <!-- Log rows -->
        <div class="log-container">
          <div v-if="filteredLogs.length === 0" class="no-results">
            No matching log entries.
          </div>
          <template v-for="log in filteredLogs" :key="log.id">
            <div
              :class="['log-row', `log-${log.level?.toLowerCase()}`, { expanded: expandedLogId === log.id }]"
              :style="{ gridTemplateColumns: gridColumns }"
              @click="toggleExpand(log.id)"
            >
              <span class="log-expand-icon">{{ expandedLogId === log.id ? '▾' : '▸' }}</span>
              <span
                v-for="col in visibleColumns"
                :key="col"
                :class="cellClass(col)"
              >
                <span v-if="col === 'level'" :class="['log-level-tag', `tag-${(log[col] || '').toLowerCase()}`]">{{ log[col] }}</span>
                <template v-else>{{ log[col] }}</template>
              </span>
            </div>
            <Transition name="expand">
              <div v-if="expandedLogId === log.id" class="log-expanded">
                <div class="expanded-tabs">
                  <button :class="['etab', { active: expandedTab === 'table' }]" @click.stop="expandedTab = 'table'">Table</button>
                  <button :class="['etab', { active: expandedTab === 'json' }]" @click.stop="expandedTab = 'json'">JSON</button>
                </div>
                <table v-if="expandedTab === 'table'" class="expanded-table">
                  <tbody>
                    <tr v-for="key in allColumns" :key="key" class="expanded-row">
                      <td class="expanded-key">{{ key }}</td>
                      <td class="expanded-val">{{ log[key] }}</td>
                      <td class="expanded-actions">
                        <button
                          class="exp-action-btn"
                          title="Filter for value"
                          @click.stop="addPill(String(key), String(log[key]), false)"
                        >🔍+</button>
                        <button
                          class="exp-action-btn"
                          title="Filter out value"
                          @click.stop="addPill(String(key), String(log[key]), true)"
                        >🔍−</button>
                        <button
                          class="exp-action-btn"
                          title="Toggle column"
                          @click.stop="toggleColumn(String(key))"
                        >⊞</button>
                        <button
                          class="exp-action-btn"
                          title="Copy value"
                          @click.stop="copyValue(String(log[key]))"
                        >📋</button>
                      </td>
                    </tr>
                  </tbody>
                </table>
                <pre v-else class="expanded-json">{{ JSON.stringify(log, null, 2) }}</pre>
                <div class="expanded-evidence-row">
                  <button class="submit-evidence-btn" @click.stop="submitSearchEvidenceForLog(log)">
                    ⚑ Submit evidence
                  </button>
                  <span v-if="searchSubmitResultById[log.id] === 'correct'" class="submit-correct">✓ Evidence logged</span>
                  <span v-else-if="searchSubmitResultById[log.id] === 'wrong'" class="submit-wrong">✗ This row is not the whistleblower clue.</span>
                </div>
              </div>
            </Transition>
          </template>
        </div>
      </div>
    </div>
    <NpcChat npc-id="sysadmin" npc-name="Alex Torres" npc-role="Senior Sysadmin" />
  </RoomLayout>
</template>

<script setup>
import { ref, computed, onMounted, nextTick } from 'vue'
import MiniSearch from 'minisearch'
import RoomLayout from '../components/RoomLayout.vue'
import NpcChat from '../components/NpcChat.vue'
import { useGameStore } from '../stores/gameStore.js'

const store = useGameStore()
const logs = ref([])
const loading = ref(true)
const freeText = ref('')
const timeRange = ref('all')
const activeHistogramHour = ref(null)
const expandedField = ref(null)
const filterPills = ref([])
const expandedLogId = ref(null)
const expandedTab = ref('table')
const visibleColumns = ref(['timestamp', 'level', 'service', 'user', 'message'])
const showColumnPicker = ref(false)
const searchInputEl = ref(null)
const searchSubmitResultById = ref({})
const activeVaultWord4 = ref('')
const activeWhistleblowerUserId = ref('')

const showAutocomplete = ref(false)
const autocompleteIndex = ref(-1)

const fieldMeta = [
  { name: 'timestamp', type: 'date', icon: '📅' },
  { name: 'level', type: 'keyword', icon: '🏷' },
  { name: 'service', type: 'keyword', icon: '⚙' },
  { name: 'user', type: 'keyword', icon: '👤' },
  { name: 'message', type: 'text', icon: '📝' },
  { name: 'ip', type: 'ip', icon: '🌐' },
]

const allColumns = ['id', 'timestamp', 'level', 'service', 'user', 'message', 'ip']
const validFields = allColumns

const columnWidths = {
  id: '70px',
  timestamp: '170px',
  level: '60px',
  service: '80px',
  user: '100px',
  message: '1fr',
  ip: '120px',
}

let pillIdCounter = 0
let miniSearch = null

function getFieldIcon(name) {
  return fieldMeta.find(f => f.name === name)?.icon || '🏷'
}

function buildIndex(entries) {
  miniSearch = new MiniSearch({
    fields: ['level', 'service', 'user', 'message', 'ip'],
    storeFields: ['id'],
    searchOptions: {
      boost: { message: 2, user: 1.5 },
      fuzzy: 0.2,
      prefix: true,
    },
  })
  miniSearch.addAll(entries.map(e => ({ ...e, user: e.user ?? '' })))
}

function parseKQL(query) {
  const fieldClauses = []
  const freeTerms = []
  const tokenPattern = /(\bAND\b|\bOR\b|\bNOT\b)|(\w+)\s*:\s*(?:"([^"]+)"|([^\s()]+))/gi
  let remaining = query
  let match
  let pendingOperator = 'AND'
  let negateNext = false

  while ((match = tokenPattern.exec(query)) !== null) {
    const operatorToken = match[1]?.toUpperCase()
    if (operatorToken) {
      if (operatorToken === 'NOT') {
        negateNext = true
      } else {
        pendingOperator = operatorToken
      }
      remaining = remaining.replace(match[0], ' ')
      continue
    }

    const field = match[2]?.toLowerCase()
    const value = (match[3] || match[4] || '').trim()
    if (field && value && validFields.includes(field)) {
      fieldClauses.push({
        field,
        value,
        operator: pendingOperator,
        negated: negateNext,
      })
      pendingOperator = 'AND'
      negateNext = false
    }
    remaining = remaining.replace(match[0], ' ')
  }

  remaining = remaining.replace(/\b(AND|OR|NOT)\b/gi, ' ').trim()
  if (remaining) freeTerms.push(remaining)
  return { fieldClauses, freeText: freeTerms.join(' ').trim() }
}

function applyFieldClauses(items, clauses) {
  if (!clauses.length) return items

  return items.filter(item => {
    let accumulator = null
    for (const clause of clauses) {
      const raw = String(item[clause.field] || '').toLowerCase()
      const target = clause.value.toLowerCase()
      let match = raw.includes(target)
      if (clause.negated) match = !match

      if (accumulator === null) {
        accumulator = match
      } else if (clause.operator === 'OR') {
        accumulator = accumulator || match
      } else {
        accumulator = accumulator && match
      }
    }
    return accumulator ?? true
  })
}

const gridColumns = computed(() => {
  const cols = visibleColumns.value.map(c => columnWidths[c] || '1fr')
  return '28px ' + cols.join(' ')
})

function cellClass(col) {
  return {
    timestamp: 'cell-ts',
    level: 'cell-level',
    service: 'cell-service',
    user: 'cell-user',
    message: 'cell-msg',
    ip: 'cell-ip',
    id: 'cell-id',
  }[col] || ''
}

function toggleColumn(col) {
  const idx = visibleColumns.value.indexOf(col)
  if (idx >= 0) {
    visibleColumns.value.splice(idx, 1)
  } else {
    visibleColumns.value.push(col)
  }
}

function getFieldUniqueCount(fieldName) {
  const set = new Set()
  for (const log of logs.value) set.add(log[fieldName])
  return set.size
}

function getTopValues(fieldName) {
  const counts = {}
  for (const log of logs.value) {
    const v = String(log[fieldName] || '')
    counts[v] = (counts[v] || 0) + 1
  }
  const sorted = Object.entries(counts)
    .map(([value, count]) => ({ value, count }))
    .sort((a, b) => b.count - a.count)
    .slice(0, 8)
  const max = sorted[0]?.count || 1
  return sorted.map(s => ({ ...s, pct: (s.count / max) * 100 }))
}

function getValueSuggestions(fieldName, partial = '') {
  const counts = {}
  for (const log of logs.value) {
    const v = String(log[fieldName] || '')
    counts[v] = (counts[v] || 0) + 1
  }

  const normalized = partial.toLowerCase()
  return Object.entries(counts)
    .map(([value, count]) => ({ value, count }))
    .filter(v => !normalized || v.value.toLowerCase().includes(normalized))
    .sort((a, b) => b.count - a.count || a.value.localeCompare(b.value))
    .slice(0, 50)
}

function addPill(field, value, negated = false) {
  if (filterPills.value.some(p => p.field === field && p.value === value && p.negated === negated)) return
  filterPills.value.push({ id: ++pillIdCounter, field, value, negated, enabled: true })
}

function removePill(id) {
  filterPills.value = filterPills.value.filter(p => p.id !== id)
}

function togglePill(id) {
  const pill = filterPills.value.find(p => p.id === id)
  if (pill) pill.enabled = !pill.enabled
}

function togglePillNegate(id) {
  const pill = filterPills.value.find(p => p.id === id)
  if (pill) pill.negated = !pill.negated
}

function copyValue(val) {
  navigator.clipboard?.writeText(val)
}

function toggleExpand(logId) {
  if (expandedLogId.value === logId) {
    expandedLogId.value = null
  } else {
    expandedLogId.value = logId
    expandedTab.value = 'table'
  }
}

function getValueSuggestionContext(input) {
  const tokens = input.split(/\s+/)
  const lastToken = tokens[tokens.length - 1] || ''

  if (lastToken.includes(':')) {
    const colonIdx = lastToken.indexOf(':')
    const field = lastToken.slice(0, colonIdx).toLowerCase()
    const partial = lastToken.slice(colonIdx + 1).trim().toLowerCase()
    if (validFields.includes(field)) {
      return { field, partial, mode: 'inline' }
    }
  }

  const prevToken = tokens[tokens.length - 2] || ''
  if (prevToken.endsWith(':')) {
    const field = prevToken.slice(0, -1).toLowerCase()
    if (validFields.includes(field)) {
      return {
        field,
        partial: lastToken.toLowerCase(),
        mode: lastToken ? 'separate' : 'append',
      }
    }
  }

  return null
}

const acSectionLabel = computed(() => {
  const input = freeText.value
  if (!input) return 'Fields'
  if (getValueSuggestionContext(input)) return 'Values'
  return 'Fields'
})

const autocompleteSuggestions = computed(() => {
  const input = freeText.value
  if (!input) {
    return fieldMeta.map(f => ({
      label: f.name,
      replaceToken: f.name + ': ',
      icon: f.icon,
      type: f.type,
      count: null,
    }))
  }

  const tokens = input.split(/\s+/)
  const lastToken = tokens[tokens.length - 1] || ''

  const valueContext = getValueSuggestionContext(input)
  if (valueContext) {
    return getValueSuggestions(valueContext.field, valueContext.partial)
      .map(v => {
        const formattedValue = v.value.includes(' ') ? '"' + v.value + '"' : v.value
        if (valueContext.mode === 'inline') {
          return {
            label: v.value,
            replaceToken: valueContext.field + ': ' + formattedValue,
            replaceMode: 'replace-last',
            icon: fieldMeta.find(f => f.name === valueContext.field)?.icon || '🏷',
            type: null,
            count: v.count,
          }
        }
        if (valueContext.mode === 'separate') {
          return {
            label: v.value,
            replaceToken: formattedValue,
            replaceMode: 'replace-last',
            icon: fieldMeta.find(f => f.name === valueContext.field)?.icon || '🏷',
            type: null,
            count: v.count,
          }
        }
        return {
          label: v.value,
          replaceToken: formattedValue,
          replaceMode: 'append',
          icon: fieldMeta.find(f => f.name === valueContext.field)?.icon || '🏷',
          type: null,
          count: v.count,
        }
      })
      .slice(0, 10)
  }

  if (!lastToken) {
    return [
      { label: 'AND', replaceToken: 'AND ', icon: '⊕', type: 'operator', count: null },
      { label: 'OR', replaceToken: 'OR ', icon: '⊕', type: 'operator', count: null },
      { label: 'NOT', replaceToken: 'NOT ', icon: '⊖', type: 'operator', count: null },
      ...fieldMeta.map(f => ({
        label: f.name,
        replaceToken: f.name + ': ',
        icon: f.icon,
        type: f.type,
        count: null,
      })),
    ]
  }

  const lower = lastToken.toLowerCase()
  const operators = ['AND', 'OR', 'NOT'].filter(op => op.toLowerCase().startsWith(lower) && op.toLowerCase() !== lower)
  const fields = fieldMeta.filter(f => f.name.startsWith(lower))

  return [
    ...operators.map(op => ({
      label: op,
      replaceToken: op + ' ',
      icon: op === 'NOT' ? '⊖' : '⊕',
      type: 'operator',
      count: null,
    })),
    ...fields.map(f => ({
      label: f.name,
      replaceToken: f.name + ': ',
      icon: f.icon,
      type: f.type,
      count: null,
    })),
  ]
})

function onSearchInput() {
  autocompleteIndex.value = -1
  showAutocomplete.value = true
}

function onSearchFocus() {
  autocompleteIndex.value = -1
  showAutocomplete.value = true
}

function moveAutocomplete(dir) {
  const len = autocompleteSuggestions.value.length
  if (!len) return
  autocompleteIndex.value = (autocompleteIndex.value + dir + len) % len
}

function acceptSuggestion(s) {
  const tokens = freeText.value.split(/\s+/)
  if (s.replaceMode === 'append') {
    freeText.value = (freeText.value || '') + s.replaceToken
  } else if (s.replaceMode === 'replace-last') {
    if (!freeText.value) {
      freeText.value = s.replaceToken
    } else {
      tokens[tokens.length - 1] = s.replaceToken
      freeText.value = tokens.join(' ')
    }
  } else if (!freeText.value || freeText.value.endsWith(' ')) {
    freeText.value = (freeText.value || '') + s.replaceToken
  } else {
    tokens[tokens.length - 1] = s.replaceToken
    freeText.value = tokens.join(' ')
  }
  showAutocomplete.value = false
  autocompleteIndex.value = -1
  nextTick(() => searchInputEl.value?.focus())
}

function tabCompleteSuggestion() {
  if (autocompleteSuggestions.value.length) {
    const idx = autocompleteIndex.value >= 0 ? autocompleteIndex.value : 0
    acceptSuggestion(autocompleteSuggestions.value[idx])
  }
}

function acceptSuggestionOrSearch() {
  if (showAutocomplete.value && autocompleteIndex.value >= 0) {
    acceptSuggestion(autocompleteSuggestions.value[autocompleteIndex.value])
  } else {
    showAutocomplete.value = false
    applyFilter()
  }
}

function hideAutocompleteDelayed() {
  setTimeout(() => { showAutocomplete.value = false }, 150)
}

onMounted(async () => {
  const vaultWord4 = resolveVaultWord4()
  const whistleblowerUserId = resolveWhistleblowerUserId()
  activeVaultWord4.value = vaultWord4
  activeWhistleblowerUserId.value = whistleblowerUserId
  try {
    const data = await store.enterRoom('search')
    logs.value = normalizeSearchLogs(Array.isArray(data) ? data : [], vaultWord4, whistleblowerUserId)
  } catch (e) {
    logs.value = getDefaultLogs(vaultWord4, whistleblowerUserId)
  } finally {
    buildIndex(logs.value)
    loading.value = false
  }
})

function resolveVaultWord4() {
  const fromSession = String(store.sessionState?.vaultWord4 || '').toLowerCase().trim()
  if (fromSession) return fromSession
  const fallback = 'greed'
  console.warn('[NovaSearch] Using fallback vaultWord4 because session value is missing.')
  return fallback
}

function resolveWhistleblowerUserId() {
  const employees = Array.isArray(store.sessionState?.employees) ? store.sessionState.employees : []
  const culpritId = Number(store.sessionState?.culprit?.id)
  const witness = employees.find(e => Number(e?.id) && Number(e.id) !== culpritId)
  if (witness?.id) return `${witness.id}`
  return '1099'
}

function getHour(log) {
  const ts = log.timestamp || ''
  return parseInt(ts.split('T')[1]?.split(':')[0] || '-1')
}

const histogramData = computed(() => {
  const hours = Array.from({ length: 24 }, (_, i) => ({ hour: i, count: 0 }))
  for (const log of logs.value) {
    const h = getHour(log)
    if (h >= 0 && h < 24) hours[h].count++
  }
  const max = Math.max(...hours.map(h => h.count), 1)
  return hours.map(h => ({ ...h, pct: (h.count / max) * 100 }))
})

const filteredLogs = computed(() => {
  let result = [...logs.value]

  for (const pill of filterPills.value) {
    if (!pill.enabled) continue
    const val = pill.value.toLowerCase()
    if (pill.negated) {
      result = result.filter(l => String(l[pill.field] || '').toLowerCase() !== val)
    } else {
      result = result.filter(l => String(l[pill.field] || '').toLowerCase() === val)
    }
  }

  if (timeRange.value === 'incident') {
    result = result.filter(l => { const h = getHour(l); return h >= 0 && h < 3 })
  } else if (timeRange.value === 'day') {
    result = result.filter(l => (l.timestamp || '').includes('2025-03-03'))
  }

  // Histogram hour
  if (activeHistogramHour.value !== null) {
    result = result.filter(l => getHour(l) === activeHistogramHour.value)
  }

  // KQL + MiniSearch
  const query = freeText.value.trim()
  if (query && miniSearch) {
    const { fieldClauses, freeText: searchText } = parseKQL(query)
    result = applyFieldClauses(result, fieldClauses)
    if (searchText) {
      const hits = miniSearch.search(searchText)
      const hitIds = new Set(hits.map(h => h.id))
      result = result.filter(l => hitIds.has(l.id))
      const scoreMap = new Map(hits.map(h => [h.id, h.score]))
      result.sort((a, b) => (scoreMap.get(b.id) || 0) - (scoreMap.get(a.id) || 0))
    }
  }

  return result
})

function toggleHistogramHour(hour) {
  activeHistogramHour.value = activeHistogramHour.value === hour ? null : hour
}

function applyFilter() {
  // explicit submit only; no auto evidence checks on filter changes
}

function submitSearchEvidenceForLog(log) {
  const vaultWord = activeVaultWord4.value
  const whistleblowerUserId = activeWhistleblowerUserId.value
  const message = String(log?.message || '').toLowerCase()
  const isWhistle = String(log?.level || '').toUpperCase() === 'ERROR'
    && String(log?.user || '').toLowerCase() === whistleblowerUserId.toLowerCase()
    && !!vaultWord
    && message.includes(String(vaultWord).toLowerCase())
    && filteredLogs.value.some(l => l.id === log.id)

  searchSubmitResultById.value[log.id] = isWhistle ? 'correct' : 'wrong'

  if (!isWhistle) return
  store.addClue(
    'search-vault-word',
    'NovaSearch',
    `Whistleblower ERROR log contains keyword: "${vaultWord}".`
  )
  store.markRoomComplete('search')
}

function normalizeSearchLogs(rawLogs, vaultWord4, whistleblowerUserId) {
  const normalized = rawLogs.map((log, index) => ({
    id: String(log?.id || `log-${String(index + 1).padStart(3, '0')}`),
    timestamp: String(log?.timestamp || ''),
    level: String(log?.level || 'INFO').toUpperCase(),
    service: normalizeService(log?.service),
    user: normalizeUserId(log?.user, whistleblowerUserId),
    message: String(log?.message || ''),
    ip: String(log?.ip || ''),
  }))
  return ensureWhistleblowerClue(normalized, vaultWord4, whistleblowerUserId)
}

function normalizeService(service) {
  const normalized = String(service || 'api').toLowerCase()
  if (normalized === 'whistleblower') return 'vault'
  return normalized
}

function normalizeUserId(user, whistleblowerUserId) {
  const raw = String(user ?? '').trim()
  const lower = raw.toLowerCase()
  if (!raw) return null
  if (lower === 'anonymous' || lower === 'anon') return null
  if (lower === 'whistleblower') return whistleblowerUserId
  return raw
}

function ensureWhistleblowerClue(entries, vaultWord4, whistleblowerUserId) {
  if (!vaultWord4) return entries

  const hasValidWhistle = entries.some((log) => {
    const message = String(log.message || '').toLowerCase()
    return String(log.level || '').toUpperCase() === 'ERROR'
      && String(log.user || '').toLowerCase() === whistleblowerUserId.toLowerCase()
      && message.includes(vaultWord4)
  })
  if (hasValidWhistle) return entries

  const fallbackMessage = `ALERT: Unauthorized vault access flagged by whistleblower. Keyword: ${vaultWord4}.`
  const whistleIndex = entries.findIndex((log) => {
    return String(log.level || '').toUpperCase() === 'ERROR'
      && (
        String(log.user || '').toLowerCase() === whistleblowerUserId.toLowerCase()
        || String(log.message || '').toLowerCase().includes('whistleblower')
      )
  })

  if (whistleIndex >= 0) {
    const current = entries[whistleIndex]
    console.warn('[NovaSearch] Injected fallback clue word into whistleblower log to keep puzzle solvable.')
    const updated = [...entries]
    updated[whistleIndex] = {
      ...current,
      user: whistleblowerUserId,
      level: 'ERROR',
      message: `${String(current.message || '').trim()} Keyword: ${vaultWord4}`.trim(),
      service: normalizeService(current.service || 'vault'),
    }
    return updated
  }

  const clueLog = {
    id: `log-whistle-${vaultWord4}`,
    timestamp: '2025-03-03T01:33:00',
    level: 'ERROR',
    service: 'vault',
    user: whistleblowerUserId,
    message: fallbackMessage,
    ip: '192.168.1.99',
  }
  console.warn('[NovaSearch] Added fallback whistleblower log to keep puzzle solvable.')
  const insertAt = Math.min(35, entries.length)
  return [...entries.slice(0, insertAt), clueLog, ...entries.slice(insertAt)]
}

function getDefaultLogs(vaultWord4, whistleblowerUserId) {
  const base = []
  const services = ['auth', 'api', 'db', 'badge', 'mail']
  const lvls = ['INFO', 'INFO', 'INFO', 'WARN', 'DEBUG']
  for (let i = 0; i < 49; i++) {
    base.push({
      id: `log-${String(i + 1).padStart(3, '0')}`,
      timestamp: `2025-03-03T${String(Math.floor(Math.random() * 24)).padStart(2, '0')}:${String(Math.floor(Math.random() * 60)).padStart(2, '0')}:00`,
      level: lvls[Math.floor(Math.random() * lvls.length)],
      service: services[Math.floor(Math.random() * services.length)],
      user: `${1002 + Math.floor(Math.random() * 7)}`,
      message: 'Normal system operation',
      ip: `192.168.1.${Math.floor(Math.random() * 200) + 10}`,
    })
  }
  base.splice(35, 0, {
    id: 'log-036',
    timestamp: '2025-03-03T01:33:00',
    level: 'ERROR',
    service: 'vault',
    user: whistleblowerUserId,
    message: `ALERT: Unauthorized vault access by employee 1001 at 01:17. Motive marker detected. Keyword: ${vaultWord4}`,
    ip: '192.168.1.99',
  })
  return normalizeSearchLogs(base, vaultWord4, whistleblowerUserId)
}
</script>

<style scoped>
.search-view {
  /* Kibana EUI Light Theme */
  --eui-empty: #FFFFFF;
  --eui-lightest: #F5F7FA;
  --eui-light: #D3DAE6;
  --eui-medium: #98A2B3;
  --eui-dark: #69707D;
  --eui-darkest: #343741;
  --eui-primary: #006BB4;
  --eui-accent: #017D73;
  --eui-warning: #F5A700;
  --eui-danger: #BD271E;

  display: grid;
  grid-template-columns: 220px 1fr;
  height: 100%;
  overflow: hidden;
  background: var(--eui-empty);
  color: var(--eui-darkest);
  font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', sans-serif;
}

.search-view * { color: inherit; }
.search-view button { cursor: pointer; }

/* ---- Sidebar ---- */
.search-sidebar {
  background: var(--eui-lightest);
  border-right: 1px solid var(--eui-light);
  display: flex;
  flex-direction: column;
  overflow-y: auto;
}

.sidebar-logo {
  padding: 12px 16px;
  font-size: 15px;
  font-weight: 700;
  color: var(--eui-darkest);
  border-bottom: 1px solid var(--eui-light);
  font-family: var(--font-mono);
  display: flex;
  align-items: center;
  gap: 6px;
}

.logo-diamond { font-size: 10px; color: var(--eui-accent); }

.sidebar-section {
  padding: 4px 0;
  border-bottom: 1px solid var(--eui-light);
}

.sidebar-label {
  padding: 6px 16px 4px;
  font-size: 10px;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 1px;
  color: var(--eui-dark);
}

/* Selected fields */
.selected-field {
  background: rgba(1, 125, 115, 0.04);
}

.selected-field .field-name {
  color: var(--eui-accent);
  font-weight: 600;
}

.remove-col-btn {
  opacity: 0;
  font-size: 9px;
  transition: opacity 0.1s;
}

.selected-field:hover .remove-col-btn {
  opacity: 1;
}

/* Available fields */
.field-item-wrap {
  border-bottom: 1px solid rgba(211, 218, 230, 0.5);
}

.field-item {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 5px 12px;
  font-size: 12px;
  font-family: var(--font-mono);
  color: var(--eui-darkest);
  cursor: pointer;
  transition: background 0.12s;
}

.field-item:hover { background: rgba(1, 125, 115, 0.05); }
.field-item.expanded { background: rgba(1, 125, 115, 0.08); color: var(--eui-accent); }

.field-type-icon { font-size: 11px; flex-shrink: 0; width: 18px; text-align: center; }
.field-name { flex: 1; }

.field-unique-count {
  font-size: 10px;
  color: var(--eui-dark);
  background: var(--eui-light);
  padding: 1px 5px;
  border-radius: 3px;
}

.field-action-btn {
  background: transparent;
  border: none;
  color: var(--eui-dark);
  font-size: 11px;
  padding: 0 3px;
  opacity: 0;
  transition: opacity 0.1s;
}

.field-item:hover .field-action-btn { opacity: 0.7; }
.field-action-btn:hover { opacity: 1 !important; color: var(--eui-accent); }

.field-values {
  padding: 4px 8px 8px 36px;
  background: var(--eui-empty);
}

.field-value-row {
  display: flex;
  align-items: center;
  gap: 4px;
  padding: 2px 4px;
  font-size: 11px;
  font-family: var(--font-mono);
  cursor: default;
  border-radius: 3px;
  position: relative;
}

.field-value-row:hover { background: rgba(1, 125, 115, 0.04); }

.value-bar-bg {
  position: absolute;
  inset: 0;
  border-radius: 3px;
  overflow: hidden;
  pointer-events: none;
}

.value-bar {
  height: 100%;
  background: rgba(1, 125, 115, 0.08);
  border-radius: 3px;
}

.value-label {
  position: relative;
  z-index: 1;
  color: var(--eui-darkest);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  flex: 1;
  min-width: 0;
}

.value-count {
  position: relative;
  z-index: 1;
  color: var(--eui-dark);
  font-size: 10px;
  flex-shrink: 0;
}

.value-actions {
  position: relative;
  z-index: 1;
  display: flex;
  gap: 1px;
  opacity: 0;
  transition: opacity 0.1s;
  flex-shrink: 0;
}

.field-value-row:hover .value-actions { opacity: 1; }

.val-action-btn {
  background: transparent;
  border: 1px solid var(--eui-light);
  border-radius: 3px;
  font-size: 11px;
  width: 20px;
  height: 18px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: var(--eui-dark);
  cursor: pointer;
  padding: 0;
  line-height: 1;
}

.val-plus:hover { background: #E6F2F1; color: var(--eui-accent); border-color: var(--eui-accent); }
.val-minus:hover { background: #FDE8E6; color: var(--eui-danger); border-color: var(--eui-danger); }

/* Slide transition */
.slide-enter-active, .slide-leave-active { transition: max-height 0.2s ease, opacity 0.2s ease; overflow: hidden; }
.slide-enter-from, .slide-leave-to { max-height: 0; opacity: 0; }
.slide-enter-to, .slide-leave-from { max-height: 300px; opacity: 1; }

/* ---- Main ---- */
.search-main {
  display: flex;
  flex-direction: column;
  overflow: hidden;
  min-height: 0;
  position: relative;
}

/* ---- Search topbar ---- */
.search-topbar {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 8px 16px;
  background: var(--eui-lightest);
  border-bottom: 1px solid var(--eui-light);
}

.search-input-wrap {
  flex: 1;
  display: flex;
  align-items: center;
  gap: 6px;
  background: var(--eui-empty);
  border: 1px solid var(--eui-light);
  border-radius: 6px;
  padding: 0 10px;
  position: relative;
  box-shadow: 0 1px 2px rgba(0,0,0,0.04);
}

.search-input-wrap:focus-within {
  border-color: var(--eui-primary);
  box-shadow: 0 0 0 1px var(--eui-primary);
}

.kql-badge {
  font-size: 10px;
  font-weight: 700;
  color: var(--eui-empty);
  background: var(--eui-dark);
  padding: 2px 6px;
  border-radius: 3px;
  flex-shrink: 0;
  letter-spacing: 0.5px;
}

.search-input {
  flex: 1;
  border: none;
  background: transparent;
  font-family: var(--font-mono);
  font-size: 13px;
  padding: 7px 0;
  color: var(--eui-darkest);
  outline: none;
}

.search-input::placeholder { color: var(--eui-medium); }

.time-select {
  font-size: 12px;
  padding: 6px 10px;
  background: var(--eui-empty);
  border: 1px solid var(--eui-light);
  border-radius: 6px;
  color: var(--eui-darkest);
}

.search-btn {
  background: var(--eui-primary);
  color: #fff !important;
  font-size: 12px;
  padding: 7px 14px;
  white-space: nowrap;
  border: none;
  border-radius: 6px;
  font-weight: 600;
  display: flex;
  align-items: center;
  gap: 4px;
}

.search-btn:hover { background: #005a9e; }
.search-btn-icon { font-size: 13px; }

/* ---- Autocomplete ---- */
.autocomplete-dropdown {
  position: absolute;
  top: calc(100% + 2px);
  left: 0;
  right: 0;
  background: var(--eui-empty);
  border: 1px solid var(--eui-light);
  border-radius: 6px;
  z-index: 50;
  max-height: 280px;
  overflow-y: auto;
  box-shadow: 0 4px 20px rgba(0,0,0,0.12);
}

.ac-section-label {
  padding: 6px 12px 2px;
  font-size: 10px;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  color: var(--eui-dark);
}

.ac-item {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 6px 12px;
  font-size: 12px;
  font-family: var(--font-mono);
  color: var(--eui-darkest);
  cursor: pointer;
  transition: background 0.08s;
}

.ac-item:hover, .ac-item.active {
  background: #E6F2F1;
  color: var(--eui-accent);
}

.ac-icon { font-size: 12px; width: 18px; text-align: center; flex-shrink: 0; }
.ac-label { flex: 1; }

.ac-type {
  font-size: 9px;
  color: var(--eui-dark);
  background: var(--eui-lightest);
  padding: 1px 5px;
  border-radius: 3px;
  text-transform: uppercase;
  letter-spacing: 0.3px;
}

.ac-count {
  font-size: 10px;
  color: var(--eui-dark);
  background: var(--eui-lightest);
  padding: 1px 5px;
  border-radius: 3px;
}

/* ---- Filter pills ---- */
.filter-pills-bar {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 6px;
  padding: 6px 16px;
  background: var(--eui-lightest);
  border-bottom: 1px solid var(--eui-light);
}

.filter-pill {
  display: inline-flex;
  align-items: center;
  background: #E6F2F1;
  border: 1px solid #B3DDD8;
  border-radius: 4px;
  font-size: 11px;
  font-family: var(--font-mono);
  overflow: hidden;
}

.filter-pill.negated {
  background: #FDE8E6;
  border-color: #F1B5B0;
}

.filter-pill.disabled {
  opacity: 0.45;
}

.pill-toggle {
  background: transparent;
  border: none;
  border-right: 1px solid inherit;
  padding: 4px 6px;
  display: flex;
  align-items: center;
}

.pill-indicator {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  background: var(--eui-light);
  transition: background 0.15s;
}

.pill-indicator.on {
  background: var(--eui-accent);
}

.filter-pill.negated .pill-indicator.on {
  background: var(--eui-danger);
}

.pill-body {
  display: flex;
  align-items: center;
  gap: 4px;
  padding: 3px 6px;
  cursor: pointer;
}

.pill-body:hover { background: rgba(0,0,0,0.04); }

.pill-field { color: var(--eui-accent); font-weight: 600; }
.filter-pill.negated .pill-field { color: var(--eui-danger); }

.pill-operator {
  color: var(--eui-dark);
  font-size: 10px;
}

.pill-value { color: var(--eui-darkest); }

.pill-remove {
  background: transparent;
  color: var(--eui-dark);
  font-size: 10px;
  padding: 4px 6px;
  border: none;
  border-left: 1px solid rgba(0,0,0,0.08);
}

.pill-remove:hover { color: var(--eui-danger); background: rgba(189, 39, 30, 0.05); }

.pills-clear {
  background: transparent;
  border: none;
  color: var(--eui-dark);
  font-size: 11px;
  padding: 2px 6px;
}

.pills-clear:hover { color: var(--eui-danger); }

/* ---- Histogram ---- */
.histogram-wrap {
  padding: 8px 16px 2px;
  background: var(--eui-empty);
  border-bottom: 1px solid var(--eui-light);
}

.histogram { display: flex; align-items: flex-end; gap: 2px; height: 48px; }

.hist-bar-wrap {
  flex: 1;
  height: 100%;
  display: flex;
  align-items: flex-end;
  cursor: pointer;
  border-radius: 2px 2px 0 0;
  transition: background 0.12s;
}

.hist-bar-wrap:hover { background: rgba(1, 125, 115, 0.06); }
.hist-bar-wrap.active { background: rgba(1, 125, 115, 0.12); }

.hist-bar {
  width: 100%;
  background: var(--eui-accent);
  border-radius: 2px 2px 0 0;
  min-height: 1px;
  opacity: 0.5;
  transition: opacity 0.12s;
}

.hist-bar-wrap:hover .hist-bar,
.hist-bar-wrap.active .hist-bar { opacity: 0.85; }

.histogram-labels {
  display: flex;
  justify-content: space-between;
  font-size: 9px;
  color: var(--eui-dark);
  font-family: var(--font-mono);
  padding: 3px 0 0;
}

/* ---- Stats row ---- */
.stats-row {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 6px 16px;
  font-size: 12px;
  background: var(--eui-lightest);
  border-bottom: 1px solid var(--eui-light);
}

.hit-count { font-weight: 700; color: var(--eui-accent); }
.total-count { color: var(--eui-dark); }
.spacer { flex: 1; }

.active-filter-tag {
  background: #E6F2F1;
  color: var(--eui-accent);
  padding: 2px 8px;
  border-radius: 3px;
  font-size: 11px;
  font-family: var(--font-mono);
  cursor: pointer;
}

.active-filter-tag:hover { background: #CCE5E2; }

.submit-evidence-btn {
  font-size: 12px;
  font-weight: 600;
  font-family: var(--font-mono);
  padding: 4px 12px;
  background: rgba(1, 125, 115, 0.1);
  border: 1px solid var(--eui-accent);
  color: var(--eui-accent);
  border-radius: 3px;
}

.submit-evidence-btn:hover {
  background: rgba(1, 125, 115, 0.18);
}

.submit-correct {
  font-size: 12px;
  font-weight: 600;
  color: #2a7d2a;
}

.submit-wrong {
  font-size: 12px;
  font-weight: 600;
  color: #b33030;
}

/* ---- Column picker ---- */
.column-picker-wrap { position: relative; }

.column-picker-btn {
  background: var(--eui-empty);
  border: 1px solid var(--eui-light);
  color: var(--eui-darkest);
  font-size: 11px;
  font-family: var(--font-mono);
  padding: 3px 10px;
  border-radius: 4px;
  transition: border-color 0.12s;
}

.column-picker-btn:hover { border-color: var(--eui-primary); color: var(--eui-primary); }

.column-picker-dropdown {
  position: absolute;
  top: 100%;
  right: 0;
  background: var(--eui-empty);
  border: 1px solid var(--eui-light);
  border-radius: 6px;
  padding: 8px 0;
  z-index: 40;
  min-width: 140px;
  box-shadow: 0 4px 16px rgba(0,0,0,0.1);
}

.col-option {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 4px 12px;
  font-size: 12px;
  font-family: var(--font-mono);
  color: var(--eui-darkest);
  cursor: pointer;
}

.col-option:hover { background: var(--eui-lightest); }

/* ---- Table header ---- */
.log-header-row {
  display: grid;
  gap: 12px;
  padding: 6px 16px;
  background: var(--eui-lightest);
  border-bottom: 2px solid var(--eui-light);
  font-size: 11px;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  color: var(--eui-dark);
  font-family: var(--font-mono);
  align-items: center;
}

.log-expand-col { width: 28px; }

.header-cell { overflow: hidden; text-overflow: ellipsis; white-space: nowrap; }

/* ---- Log container ---- */
.log-container {
  flex: 1;
  overflow-y: auto;
  font-family: var(--font-mono);
  font-size: 12px;
  background: var(--eui-empty);
}

.no-results { padding: 24px; text-align: center; color: var(--eui-dark); font-style: italic; }

.log-row {
  display: grid;
  gap: 12px;
  padding: 5px 16px;
  border-bottom: 1px solid var(--eui-light);
  cursor: pointer;
  transition: background 0.08s;
  align-items: center;
}

.log-row:hover { background: var(--eui-lightest); }
.log-row.log-error { background: #FEF0EF; }
.log-row.log-error:hover { background: #FDE8E6; }
.log-row.log-warn { background: #FFF8E6; }
.log-row.log-warn:hover { background: #FFF3D1; }
.log-row.expanded { background: #E6F2F1; border-left: 2px solid var(--eui-accent); }

.log-expand-icon { color: var(--eui-dark); font-size: 10px; width: 28px; text-align: center; user-select: none; }

.cell-ts { color: var(--eui-dark); white-space: nowrap; }
.cell-service { color: var(--eui-accent); }
.cell-user { color: var(--eui-darkest); }
.cell-msg { color: var(--eui-darkest); white-space: nowrap; overflow: hidden; text-overflow: ellipsis; }
.cell-ip { color: var(--eui-dark); font-size: 11px; }
.cell-id { color: var(--eui-dark); font-size: 11px; }

.log-level-tag {
  display: inline-block;
  font-size: 10px;
  font-weight: 700;
  padding: 2px 5px;
  border-radius: 3px;
  text-align: center;
}

.tag-info  { background: #E6F2F1; color: var(--eui-accent); }
.tag-warn  { background: #FFF3D1; color: #9B6900; }
.tag-error { background: #FDE8E6; color: var(--eui-danger); }
.tag-debug { background: #F0E6F6; color: #764FA5; }

/* ---- Expanded row ---- */
.log-expanded {
  background: var(--eui-lightest);
  border-bottom: 1px solid var(--eui-light);
  border-left: 2px solid var(--eui-accent);
  padding: 0 16px 8px 46px;
}

.expanded-tabs {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 10px 16px;
  border-bottom: 1px solid var(--border-color);
}

.detail-title {
  font-size: 12px;
  font-weight: 600;
  color: var(--eui-dark);
  cursor: pointer;
}

.etab.active {
  color: var(--eui-primary);
  border-bottom-color: var(--eui-primary);
}

.etab:hover { color: var(--eui-darkest); }

.expanded-table { width: 100%; border-collapse: collapse; }

.expanded-row td {
  padding: 3px 8px;
  font-size: 12px;
  font-family: var(--font-mono);
  border-bottom: 1px solid rgba(211, 218, 230, 0.5);
  vertical-align: top;
}

.expanded-key { color: var(--eui-accent); width: 100px; font-weight: 600; }
.expanded-val { color: var(--eui-darkest); word-break: break-all; }

.expanded-actions {
  width: 120px;
  display: flex;
  gap: 2px;
  opacity: 0;
  transition: opacity 0.1s;
}

.expanded-row:hover .expanded-actions { opacity: 1; }

.exp-action-btn {
  background: transparent;
  border: 1px solid var(--eui-light);
  border-radius: 3px;
  font-size: 11px;
  padding: 1px 4px;
  color: var(--eui-dark);
  cursor: pointer;
  line-height: 1.2;
}

.exp-action-btn:hover {
  background: #E6F2F1;
  border-color: var(--eui-accent);
  color: var(--eui-accent);
}

.expanded-json {
  font-family: var(--font-mono);
  font-size: 12px;
  color: var(--eui-darkest);
  background: var(--eui-empty);
  border: 1px solid var(--eui-light);
  border-radius: 4px;
  padding: 12px;
  overflow-x: auto;
  white-space: pre;
  margin: 0;
}

/* ---- Transitions ---- */
.expand-enter-active, .expand-leave-active { transition: max-height 0.2s ease, opacity 0.15s ease; overflow: hidden; }
.expand-enter-from, .expand-leave-to { max-height: 0; opacity: 0; }
.expand-enter-to, .expand-leave-from { max-height: 500px; opacity: 1; }

.fade-enter-active, .fade-leave-active { transition: opacity 0.12s ease; }
.fade-enter-from, .fade-leave-to { opacity: 0; }
</style>
