<script setup lang="ts">
import { ref, useId } from 'vue';
import AppButton from '@/components/common/AppButton.vue';
import { useModalDialog } from '@/composables/useModalDialog';

defineProps<{
  title: string;
  message: string;
  confirmLabel?: string;
  loading?: boolean;
}>();

const emit = defineEmits<{
  confirm: [];
  cancel: [];
}>();

const rootRef = ref<HTMLElement | null>(null);
const titleId = useId();
const messageId = useId();

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
    <div class="confirm-panel__actions">
      <AppButton variant="secondary" :disabled="loading" @click="emit('cancel')">
        Cancel
      </AppButton>
      <AppButton data-autofocus :loading="loading" @click="emit('confirm')">
        {{ confirmLabel ?? 'Confirm change' }}
      </AppButton>
    </div>
  </section>
</template>
