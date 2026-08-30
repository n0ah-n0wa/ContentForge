<script setup lang="ts">
import AppButton from '@/components/common/AppButton.vue';

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
</script>

<template>
  <section class="confirm-panel" role="dialog" aria-modal="true" :aria-label="title">
    <h3>{{ title }}</h3>
    <p>{{ message }}</p>
    <div class="confirm-panel__actions">
      <AppButton variant="secondary" :disabled="loading" @click="emit('cancel')">
        Cancel
      </AppButton>
      <AppButton :loading="loading" @click="emit('confirm')">
        {{ confirmLabel ?? 'Confirm change' }}
      </AppButton>
    </div>
  </section>
</template>
