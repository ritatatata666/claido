<template>
  <section class="mansion-map">
    <div class="mansion-map__fog" aria-hidden="true"></div>
    <div class="mansion-map__glimmer" aria-hidden="true"></div>
    <div class="mansion-map__grid-lines" aria-hidden="true"></div>
    <div class="mansion-map__corridor hallway-horizontal"></div>
    <div class="mansion-map__corridor hallway-vertical"></div>

    <div class="mansion-map__grid">
      <RoomTile
        v-for="room in rooms"
        :key="room.id"
        :room="room"
        :active="room.id === activeRoom"
        @select="() => emit('room-select', room.id)"
        :custom-styles="{ gridArea: room.area }"
      />
    </div>
  </section>
</template>

<script setup>
import RoomTile from './RoomTile.vue'

const props = defineProps({
  rooms: { type: Array, required: true },
  activeRoom: { type: String, default: '' },
})

const emit = defineEmits(['room-select'])
</script>

<style scoped>
.mansion-map {
  position: relative;
  border-radius: 30px;
  background: linear-gradient(135deg, rgba(5, 8, 12, 0.88), rgba(2, 3, 5, 0.9));
  padding: 2rem;
  overflow: hidden;
  min-height: 540px;
  box-shadow: inset 0 0 60px rgba(0, 0, 0, 0.9), 0 30px 60px rgba(0, 0, 0, 0.85);
}

.mansion-map__fog {
  position: absolute;
  inset: 1rem;
  background: radial-gradient(circle at 30% 30%, rgba(255, 255, 255, 0.06), transparent 40%),
    radial-gradient(circle at 70% 70%, rgba(255, 255, 255, 0.05), transparent 50%);
  filter: blur(6px);
  opacity: 0.4;
  pointer-events: none;
}

.mansion-map__glimmer {
  position: absolute;
  inset: 0;
  background: linear-gradient(120deg, rgba(255, 255, 255, 0.08), transparent 45%);
  mix-blend-mode: screen;
  opacity: 0.6;
  animation: drift 14s ease-in-out infinite;
}

.mansion-map__grid-lines {
  position: absolute;
  inset: 0;
  background-image: linear-gradient(0deg, rgba(255, 255, 255, 0.04) 1px, transparent 1px),
    linear-gradient(90deg, rgba(255, 255, 255, 0.04) 1px, transparent 1px);
  background-size: 120px 120px;
  opacity: 0.2;
  pointer-events: none;
}

.mansion-map__corridor {
  position: absolute;
  background: rgba(255, 255, 255, 0.04);
  border-radius: 999px;
  box-shadow: 0 0 12px rgba(255, 255, 255, 0.1);
}

.hallway-horizontal {
  inset: 45% auto auto 16%;
  width: 68%;
  height: 12px;
}

.hallway-vertical {
  inset: 25% auto 20% 48%;
  width: 12px;
  height: 55%;
}

.mansion-map__grid {
  position: relative;
  display: grid;
  grid-template-columns: repeat(3, minmax(0, 1fr));
  grid-template-rows: repeat(4, minmax(160px, 1fr));
  grid-template-areas:
    "kitchen ballroom conservatory"
    "dining hall billiard"
    "lounge hall library"
    ". study .";
  gap: 18px;
  z-index: 1;
}

@media (max-width: 900px) {
  .mansion-map {
    padding: 1.5rem;
  }

  .mansion-map__grid {
    grid-template-columns: repeat(3, minmax(0, 1fr));
    grid-template-rows: repeat(10, 110px);
  }

  .mansion-map__corridor {
    display: none;
  }
}

@media (max-width: 640px) {
  .mansion-map {
    padding: 1rem;
  }

  .mansion-map__grid {
    grid-template-columns: repeat(2, minmax(0, 1fr));
    grid-template-rows: repeat(12, 100px);
  }
}

@keyframes drift {
  0% {
    transform: translate3d(-8px, 0, 0);
  }
  50% {
    transform: translate3d(12px, -4px, 0);
  }
  100% {
    transform: translate3d(-8px, 6px, 0);
  }
}
</style>
