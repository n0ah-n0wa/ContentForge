<script setup lang="ts">
import { useAppStore } from '@/stores/appStore';

const appStore = useAppStore();
</script>

<template>
  <section class="home-view">
    <h2>{{ appStore.title }}</h2>
    <p>
      Repository foundation is ready. Business features will be implemented in subsequent phases.
    </p>
  </section>
</template>
