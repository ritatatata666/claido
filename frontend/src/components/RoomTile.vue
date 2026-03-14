<template>
  <button
    class="room-tile"
    :class="[
      `room-tile--${room.theme ?? 'green'}`,
      { 'room-tile--active': active },
    ]"
    @click="handleClick"
    :aria-label="`${room.roomName} room, status ${statusLabel}`"
    :style="[roomBackgroundStyle, customStyles]"
  >
    <div class="room-overlay" aria-hidden="true"></div>
    <div class="room-content">
      <span class="room-name-label">{{ room.roomName }}</span>
      <span class="room-challenge">{{ room.challengeTitle }}</span>
    </div>
    <span class="room-pill">{{ room.id.toUpperCase() }}</span>
  </button>
</template>

<script setup>
import { computed } from 'vue'

const props = defineProps({
  room: { type: Object, required: true },
  active: { type: Boolean, default: false },
  customStyles: { type: Object, default: () => ({}) },
})

const emit = defineEmits(['select'])

const statusLabel = computed(() => props.room.status ?? 'Active')

const roomBackgroundStyle = computed(() => ({
  backgroundImage: `linear-gradient(180deg, rgba(0, 0, 0, 0.15), rgba(0, 0, 0, 0.45)), url(${props.room.backgroundImage})`,
  backgroundSize: 'cover',
  backgroundPosition: 'center',
}))

function handleClick() {
  emit('select')
}
</script>

<style scoped>
.room-tile {
  position: relative;
  border-radius: 12px;
  border: 1px solid rgba(255, 255, 255, 0.12);
  padding: 1rem;
  cursor: pointer;
  background-color: #050608;
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  overflow: hidden;
  transition: transform 0.3s ease, box-shadow 0.3s ease, border-color 0.3s ease;
  box-shadow: inset 0 0 45px rgba(0, 0, 0, 0.85), 0 10px 30px rgba(0, 0, 0, 0.9);
}

.room-tile:hover,
.room-tile:focus-visible {
  transform: translateY(-6px) scale(1.01);
  border-color: rgba(255, 255, 255, 0.4);
  box-shadow:
    0 22px 45px rgba(0, 0, 0, 0.9),
    0 0 45px rgba(255, 255, 255, 0.12);
}

.room-tile--active {
  animation: pulseGlow 3s ease-in-out infinite alternate;
}

.room-tile--active:hover {
  animation: pulseGlow 2.5s ease-in-out infinite alternate;
}

.room-overlay {
  position: absolute;
  inset: 0;
  background: linear-gradient(180deg, rgba(0, 0, 0, 0.25), rgba(0, 0, 0, 0.55));
  z-index: 1;
}

.room-content {
  position: relative;
  z-index: 2;
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  height: 100%;
}

.room-name-label {
  font-size: 0.7rem;
  letter-spacing: 0.2rem;
  text-transform: uppercase;
  color: rgba(255, 255, 255, 0.7);
  margin-bottom: 0.4rem;
}

.room-challenge {
  flex: 1;
  display: flex;
  justify-content: center;
  align-items: center;
  font-size: 1.15rem;
  font-weight: 700;
  text-transform: uppercase;
  letter-spacing: 0.35rem;
  color: #fff5c1;
  text-align: center;
}

.room-pill {
  position: relative;
  z-index: 2;
  align-self: flex-start;
  font-size: 0.65rem;
  letter-spacing: 0.5rem;
  text-transform: uppercase;
  color: rgba(255, 255, 255, 0.5);
}

.room-tile--green {
  border-color: rgba(46, 139, 87, 0.35);
  box-shadow: inset 0 0 25px rgba(46, 139, 87, 0.15),
    0 20px 50px rgba(46, 139, 87, 0.35);
}

.room-tile--gold {
  border-color: rgba(230, 181, 72, 0.35);
  box-shadow: inset 0 0 25px rgba(230, 181, 72, 0.15),
    0 20px 50px rgba(230, 181, 72, 0.35);
}

.room-tile--brown {
  border-color: rgba(173, 102, 43, 0.35);
  box-shadow: inset 0 0 25px rgba(173, 102, 43, 0.2),
    0 20px 50px rgba(173, 102, 43, 0.4);
}

@keyframes pulseGlow {
  from {
    box-shadow:
      inset 0 0 40px rgba(127, 255, 187, 0.4),
      0 20px 50px rgba(127, 255, 187, 0.15);
  }
  to {
    box-shadow:
      inset 0 0 60px rgba(127, 255, 187, 0.6),
      0 30px 70px rgba(127, 255, 187, 0.3);
  }
}
</style>
