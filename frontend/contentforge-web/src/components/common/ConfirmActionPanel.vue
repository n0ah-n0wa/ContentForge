<script setup lang="ts">
import { ref, useId } from 'vue';
import AppButton from '@/components/common/AppButton.vue';
import { useModalDialog } from '@/composables/useModalDialog';

const changeSummary = defineModel<string>('changeSummary', { default: '' });

defineProps<{
  title: string;
  message: string;
  confirmLabel?: string;
  loading?: boolean;
  requireChangeSummary?: boolean;
}>();

const emit = defineEmits<{
  confirm: [];
  cancel: [];
}>();

const rootRef = ref<HTMLElement | null>(null);
const titleId = useId();
const messageId = useId();
const summaryId = useId();

useModalDialog({
  rootRef,
  onEscape: () => emit('cancel'),
});
</script>

<template>
  <section
    ref="rootRef"
    class="confirm-panel"
    role="dialog"
    aria-modal="true"
    :aria-labelledby="titleId"
    :aria-describedby="messageId"
  >
    <h3 :id="titleId">{{ title }}</h3>
    <p :id="messageId">{{ message }}</p>

    <label v-if="requireChangeSummary" class="form-field" :for="summaryId">
      <span>Change summary</span>
      <input
        :id="summaryId"
        v-model="changeSummary"
        type="text"
        data-autofocus
        placeholder="Describe why this action is being taken"
        :disabled="loading"
        :aria-required="true"
      />
    </label>

    <div class="confirm-panel__actions">
      <AppButton variant="secondary" :disabled="loading" @click="emit('cancel')">
        Cancel
      </AppButton>
      <AppButton
        :loading="loading"
        :disabled="requireChangeSummary && !changeSummary.trim()"
        @click="emit('confirm')"
      >
        {{ confirmLabel ?? 'Confirm' }}
      </AppButton>
    </div>
  </section>
</template>
