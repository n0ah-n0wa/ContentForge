<script setup lang="ts">
import { ref, useId } from 'vue';
import AppButton from '@/components/common/AppButton.vue';
import type { ConcurrencyConflictProblem } from '@/api/concurrency';
import { describeConcurrencyConflict } from '@/api/concurrency';
import { useModalDialog } from '@/composables/useModalDialog';

defineProps<{
  conflict: ConcurrencyConflictProblem;
  loading?: boolean;
}>();

const emit = defineEmits<{
  reload: [];
  dismiss: [];
}>();

const rootRef = ref<HTMLElement | null>(null);
const titleId = useId();
const descriptionId = useId();

useModalDialog({
  rootRef,
  onEscape: () => emit('dismiss'),
});
</script>

<template>
  <section
    ref="rootRef"
    class="confirm-panel concurrency-panel"
    data-testid="concurrency-conflict"
    role="alertdialog"
    aria-modal="true"
    :aria-labelledby="titleId"
    :aria-describedby="descriptionId"
  >
    <div>
      <h3 :id="titleId">Concurrency conflict</h3>
      <p :id="descriptionId">{{ describeConcurrencyConflict(conflict) }}</p>
      <p class="entry-metadata__hint">
        Last updated at {{ new Date(conflict.updatedAt).toLocaleString() }}.
      </p>
    </div>
    <div class="confirm-panel__actions">
      <AppButton variant="secondary" type="button" :disabled="loading" @click="emit('dismiss')">
        Keep editing
      </AppButton>
      <AppButton type="button" data-autofocus :disabled="loading" @click="emit('reload')">
        Reload latest
      </AppButton>
    </div>
  </section>
</template>
