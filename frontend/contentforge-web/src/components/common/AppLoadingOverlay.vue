<script setup lang="ts">
import { useLoading } from '@/composables/useLoading';
import AppSpinner from '@/components/common/AppSpinner.vue';

const { isGlobalLoading } = useLoading();
</script>

<template>
  <div
    v-if="isGlobalLoading"
    class="app-loading-overlay"
    role="alertdialog"
    aria-modal="true"
    aria-busy="true"
    aria-live="assertive"
    aria-label="Loading"
  >
    <div class="app-loading-overlay__panel">
      <AppSpinner size="lg" label="Loading application data" />
      <p>Loading…</p>
    </div>
  </div>
</template>
