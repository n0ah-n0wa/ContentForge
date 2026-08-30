<script setup lang="ts">
import { useLoading } from '@/composables/useLoading';
import AppSpinner from '@/components/common/AppSpinner.vue';

const { isGlobalLoading } = useLoading();
</script>

<template>
  <div v-if="isGlobalLoading" class="app-loading-overlay" aria-live="polite" aria-busy="true">
    <div class="app-loading-overlay__panel">
      <AppSpinner size="lg" label="Loading application data" />
      <p>Loading…</p>
    </div>
  </div>
</template>
