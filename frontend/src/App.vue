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

  <Teleport to="body">
    <div v-if="showCreepyOverlay" :class="['creepy-overlay', { 'is-hub': isHub, 'is-landing': isLanding, 'is-vault': isVault }]" aria-hidden="true">
      <img :src="lightLeakImg" alt="" class="creepy-overlay__img creepy-overlay__img--lightleak" />
      <img :src="shadowImg" alt="" class="creepy-overlay__img creepy-overlay__img--shadow" />
      <img :src="fingerprintImg" alt="" class="creepy-overlay__img creepy-overlay__img--fingerprint" />
      <img :src="handprintImg" alt="" class="creepy-overlay__img creepy-overlay__img--handprint" />
    </div>
  </Teleport>

  <div class="app-shell" :class="{ 'is-hub': isHub, 'is-landing': isLanding, 'is-vault': isVault }">
    <div v-if="isLanding" class="app-shell__board" aria-hidden="true">
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
import fingerprintImg from '../images/fingerprint.png'
import handprintImg from '../images/handprint.png'
import lightLeakImg from '../images/lightleak.webp'
import shadowImg from '../images/shadow.png'

const route = useRoute()
const isHub = computed(() => route.path === '/hub')
const isLanding = computed(() => route.path === '/' || route.path === '/history')
const isVault = computed(() => route.path === '/vault')
const showSplatters = computed(() => isHub.value || isLanding.value || isVault.value)
const showCreepyOverlay = computed(() => isHub.value || isLanding.value || isVault.value)
</script>

<style scoped>
.app-shell {
  position: relative;
  height: 100%;
  overflow: hidden;
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
  height: 100%;
  overflow-y: auto;
  overflow-x: hidden;
}

.creepy-overlay {
  position: fixed;
  inset: 0;
  pointer-events: none;
  z-index: 4;
  overflow: hidden;
}

.creepy-overlay__img {
  position: absolute;
  user-select: none;
  -webkit-user-drag: none;
  filter: saturate(0.65) contrast(1.05);
}

.creepy-overlay__img--lightleak {
  top: -8%;
  left: -4%;
  width: 55vw;
  max-width: 820px;
  opacity: 0.1;
  mix-blend-mode: screen;
}

.creepy-overlay__img--shadow {
  left: -10%;
  bottom: -11%;
  width: 24vw;
  max-width: 360px;
  opacity: 0.11;
  transform: rotate(5deg);
  mix-blend-mode: multiply;
}

.creepy-overlay.is-hub .creepy-overlay__img--shadow {
  opacity: 0.055;
}

.creepy-overlay__img--fingerprint {
  top: 22%;
  right: 7%;
  width: 150px;
  opacity: 0.14;
  transform: rotate(8deg);
  mix-blend-mode: multiply;
}

.creepy-overlay__img--handprint {
  top: 11%;
  right: 1.5%;
  width: 150px;
  opacity: 0.06;
  transform: rotate(-14deg);
  mix-blend-mode: multiply;
}

@media (max-width: 900px) {
  .creepy-overlay__img--shadow {
    width: 30vw;
    left: -14%;
    bottom: -8%;
    opacity: 0.1;
  }

  .creepy-overlay.is-hub .creepy-overlay__img--shadow {
    opacity: 0.045;
  }

  .creepy-overlay__img--fingerprint {
    width: 120px;
    right: 3%;
  }

  .creepy-overlay__img--handprint {
    width: 106px;
    top: 10%;
    right: -2%;
    opacity: 0.05;
    transform: rotate(-12deg);
  }
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
  top: 55%;
  left: 8%;
  width: 36%;
  transform: rotate(9deg);
}

.thread--two {
  top: 70%;
  right: 10%;
  width: 30%;
  transform: rotate(-18deg);
}

.thread--three {
  bottom: 8%;
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
  top: 54%;
  left: 10%;
}

.pin--two {
  top: 68%;
  right: 12%;
}

.pin--three {
  bottom: 7%;
  left: 58%;
}
</style>
