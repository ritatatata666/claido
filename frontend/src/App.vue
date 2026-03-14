<template>
  <Teleport to="body">
    <div v-if="showSplatters" class="blood-splatter-container" aria-hidden="true">
      <div class="blood-splatter-global blood-splatter-global--1"><img :src="bloodSplatter1" alt="" /></div>
      <div class="blood-splatter-global blood-splatter-global--2"><img :src="bloodSplatter2" alt="" /></div>
      <div class="blood-splatter-global blood-splatter-global--3"><img :src="bloodSplatter1" alt="" /></div>
      <div class="blood-splatter-global blood-splatter-global--4"><img :src="bloodSplatter2" alt="" /></div>
      <div class="blood-splatter-global blood-splatter-global--5"><img :src="bloodSplatter1" alt="" /></div>
    </div>
  </Teleport>

  <div class="app-shell" :class="{ 'is-hub': isHub, 'is-landing': isLanding }">
    <div class="app-shell__board" aria-hidden="true">
      <span class="thread thread--one"></span>
      <span class="thread thread--two"></span>
      <span class="thread thread--three"></span>
      <span class="pin pin--red pin--one"></span>
      <span class="pin pin--blue pin--two"></span>
      <span class="pin pin--gold pin--three"></span>
    </div>

    <div class="app-shell__content">
      <RouterView />
      <ClueNotification />
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue'
import { RouterView, useRoute } from 'vue-router'
import ClueNotification from './components/ClueNotification.vue'
import bloodSplatter1 from '../images/bloodSplatter1.png'
import bloodSplatter2 from '../images/bloodSplatter2.png'

const route = useRoute()
const isHub = computed(() => route.path === '/hub')
const isLanding = computed(() => route.path === '/')
const showSplatters = computed(() => isHub.value || isLanding.value)
</script>

<style scoped>
.app-shell {
  position: relative;
  min-height: 100%;
}

.app-shell__board {
  position: fixed;
  inset: 0;
  pointer-events: none;
  z-index: 0;
  overflow: hidden;
}

.app-shell__content {
  position: relative;
  z-index: 1;
  min-height: 100%;
}

.thread {
  position: absolute;
  height: 2px;
  border-radius: 999px;
  background: linear-gradient(90deg, rgba(128, 22, 26, 0.25), rgba(166, 26, 33, 0.7), rgba(128, 22, 26, 0.25));
  box-shadow: 0 0 0 1px rgba(82, 13, 15, 0.1), 0 2px 8px rgba(122, 21, 25, 0.18);
  opacity: 0.45;
}

.thread--one {
  top: 16%;
  left: 8%;
  width: 36%;
  transform: rotate(9deg);
}

.thread--two {
  top: 28%;
  right: 10%;
  width: 30%;
  transform: rotate(-18deg);
}

.thread--three {
  bottom: 18%;
  left: 34%;
  width: 28%;
  transform: rotate(-12deg);
}

.pin {
  position: absolute;
  width: 12px;
  height: 12px;
  border-radius: 50%;
  box-shadow: 0 2px 6px rgba(0, 0, 0, 0.28), inset 0 1px 1px rgba(255, 255, 255, 0.6);
  opacity: 0.7;
}

.pin--red { background: #c9463d; }
.pin--blue { background: #4d78a5; }
.pin--gold { background: #b78a2a; }

.pin--one {
  top: 15%;
  left: 10%;
}

.pin--two {
  top: 25%;
  right: 12%;
}

.pin--three {
  bottom: 17%;
  left: 58%;
}
</style>
