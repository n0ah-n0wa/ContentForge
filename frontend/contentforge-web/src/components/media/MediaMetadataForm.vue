<script setup lang="ts">
import { reactive, watch } from 'vue';

import AppButton from '@/components/common/AppButton.vue';

const metadata = defineModel<{
  altText: string;
  title: string;
  description: string;
}>('metadata', { required: true });

defineProps<{
  disabled?: boolean;
}>();

const emit = defineEmits<{
  submit: [];
}>();

const local = reactive({
  altText: metadata.value.altText,
  title: metadata.value.title,
  description: metadata.value.description,
});

watch(
  metadata,
  (value) => {
    local.altText = value.altText;
    local.title = value.title;
    local.description = value.description;
  },
  { deep: true },
);

function applyChanges(): void {
  metadata.value = {
    altText: local.altText,
    title: local.title,
    description: local.description,
  };
  emit('submit');
}
</script>

<template>
  <form class="stack-form media-metadata-form" @submit.prevent="applyChanges">
    <label class="form-field">
      <span>Title</span>
      <input v-model="local.title" type="text" :disabled="disabled" />
    </label>
    <label class="form-field">
      <span>Alt text</span>
      <input v-model="local.altText" type="text" :disabled="disabled" />
    </label>
    <label class="form-field">
      <span>Description</span>
      <textarea v-model="local.description" rows="4" :disabled="disabled" />
    </label>
    <div class="form-actions">
      <slot name="actions">
        <AppButton type="submit" :disabled="disabled">Save metadata</AppButton>
      </slot>
    </div>
  </form>
</template>
