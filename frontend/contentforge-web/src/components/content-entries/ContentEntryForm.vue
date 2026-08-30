<script setup lang="ts">
import ContentEntryFieldInput from '@/components/content-entries/ContentEntryFieldInput.vue';
import type { ContentType, ContentTypeField } from '@/types/contentTypes';
import type { ContentFieldValue } from '@/types/contentEntries';

const data = defineModel<Record<string, ContentFieldValue>>('data', { required: true });

defineProps<{
  contentType: ContentType;
  fields: ContentTypeField[];
  disabled?: boolean;
  fieldErrors?: (fieldName: string) => string[];
}>();

function relationTargetForField(field: ContentTypeField): string | null {
  return field.configuration.relationTarget;
}
</script>

<template>
  <div class="entry-form stack-form">
    <ContentEntryFieldInput
      v-for="field in fields"
      :key="field.id"
      :model-value="data[field.name]"
      :field="field"
      :disabled="disabled"
      :errors="fieldErrors?.(field.name)"
      :relation-target-type-id="relationTargetForField(field)"
      @update:model-value="(value) => (data[field.name] = value)"
    />
  </div>
</template>
