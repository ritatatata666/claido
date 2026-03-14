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
const hintsUsed = ref(0)
const HINTS = [
  'Hidden files start with a dot. Try: ls -a',
  'Environment files store secrets. Try: cat .env',
  'The VAULT_WORD is base64 encoded. Try: base64 -d <the_value>',
]

onMounted(async () => {
  // Load room content
  try {
    const data = await store.enterRoom('shell')
    filesystem = normalizeFilesystem(data)
  } catch (e) {
    filesystem = getDefaultFilesystem()
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
  term.writeln('\x1b[32m╔══════════════════════════════════════════════════╗\x1b[0m')
  term.writeln('\x1b[32m║          NovaCorp Internal Shell v3.1.4          ║\x1b[0m')
  term.writeln('\x1b[32m║     AUTHORISED ACCESS ONLY — ALL ACTIVITY LOGGED ║\x1b[0m')
  term.writeln('\x1b[32m╚══════════════════════════════════════════════════╝\x1b[0m')
  term.writeln('')
  term.writeln('\x1b[33mType "help" for available commands.\x1b[0m')
  term.writeln('')

  prompt()

  term.onKey(({ key, domEvent }) => {
    const code = domEvent.keyCode

    if (code === 13) {
      // Enter
      term.writeln('')
      handleCommand(inputBuffer.trim())
      inputBuffer = ''
      prompt()
    } else if (code === 8) {
      // Backspace
      if (inputBuffer.length > 0) {
        inputBuffer = inputBuffer.slice(0, -1)
        term.write('\b \b')
      }
    } else if (code === 67 && domEvent.ctrlKey) {
      // Ctrl+C
      term.writeln('^C')
      inputBuffer = ''
      prompt()
    } else if (key && !domEvent.ctrlKey && !domEvent.altKey) {
      inputBuffer += key
      term.write(key)
    }
  })
})

onUnmounted(() => {
  resizeObserver?.disconnect()
  term?.dispose()
})

function prompt() {
  const user = filesystem?.username || 'analyst'
  const host = filesystem?.hostname || 'novacorp-srv-01'
  const shortCwd = cwd.replace(`/home/${user}`, '~')
  term.write(`\x1b[32m${user}@${host}\x1b[0m:\x1b[34m${shortCwd}\x1b[0m$ `)
}

function handleCommand(cmd) {
  if (!cmd) return

  const [prog, ...args] = cmd.split(/\s+/)

  switch (prog) {
    case 'help':
      term.writeln('Available commands: ls, cat, cd, pwd, whoami, echo, env, ps, grep, find, mkdir, touch, rm, uname, hostname, date, history, sudo, chmod, man, base64, hint, clear, help')
      term.writeln('')
      break

    case 'pwd':
      term.writeln(cwd)
      break

    case 'whoami':
      term.writeln(filesystem?.username || 'analyst')
      break

    case 'clear':
      term.clear()
      break

    case 'ls':
      cmdLs(args)
      break

    case 'cat':
      cmdCat(args[0])
      break

    case 'cd':
      cmdCd(args[0])
      break

    case 'echo':
      term.writeln(args.join(' '))
      break

    case 'uname':
      term.writeln(args.includes('-a')
        ? 'Linux novacorp-srv-01 5.15.0-91-generic #101-Ubuntu SMP x86_64 GNU/Linux'
        : 'Linux')
      break

    case 'hostname':
      term.writeln(filesystem?.hostname || 'novacorp-srv-01')
      break

    case 'date':
      term.writeln(new Date().toString())
      break

    case 'env':
      term.writeln('PATH=/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin')
      term.writeln(`USER=${filesystem?.username || 'analyst'}`)
      term.writeln(`HOME=/home/${filesystem?.username || 'analyst'}`)
      term.writeln(`SHELL=/bin/bash`)
      term.writeln(`TERM=xterm-256color`)
      term.writeln(`LANG=en_AU.UTF-8`)
      break

    case 'ps':
      term.writeln('  PID TTY          TIME CMD')
      term.writeln(' 1337 pts/0    00:00:00 bash')
      term.writeln(' 1342 pts/0    00:00:00 ps')
      break

    case 'history':
      ;['ls', 'cd /home/analyst', 'cat readme.txt', 'ls logs/', 'cat logs/access.log', 'cat .env', 'cd ..', 'grep -r "vault" .'].forEach((h, i) => {
        term.writeln(`  ${String(i + 1).padStart(3)}  ${h}`)
      })
      break

    case 'grep':
      cmdGrep(args)
      break

    case 'find':
      cmdFind(args)
      break

    case 'sudo':
      term.writeln(`\x1b[31m[sudo] password for ${filesystem?.username || 'analyst'}: `)
      term.writeln(`\x1b[31m${filesystem?.username || 'analyst'} is not in the sudoers file. This incident will be reported.\x1b[0m`)
      break

    case 'chmod':
    case 'mkdir':
    case 'touch':
    case 'rm':
      term.writeln(`\x1b[33mOperation not permitted on forensic read-only mount.\x1b[0m`)
      break

    case 'man':
      term.writeln(`No manual entry for ${args[0] || prog}`)
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
      term.writeln(`\x1b[31mNetwork access disabled on this terminal.\x1b[0m`)
      break

    case 'python':
    case 'python3':
    case 'node':
    case 'perl':
    case 'ruby':
      term.writeln(`\x1b[31mInterpreter execution blocked by security policy.\x1b[0m`)
      break

    case 'exit':
    case 'logout':
      term.writeln('Session cannot be terminated during active investigation.')
      break

    default:
      term.writeln(`\x1b[31mbash: ${prog}: command not found\x1b[0m`)
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

function cmdLs(rawArgs) {
  // Separate flags (starting with -) from the path argument
  const lsArgs = Array.isArray(rawArgs) ? rawArgs : (rawArgs ? [rawArgs] : [])
  const flags = lsArgs.filter(a => a.startsWith('-')).join('')
  const showHidden = flags.includes('a')
  const pathArg = lsArgs.find(a => !a.startsWith('-'))

  const path = resolvePath(pathArg)
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
    term.writeln(`ls: cannot access '${pathArg || path}': No such file or directory`)
    return
  }

  const visible = showHidden ? children : children.filter(c => !c.name.startsWith('.'))

  const dirNames  = visible.filter(c => c.type === 'dir').map(c => `\x1b[34m${c.name}/\x1b[0m`)
  const fileNames = visible.filter(c => c.type === 'file').map(c =>
    c.name.startsWith('.') ? `\x1b[90m${c.name}\x1b[0m` : c.name
  )

  term.writeln([...dirNames, ...fileNames].join('  '))
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
    term.writeln(`cd: ${arg}: No such file or directory`)
  }
}

function cmdCat(arg) {
  if (!arg) {
    term.writeln('cat: missing operand')
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
    term.writeln(`\x1b[31mcat: ${arg}: No such file or directory\x1b[0m`)
    return
  }

  // Print raw file content without any decoding
  content.split('\n').forEach(line => term.writeln(line))

  // Check for suspicious access log clue
  if (path.endsWith('access.log')) {
    const culpritId = store.sessionState?.culprit?.id
    if (culpritId && content.includes(String(culpritId))) {
      store.addClue(
        'shell-access-log',
        'NovaShell',
        `Access log shows employee ${culpritId} accessed Server Room B at unusual hours.`
      )
    }
  }

  term.writeln('')
}

function cmdBase64(args) {
  const flagIdx = args.indexOf('-d')
  if (flagIdx < 0) {
    term.writeln('Usage: base64 -d <value>')
    return
  }
  const value = args.find((a, i) => i !== flagIdx)
  if (!value) {
    term.writeln('base64: missing operand')
    return
  }
  try {
    const decoded = atob(value)
    term.writeln(decoded)
    // Check if the decoded value matches the session vault word
    const expected = store.sessionState?.vaultWord1
    if (expected && decoded === expected) {
      store.addClue(
        'shell-vault-word',
        'NovaShell',
        `Found in .env: VAULT_WORD = ${decoded}`
      )
      store.markRoomComplete('shell')
    }
  } catch (e) {
    term.writeln(`\x1b[31mbase64: invalid input — not valid base64\x1b[0m`)
  }
}

function cmdHint() {
  const MAX_HINTS = 3
  if (hintsUsed.value >= MAX_HINTS) {
    term.writeln('\x1b[33mNo hints remaining.\x1b[0m')
    return
  }
  const hint = HINTS[hintsUsed.value]
  hintsUsed.value++
  term.writeln(`\x1b[33m[HINT ${hintsUsed.value}/${MAX_HINTS}]\x1b[0m ${hint}`)
}

function cmdGrep(args) {
  if (args.length < 2) {
    term.writeln('Usage: grep [pattern] [file]')
    return
  }
  const pattern = args[0]
  const filePath = resolvePath(args[args.length - 1])
  const files = filesystem?.files || {}
  const content = files[filePath]
  if (!content) {
    term.writeln(`grep: ${args[args.length - 1]}: No such file or directory`)
    return
  }
  const regex = new RegExp(pattern, 'i')
  const matches = content.split('\n').filter(l => regex.test(l))
  if (matches.length === 0) {
    return
  }
  matches.forEach(l => term.writeln(l.replace(regex, m => `\x1b[31m${m}\x1b[0m`)))
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
    term.writeln(`find: nothing found`)
    return
  }
  results.forEach(r => term.writeln(r))
}

function normalizeFilesystem(data) {
  if (!data || typeof data !== 'object') return getDefaultFilesystem()
  const home = `/home/${data.username || 'analyst'}`
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
  return { ...data, files: normalized, dirs: [...dirSet] }
}

function getDefaultFilesystem() {
  return {
    hostname: 'novacorp-srv-01',
    username: 'analyst',
    files: {
      '/home/analyst/.env': 'VAULT_WORD=bWlkbmlnaHQ=\nDB_PASS=hunter2\nAPI_KEY=sk-fake-abc123',
      '/home/analyst/logs/access.log': '[01:17:22] WARN  Employee 1001 accessed Server Room B (AFTER_HOURS)\n[08:30:01] INFO  Normal access main entrance\n[09:15:44] INFO  Engineering floor scan',
      '/home/analyst/readme.txt': 'Welcome to NovaCorp analyst workstation.\nCase file: PROJECT_NOVA_INCIDENT_2025.',
      '/etc/passwd': 'root:x:0:0:root:/root:/bin/bash\nanalyst:x:1000:1000::/home/analyst:/bin/bash',
    },
    dirs: ['/home/analyst', '/home/analyst/logs', '/etc', '/var/log', '/tmp'],
  }
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
