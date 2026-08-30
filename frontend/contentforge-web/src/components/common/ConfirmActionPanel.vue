<script setup lang="ts">
import AppButton from '@/components/common/AppButton.vue';

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
</script>

<template>
  <section class="confirm-panel" role="dialog" aria-modal="true" :aria-label="title">
    <h3>{{ title }}</h3>
    <p>{{ message }}</p>

    <label v-if="requireChangeSummary" class="form-field">
      <span>Change summary</span>
      <input
        v-model="changeSummary"
        type="text"
        placeholder="Describe why this action is being taken"
        :disabled="loading"
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
