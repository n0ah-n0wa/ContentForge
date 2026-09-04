<script setup lang="ts">
import AppButton from '@/components/common/AppButton.vue';
import type { ConcurrencyConflictProblem } from '@/api/concurrency';
import { describeConcurrencyConflict } from '@/api/concurrency';

defineProps<{
  conflict: ConcurrencyConflictProblem;
  loading?: boolean;
}>();

const emit = defineEmits<{
  reload: [];
  dismiss: [];
}>();
</script>

<template>
  <section class="confirm-panel concurrency-panel" data-testid="concurrency-conflict">
    <div>
      <h3>Concurrency conflict</h3>
      <p>{{ describeConcurrencyConflict(conflict) }}</p>
      <p class="entry-metadata__hint">
        Last updated at {{ new Date(conflict.updatedAt).toLocaleString() }}.
      </p>
    </div>
    <div class="confirm-panel__actions">
      <AppButton variant="secondary" type="button" :disabled="loading" @click="emit('dismiss')">
        Keep editing
      </AppButton>
      <AppButton type="button" :disabled="loading" @click="emit('reload')">
        Reload latest
      </AppButton>
    </div>
  </section>
</template>
