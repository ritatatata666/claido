<template>
  <RouterView />
  <ClueNotification />
</template>

<script setup>
import { RouterView } from 'vue-router'
import ClueNotification from './components/ClueNotification.vue'
</script>
