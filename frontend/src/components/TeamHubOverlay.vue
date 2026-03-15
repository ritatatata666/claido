<template>
  <div v-if="store.teamMode === 'team'" class="team-overlay" aria-hidden="true">
    <span class="t-thread t-thread--1"></span>
    <span class="t-thread t-thread--2"></span>
    <span class="t-thread t-thread--3"></span>
    <span class="t-thread t-thread--4"></span>
    <span class="t-pin t-pin--red t-pin--1"></span>
    <span class="t-pin t-pin--blue t-pin--2"></span>
    <span class="t-pin t-pin--gold t-pin--3"></span>
    <span class="t-pin t-pin--red t-pin--4"></span>
  </div>
</template>

<script setup>
import { useGameStore } from '../stores/gameStore.js'
const store = useGameStore()
</script>

<style scoped>
.team-overlay {
  position: fixed;
  inset: 0;
  pointer-events: none;
  z-index: 0;
  overflow: hidden;
}

.t-thread {
  position: absolute;
  height: 2px;
  border-radius: 999px;
  background: linear-gradient(90deg, rgba(128, 22, 26, 0.2), rgba(166, 26, 33, 0.6), rgba(128, 22, 26, 0.2));
  box-shadow: 0 0 0 1px rgba(82, 13, 15, 0.08), 0 2px 8px rgba(122, 21, 25, 0.14);
  opacity: 0.4;
}

.t-thread--1 {
  top: 14%;
  left: 5%;
  width: 42%;
  transform: rotate(5deg);
}

.t-thread--2 {
  top: 36%;
  right: 4%;
  width: 34%;
  transform: rotate(-12deg);
}

.t-thread--3 {
  bottom: 28%;
  left: 20%;
  width: 50%;
  transform: rotate(3deg);
}

.t-thread--4 {
  bottom: 12%;
  left: 8%;
  width: 30%;
  transform: rotate(-8deg);
}

.t-pin {
  position: absolute;
  width: 10px;
  height: 10px;
  border-radius: 50%;
  box-shadow: 0 2px 5px rgba(0, 0, 0, 0.25), inset 0 1px 1px rgba(255, 255, 255, 0.5);
  opacity: 0.6;
}

.t-pin--red { background: #c9463d; }
.t-pin--blue { background: #4d78a5; }
.t-pin--gold { background: #b78a2a; }

.t-pin--1 {
  top: 13%;
  left: 6%;
}

.t-pin--2 {
  top: 34%;
  right: 6%;
}

.t-pin--3 {
  bottom: 27%;
  left: 44%;
}

.t-pin--4 {
  bottom: 11%;
  left: 9%;
}
</style>
