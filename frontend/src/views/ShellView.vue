<template>
  <RoomLayout>
    <div class="shell-view" ref="containerEl"></div>
    <NpcChat npc-id="sysadmin" npc-name="Alex Torres" npc-role="Senior Sysadmin" />
  </RoomLayout>
</template>

<script setup>
import { ref, onMounted, onUnmounted } from 'vue'
import { useRouter } from 'vue-router'
import { Terminal } from '@xterm/xterm'
import { FitAddon } from '@xterm/addon-fit'
import '@xterm/xterm/css/xterm.css'
import RoomLayout from '../components/RoomLayout.vue'
import NpcChat from '../components/NpcChat.vue'
import { useGameStore } from '../stores/gameStore.js'

const store = useGameStore()
const router = useRouter()
const containerEl = ref(null)

let term = null
let fitAddon = null
let resizeObserver = null

// Runtime state
let cwd = '/home/analyst'
let inputBuffer = ''
let filesystem = null
const commandHistory = ref([])
let historyIndex = 0
const activeVaultWord1 = ref('')
const activeCulpritId = ref(1001)

onMounted(async () => {
  const vaultWord1 = resolveVaultWord1()
  const culpritId = Number(store.sessionState?.culprit?.id) || 1001
  activeVaultWord1.value = vaultWord1
  activeCulpritId.value = culpritId

  // Restore shell history from store
  const restoredHistory = Array.isArray(store.shellHistory) ? store.shellHistory : []
  commandHistory.value = [...restoredHistory]
  historyIndex = commandHistory.value.length

  // Load room content
  try {
    const data = await store.enterRoom('shell')
    filesystem = normalizeFilesystem(data)
  } catch (e) {
    filesystem = getDefaultFilesystem(vaultWord1, culpritId)
  }

  // Init terminal
  term = new Terminal({
    cursorBlink: true,
    fontSize: 14,
    fontFamily: "'Courier New', monospace",
    theme: {
      background: '#0d0d0d',
      foreground: '#00ff41',
      cursor: '#00ff41',
      selectionBackground: '#003300',
      black: '#0d0d0d',
      green: '#00ff41',
      brightGreen: '#00ff88',
    },
    convertEol: true,
  })

  fitAddon = new FitAddon()
  term.loadAddon(fitAddon)
  term.open(containerEl.value)
  fitAddon.fit()

  resizeObserver = new ResizeObserver(() => fitAddon.fit())
  resizeObserver.observe(containerEl.value)

  // Welcome message
  writelnTerminal('\x1b[32m╔══════════════════════════════════════════════════╗\x1b[0m')
  writelnTerminal('\x1b[32m║          NovaCorp Internal Shell v3.1.4          ║\x1b[0m')
  writelnTerminal('\x1b[32m║     AUTHORISED ACCESS ONLY — ALL ACTIVITY LOGGED ║\x1b[0m')
  writelnTerminal('\x1b[32m╚══════════════════════════════════════════════════╝\x1b[0m')
  writelnTerminal('')
  writelnTerminal('\x1b[33mType "help" for available commands.\x1b[0m')
  writelnTerminal('')
  prompt()

  term.onKey(({ key, domEvent }) => {
    domEvent.preventDefault()
    const domKey = domEvent.key
    if (domEvent.keyCode === 13) {
      // Enter
      const cmd = inputBuffer.trim()
      writelnTerminal('')
      if (cmd) {
        commandHistory.value.push(cmd)
        historyIndex = commandHistory.value.length
        store.setShellHistory(commandHistory.value)
      }
      handleCommand(cmd)
      inputBuffer = ''
      prompt()
      return
    }

    if (domEvent.keyCode === 8) {
      // Backspace
      if (inputBuffer.length > 0) {
        inputBuffer = inputBuffer.slice(0, -1)
        writeTerminal('\b \b')
      }
      return
    }

    if (domEvent.ctrlKey && domKey.toLowerCase() === 'c') {
      // Ctrl+C
      writelnTerminal('^C')
      inputBuffer = ''
      prompt()
      return
    }

    if (domKey === 'ArrowUp') {
      if (commandHistory.value.length === 0) return
      if (historyIndex > 0) historyIndex--
      const prev = commandHistory.value[historyIndex] ?? ''
      clearCurrentInput()
      inputBuffer = prev
      writeTerminal(prev)
      return
    }

    if (domKey === 'ArrowDown') {
      if (historyIndex < commandHistory.value.length - 1) {
        historyIndex++
        const next = commandHistory.value[historyIndex] ?? ''
        clearCurrentInput()
        inputBuffer = next
        writeTerminal(next)
      } else {
        historyIndex = commandHistory.value.length
        clearCurrentInput()
        inputBuffer = ''
      }
      return
    }

    if (domKey === 'ArrowLeft' || domKey === 'ArrowRight') {
      return
    }

    if ((domKey && domKey.length === 1) || domKey === '\t') {
      // Printable chars — also handles paste (multi-char data)
      inputBuffer += key
      writeTerminal(key)
      historyIndex = commandHistory.value.length
    }
  })
})

onUnmounted(() => {
  resizeObserver?.disconnect()
  term?.dispose()
})

function clearCurrentInput() {
  // Erase current input from terminal display
  writeTerminal('\b \b'.repeat(inputBuffer.length))
  inputBuffer = ''
}

function prompt() {
  const user = filesystem?.username || 'analyst'
  const host = filesystem?.hostname || 'novacorp-srv-01'
  const shortCwd = cwd.replace(`/home/${user}`, '~')
  writeTerminal(`\x1b[32m${user}@${host}\x1b[0m:\x1b[34m${shortCwd}\x1b[0m$ `)
}

function writeTerminal(data) {
  term.write(data)
}

function writelnTerminal(data = '') {
  term.writeln(data)
}

function handleCommand(cmd) {
  if (!cmd) return

  const [prog, ...args] = cmd.split(/\s+/)

  switch (prog) {
    case 'help':
      writelnTerminal('Available commands: ls, cat, cd, pwd, whoami, echo, env, ps, grep, find, mkdir, touch, rm, uname, hostname, date, history, sudo, chmod, man, base64, hint, clear, help')
      writelnTerminal('')
      break

    case 'pwd':
      writelnTerminal(cwd)
      break

    case 'whoami':
      writelnTerminal(filesystem?.username || 'analyst')
      break

    case 'clear':
      term.clear()
      break

    case 'ls':
      cmdLs(args[0])
      break

    case 'cat':
      cmdCat(args[0])
      break

    case 'cd':
      cmdCd(args[0])
      break

    case 'echo':
      writelnTerminal(args.join(' '))
      break

    case 'uname':
      writelnTerminal(args.includes('-a')
        ? 'Linux novacorp-srv-01 5.15.0-91-generic #101-Ubuntu SMP x86_64 GNU/Linux'
        : 'Linux')
      break

    case 'hostname':
      writelnTerminal(filesystem?.hostname || 'novacorp-srv-01')
      break

    case 'date':
      writelnTerminal(new Date().toString())
      break

    case 'env':
      writelnTerminal('PATH=/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin')
      writelnTerminal(`USER=${filesystem?.username || 'analyst'}`)
      writelnTerminal(`HOME=/home/${filesystem?.username || 'analyst'}`)
      writelnTerminal(`SHELL=/bin/bash`)
      writelnTerminal(`TERM=xterm-256color`)
      writelnTerminal(`LANG=en_AU.UTF-8`)
      break

    case 'ps':
      writelnTerminal('  PID TTY          TIME CMD')
      writelnTerminal(' 1337 pts/0    00:00:00 bash')
      writelnTerminal(' 1342 pts/0    00:00:00 ps')
      break

    case 'history':
      commandHistory.value.forEach((h, i) => {
        writelnTerminal(`  ${String(i + 1).padStart(3)}  ${h}`)
      })
      break

    case 'grep':
      cmdGrep(args)
      break

    case 'find':
      cmdFind(args)
      break

    case 'sudo':
      writelnTerminal(`\x1b[31m[sudo] password for ${filesystem?.username || 'analyst'}: `)
      writelnTerminal(`\x1b[31m${filesystem?.username || 'analyst'} is not in the sudoers file. This incident will be reported.\x1b[0m`)
      break

    case 'chmod':
    case 'mkdir':
    case 'touch':
    case 'rm':
      writelnTerminal(`\x1b[33mOperation not permitted on forensic read-only mount.\x1b[0m`)
      break

    case 'man':
      writelnTerminal(`No manual entry for ${args[0] || prog}`)
      break

    case 'base64':
      cmdBase64(args)
      break

    case 'hint':
      cmdHint()
      break

    case 'ssh':
    case 'scp':
    case 'curl':
    case 'wget':
      writelnTerminal(`\x1b[31mNetwork access disabled on this terminal.\x1b[0m`)
      break

    case 'python':
    case 'python3':
    case 'node':
    case 'perl':
    case 'ruby':
      writelnTerminal(`\x1b[31mInterpreter execution blocked by security policy.\x1b[0m`)
      break

    case 'exit':
    case 'logout':
      writelnTerminal('Session cannot be terminated during active investigation.')
      break

    default:
      writelnTerminal(`\x1b[31mbash: ${prog}: command not found\x1b[0m`)
  }
}

function resolvePath(p) {
  if (!p || p === '.') return cwd
  if (p.startsWith('/')) return p
  if (p === '~') return `/home/${filesystem?.username || 'analyst'}`
  if (p === '..') {
    const parts = cwd.split('/')
    parts.pop()
    return parts.join('/') || '/'
  }
  return `${cwd}/${p}`.replace('//', '/')
}

function cmdLs(arg) {
  const path = resolvePath(arg)
  const dirs = filesystem?.dirs || []
  const files = filesystem?.files || {}

  const children = []

  // Sub-dirs
  dirs.forEach(d => {
    if (d !== path && d.startsWith(path + '/') && !d.slice(path.length + 1).includes('/')) {
      children.push({ name: d.split('/').pop(), type: 'dir' })
    }
  })

  // Files in this dir
  Object.keys(files).forEach(f => {
    const dir = f.substring(0, f.lastIndexOf('/'))
    if (dir === path) {
      children.push({ name: f.split('/').pop(), type: 'file' })
    }
  })

  if (children.length === 0) {
    writelnTerminal(`ls: cannot access '${pathArg || path}': No such file or directory`)
    return
  }

  const dirNames = children.filter(c => c.type === 'dir').map(c => `\x1b[34m${c.name}/\x1b[0m`)
  const fileNames = children.filter(c => c.type === 'file').map(c => {
    const hidden = c.name.startsWith('.')
    return hidden ? `\x1b[90m${c.name}\x1b[0m` : c.name
  })

  writelnTerminal([...dirNames, ...fileNames].join('  '))
}

function cmdCd(arg) {
  if (!arg || arg === '~') {
    cwd = `/home/${filesystem?.username || 'analyst'}`
    return
  }
  const target = resolvePath(arg)
  const dirs = filesystem?.dirs || []
  if (dirs.includes(target)) {
    cwd = target
  } else {
    writelnTerminal(`cd: ${arg}: No such file or directory`)
  }
}

function cmdCat(arg) {
  if (!arg) {
    writelnTerminal('cat: missing operand')
    return
  }
  const path = resolvePath(arg)
  const files = filesystem?.files || {}

  // Exact path lookup first; fall back to matching by filename anywhere in fs
  let content = files[path]
  if (content === undefined) {
    const filename = arg.split('/').pop()
    const match = Object.entries(files).find(([k]) => k.split('/').pop() === filename)
    if (match) content = match[1]
  }
  if (content === undefined) {
    writelnTerminal(`\x1b[31mcat: ${arg}: No such file or directory\x1b[0m`)
    return
  }

  // Print raw file content without any decoding
  content.split('\n').forEach(line => writelnTerminal(line))

  // Check for suspicious access log clue
  if (path.endsWith('access.log')) {
    const culpritId = activeCulpritId.value
    if (culpritId && content.includes(String(culpritId))) {
      store.addClue(
        'shell-access-log',
        'NovaShell',
        `Access log shows employee ${culpritId} accessed Server Room B at unusual hours.`
      )
    }
  }

  writelnTerminal('')
}

function cmdBase64(args) {
  const flagIdx = args.indexOf('-d')
  if (flagIdx < 0) {
    writelnTerminal('Usage: base64 -d <value>')
    return
  }
  const value = args.find((a, i) => i !== flagIdx)
  if (!value) {
    writelnTerminal('base64: missing operand')
    return
  }
  try {
    const decoded = atob(value)
    writelnTerminal(decoded)
    // Check if the decoded value matches the session vault word
    const expected = activeVaultWord1.value
    if (expected && decoded === expected) {
      store.addClue(
        'shell-vault-word',
        'NovaShell',
        `Found in .env: VAULT_WORD = ${decoded}`
      )
      store.markRoomComplete('shell')
    }
  } catch (e) {
    writelnTerminal(`\x1b[31mbase64: invalid input — not valid base64\x1b[0m`)
  }
}

function cmdHint() {
  const MAX_HINTS = 3
  if (hintsUsed.value >= MAX_HINTS) {
    writelnTerminal('\x1b[33mNo hints remaining.\x1b[0m')
    return
  }
  const hint = HINTS[hintsUsed.value]
  hintsUsed.value++
  writelnTerminal(`\x1b[33m[HINT ${hintsUsed.value}/${MAX_HINTS}]\x1b[0m ${hint}`)
}

function cmdGrep(args) {
  if (args.length < 2) {
    writelnTerminal('Usage: grep [pattern] [file]')
    return
  }
  const pattern = args[0]
  const filePath = resolvePath(args[args.length - 1])
  const files = filesystem?.files || {}
  const content = files[filePath]
  if (!content) {
    writelnTerminal(`grep: ${args[args.length - 1]}: No such file or directory`)
    return
  }
  const regex = new RegExp(pattern, 'i')
  const matches = content.split('\n').filter(l => regex.test(l))
  if (matches.length === 0) {
    return
  }
  matches.forEach(l => writelnTerminal(l.replace(regex, m => `\x1b[31m${m}\x1b[0m`)))
}

function cmdFind(args) {
  const searchPath = args[0] === '.' || !args[0] ? cwd : resolvePath(args[0])
  const nameIdx = args.indexOf('-name')
  const pattern = nameIdx >= 0 ? args[nameIdx + 1] : null
  const allPaths = [
    ...(filesystem?.dirs || []),
    ...Object.keys(filesystem?.files || {}),
  ]
  const results = allPaths.filter(p => {
    if (!p.startsWith(searchPath)) return false
    if (!pattern) return true
    const glob = pattern.replace('*', '.*').replace('?', '.')
    return new RegExp(glob).test(p.split('/').pop())
  })
  if (results.length === 0) {
    writelnTerminal(`find: nothing found`)
    return
  }
  results.forEach(r => writelnTerminal(r))
}

function normalizeFilesystem(data) {
  if (!data || typeof data !== 'object') return getDefaultFilesystem(resolveVaultWord1(), Number(store.sessionState?.culprit?.id) || 1001)
  const home = `/home/${data.username || 'analyst'}`
  const vaultWord1 = resolveVaultWord1()
  const rawFiles = data.files || {}
  const normalized = {}
  for (const [key, value] of Object.entries(rawFiles)) {
    // If the key is already an absolute path, keep it
    // Otherwise treat it as relative to the home directory
    const absKey = key.startsWith('/') ? key : `${home}/${key}`
    normalized[absKey] = value
  }
  // Normalise dirs too
  const rawDirs = (data.dirs || []).map(d => d.startsWith('/') ? d : `${home}/${d}`)
  // Always ensure home and home/logs exist as dirs
  const dirSet = new Set([home, `${home}/logs`, '/etc', '/var/log', '/tmp', ...rawDirs])
  const ensuredFiles = ensureShellVaultWordFile(normalized, home, vaultWord1)
  return { ...data, files: ensuredFiles, dirs: [...dirSet] }
}

function resolveVaultWord1() {
  const fromSession = String(store.sessionState?.vaultWord1 || '').toLowerCase().trim()
  if (fromSession) return fromSession
  const fallback = 'midnight'
  console.warn('[NovaShell] Using fallback vaultWord1 because session value is missing.')
  return fallback
}

function getDefaultFilesystem(vaultWord1, culpritId = 1001) {
  const encodedWord = btoa(vaultWord1)
  return {
    hostname: 'novacorp-srv-01',
    username: 'analyst',
    files: {
      '/home/analyst/.env': `VAULT_WORD=${encodedWord}\nDB_PASS=hunter2\nAPI_KEY=sk-fake-abc123`,
      '/home/analyst/logs/access.log': `[01:17:22] WARN  Employee ${culpritId} accessed Server Room B (AFTER_HOURS)\n[08:30:01] INFO  Normal access main entrance\n[09:15:44] INFO  Engineering floor scan`,
      '/home/analyst/readme.txt': 'Welcome to NovaCorp analyst workstation.\nCase file: PROJECT_NOVA_INCIDENT_2025.',
      '/etc/passwd': 'root:x:0:0:root:/root:/bin/bash\nanalyst:x:1000:1000::/home/analyst:/bin/bash',
    },
    dirs: ['/home/analyst', '/home/analyst/logs', '/etc', '/var/log', '/tmp'],
  }
}

function ensureShellVaultWordFile(files, home, vaultWord1) {
  const envPath = `${home}/.env`
  const expectedEncoded = btoa(vaultWord1)
  const existing = String(files[envPath] || '')
  const hasExpected = existing.includes(`VAULT_WORD=${expectedEncoded}`)
  if (hasExpected) return files

  const nextEnv = existing
    ? existing.replace(/VAULT_WORD=.*$/m, `VAULT_WORD=${expectedEncoded}`)
    : `VAULT_WORD=${expectedEncoded}\nDB_PASS=hunter2\nAPI_KEY=sk-fake-abc123`
  files[envPath] = nextEnv.includes('VAULT_WORD=') ? nextEnv : `VAULT_WORD=${expectedEncoded}\n${nextEnv}`
  console.warn('[NovaShell] Injected vault word into .env fallback content to keep puzzle solvable.')
  return files
}
</script>

<style scoped>
.shell-view {
  --bg-primary: #0d0d0d;
  --bg-secondary: #111;
  --text-primary: #00ff41;

  width: 100%;
  height: 100%;
  background: #0d0d0d;
  overflow: hidden;
}

.shell-view :deep(.xterm) {
  height: 100%;
  padding: 16px;
}

.shell-view :deep(.xterm-viewport) {
  background: #0d0d0d !important;
}
</style>
