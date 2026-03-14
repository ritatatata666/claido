<template>
  <RoomLayout>
    <div class="onion-view">
      <!-- Header -->
      <div class="onion-header">
        <div class="onion-logo">
          <span class="onion-dot">●</span> THE ONION
          <span class="onion-tagline">.onion — anonymised — proceed with caution</span>
        </div>
        <div class="onion-nav">
          <span
            v-for="tab in tabs"
            :key="tab"
            :class="['onion-tab', { active: activeTab === tab }]"
            @click="activeTab = tab"
          >{{ tab }}</span>
        </div>
      </div>

      <div class="onion-body">
        <!-- Loading -->
        <div v-if="loading" class="onion-loading">
          <span class="blink">_</span> Establishing anonymous connection...
        </div>

        <!-- Forum -->
        <div v-else-if="activeTab === 'FORUM'" class="forum-view">
          <div class="forum-header"># NOVACORP LEAKS</div>
          <div
            v-for="post in forumPosts"
            :key="post.id"
            :class="['post-item', { selected: selectedPost?.id === post.id }]"
            @click="selectedPost = selectedPost?.id === post.id ? null : post"
          >
            <div class="post-meta">
              <span class="post-handle">{{ post.handle }}</span>
              <span class="post-ts">{{ post.timestamp }}</span>
              <span class="post-replies">{{ post.replies }} replies</span>
            </div>
            <div class="post-title">{{ post.title }}</div>
            <Transition name="fade">
              <div v-if="selectedPost?.id === post.id" class="post-body">
                {{ post.body }}
              </div>
            </Transition>
          </div>
        </div>

        <!-- Market -->
        <div v-else-if="activeTab === 'MARKET'" class="market-view">
          <div class="market-header"># DARKMARKET</div>
          <div
            v-for="listing in marketListings"
            :key="listing.id"
            :class="['listing-item', { selected: selectedListing?.id === listing.id }]"
            @click="selectedListing = selectedListing?.id === listing.id ? null : listing"
          >
            <div class="listing-top">
              <span class="listing-title">{{ listing.title }}</span>
              <span class="listing-price">{{ listing.price }}</span>
            </div>
            <div class="listing-seller">sold by: {{ listing.seller }}</div>
            <div class="listing-desc">{{ listing.description }}</div>
            <span class="listing-cat">[ {{ listing.category }} ]</span>
            <div v-if="selectedListing?.id === listing.id" class="listing-actions">
              <button class="submit-evidence-btn" @click.stop="submitOnionEvidence(listing)">
                ⚑ Submit as evidence
              </button>
              <span v-if="onionSubmitResult[listing.id] === 'correct'" class="submit-correct">✓ Evidence logged</span>
              <span v-else-if="onionSubmitResult[listing.id] === 'wrong'" class="submit-wrong">✗ Not enough linkage to culprit.</span>
            </div>
          </div>
        </div>
      </div>
    </div>
    <NpcChat npc-id="cfo" npc-name="Richard Harlow" npc-role="Chief Financial Officer" />
  </RoomLayout>
</template>

<script setup>
import { ref, onMounted } from 'vue'
import RoomLayout from '../components/RoomLayout.vue'
import NpcChat from '../components/NpcChat.vue'
import { useGameStore } from '../stores/gameStore.js'

const store = useGameStore()
const loading = ref(true)
const forumPosts = ref([])
const marketListings = ref([])
const activeTab = ref('FORUM')
const selectedPost = ref(null)
const selectedListing = ref(null)
const onionSubmitResult = ref({})
const tabs = ['FORUM', 'MARKET']

onMounted(async () => {
  const culpritDepartment = resolveCulpritDepartment()
  try {
    const data = await store.enterRoom('onion')
    const normalized = normalizeOnionContent(data, culpritDepartment)
    forumPosts.value = normalized.forumPosts
    marketListings.value = normalized.marketListings
  } catch (e) {
    const defaults = getDefaultContent(culpritDepartment)
    forumPosts.value = defaults.forumPosts
    marketListings.value = defaults.marketListings
  } finally {
    loading.value = false
  }
})

function resolveCulpritDepartment() {
  const dept = String(store.sessionState?.culprit?.department || '').trim()
  if (dept) return dept
  const fallback = 'Finance'
  console.warn('[Onion] Using fallback culprit department because session value is missing.')
  return fallback
}

function normalizeOnionContent(data, culpritDepartment) {
  const forumPosts = Array.isArray(data?.forumPosts) ? data.forumPosts : []
  const marketListings = Array.isArray(data?.marketListings) ? data.marketListings : []
  const deptNeedle = culpritDepartment.toLowerCase()

  const hasDeptListing = marketListings.some((listing) => {
    const haystack = `${listing?.title || ''} ${listing?.description || ''}`.toLowerCase()
    return haystack.includes(deptNeedle)
  })
  if (hasDeptListing) return { forumPosts, marketListings }

  if (marketListings.length > 0) {
    marketListings[0] = {
      ...marketListings[0],
      title: `${marketListings[0].title || 'Stolen credentials'} — ${culpritDepartment} access`,
      description: `${marketListings[0].description || ''} Linked to NovaCorp ${deptNeedle} department credentials.`.trim(),
    }
    console.warn('[Onion] Injected culprit department into listing data to keep puzzle solvable.')
    return { forumPosts, marketListings }
  }

  console.warn('[Onion] Onion room data missing listings; using default content with fallback department clue.')
  return getDefaultContent(culpritDepartment)
}

function submitOnionEvidence(listing) {
  const culprit = store.sessionState?.culprit
  if (!culprit || !listing) return
  const dept = culprit.department?.toLowerCase()
  const haystack = `${listing.title || ''} ${listing.description || ''}`.toLowerCase()
  const matchesDept = !!dept && haystack.includes(dept)

  if (matchesDept) {
    onionSubmitResult.value[listing.id] = 'correct'
    store.addClue(
      'onion-market',
      'The Onion',
      `Dark web listing found: "${listing.title}" — relates to ${culprit.department} department.`
    )
    store.markRoomComplete('onion')
    return
  }
  onionSubmitResult.value[listing.id] = 'wrong'
}

function getDefaultContent(culpritDepartment) {
  const dept = culpritDepartment
  const deptLower = dept.toLowerCase()
  return {
    forumPosts: [
      {
        id: 'post-001',
        handle: 'g0stwr1ter',
        timestamp: '2025-03-04T08:22:00',
        category: 'leaks',
        title: 'NovaCorp internal docs — Project Nova is a front',
        body: 'I worked there for 3 years. Project Nova is being used to launder funds through a subsidiary. The CFO knows. Someone on the inside is helping exfiltrate data. Check the access logs.',
        replies: 14,
      },
      {
        id: 'post-002',
        handle: 'anon_watcher',
        timestamp: '2025-03-03T14:05:00',
        category: 'general',
        title: 'Did something happen at NovaCorp last night?',
        body: 'Multiple encrypted comms picked up from novacorp.com domain between 01:00 and 03:00 UTC. Unusual patterns. Anyone have more info?',
        replies: 7,
      },
      {
        id: 'post-003',
        handle: 'xd4rk_n3t',
        timestamp: '2025-03-02T22:11:00',
        category: 'market',
        title: `[SALE] NovaCorp ${dept} wing credentials — fresh dump`,
        body: `Selling access to a ${dept} dept account. Level 5 clearance. Verified. Contact via escrow.`,
        replies: 3,
      },
    ],
    marketListings: [
      {
        id: 'listing-001',
        seller: 'xd4rk_n3t',
        title: `NovaCorp ${dept} Dept — Level 5 Access`,
        price: '0.045 BTC',
        description: `Fresh credentials for a NovaCorp ${deptLower} department account. High clearance. Vault access included.`,
        category: 'credentials',
      },
      {
        id: 'listing-002',
        seller: 'g0stwr1ter',
        title: 'NovaCorp Project Nova Full Document Dump',
        price: '0.12 BTC',
        description: 'Complete internal documentation for Project Nova. Exposes executive misconduct. novacorp insider source.',
        category: 'data',
      },
    ],
  }
}
</script>

<style scoped>
.onion-view {
  --bg-primary: #000000;
  --bg-secondary: #050505;
  --text-primary: #00ff41;
  --text-secondary: #00cc33;
  --border-color: #003300;

  display: flex;
  flex-direction: column;
  height: 100%;
  background: #000000;
  color: #00ff41;
  font-family: var(--font-mono);
  overflow: hidden;
}

.onion-header {
  border-bottom: 1px dotted #003300;
  padding: 10px 16px;
  background: #030303;
}

.onion-logo {
  font-size: 16px;
  font-weight: 700;
  letter-spacing: 4px;
  color: #00ff41;
  margin-bottom: 8px;
}

.onion-dot {
  color: #00cc33;
  animation: blink-anim 1.5s step-end infinite;
}

@keyframes blink-anim {
  50% { opacity: 0; }
}

.onion-tagline {
  font-size: 10px;
  color: #006600;
  letter-spacing: 1px;
  margin-left: 12px;
}

.onion-nav {
  display: flex;
  gap: 16px;
}

.onion-tab {
  font-size: 12px;
  letter-spacing: 2px;
  color: #006600;
  cursor: pointer;
  padding: 2px 0;
  border-bottom: 1px dotted transparent;
}

.onion-tab:hover {
  color: #00cc33;
}

.onion-tab.active {
  color: #00ff41;
  border-bottom-color: #00ff41;
}

.onion-body {
  flex: 1;
  overflow-y: auto;
  padding: 16px;
}

.onion-loading {
  color: #00cc33;
  font-size: 13px;
  padding: 24px;
}

.blink {
  animation: blink-anim 0.8s step-end infinite;
}

/* Forum */
.forum-header, .market-header {
  font-size: 11px;
  letter-spacing: 2px;
  color: #006600;
  border-bottom: 1px dotted #003300;
  padding-bottom: 8px;
  margin-bottom: 12px;
}

.post-item {
  border: 1px dotted #003300;
  padding: 12px;
  margin-bottom: 8px;
  cursor: pointer;
  transition: border-color 0.15s;
}

.post-item:hover {
  border-color: #006600;
}

.post-item.selected {
  border-color: #00ff41;
  background: #020a02;
}

.post-meta {
  display: flex;
  gap: 16px;
  font-size: 11px;
  color: #006600;
  margin-bottom: 6px;
}

.post-handle {
  color: #00cc33;
}

.post-title {
  font-size: 13px;
  color: #00ff41;
  line-height: 1.4;
}

.post-body {
  margin-top: 10px;
  font-size: 12px;
  color: #00cc33;
  line-height: 1.7;
  border-top: 1px dotted #003300;
  padding-top: 10px;
}

/* Market */
.listing-item {
  border: 1px dotted #003300;
  padding: 12px;
  margin-bottom: 8px;
  cursor: pointer;
}

.listing-item.selected {
  border-color: #00ff41;
  background: #020a02;
}

.listing-top {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 4px;
}

.listing-title {
  font-size: 13px;
  color: #00ff41;
}

.listing-price {
  font-size: 13px;
  color: #00cc33;
  font-weight: 700;
}

.listing-seller {
  font-size: 11px;
  color: #006600;
  margin-bottom: 8px;
}

.listing-desc {
  font-size: 12px;
  color: #009900;
  line-height: 1.6;
  margin-bottom: 8px;
}

.listing-cat {
  font-size: 10px;
  color: #004400;
  letter-spacing: 1px;
}

.listing-actions {
  margin-top: 10px;
  display: flex;
  align-items: center;
  gap: 12px;
}

.submit-evidence-btn {
  font-size: 12px;
  font-weight: 600;
  font-family: var(--font-mono);
  padding: 4px 12px;
  background: rgba(0, 255, 65, 0.08);
  border: 1px solid #00ff41;
  color: #00ff41;
  border-radius: 3px;
  cursor: pointer;
}

.submit-evidence-btn:hover {
  background: rgba(0, 255, 65, 0.16);
}

.submit-correct {
  font-size: 12px;
  font-weight: 600;
  color: #4fdc7d;
}

.submit-wrong {
  font-size: 12px;
  font-weight: 600;
  color: #c85e5e;
}
</style>
