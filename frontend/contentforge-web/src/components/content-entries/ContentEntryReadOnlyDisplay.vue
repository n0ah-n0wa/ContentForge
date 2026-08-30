<script setup lang="ts">
import { FieldType, type ContentTypeField } from '@/types/contentTypes';
import type { ContentFieldValue } from '@/types/contentEntries';
import { getFieldTypeLabel } from '@/config/fieldTypeDefinitions';
import { richTextToPlainText } from '@/utils/richTextSanitizer';

defineProps<{
  fields: ContentTypeField[];
  data: Record<string, ContentFieldValue>;
}>();

function formatValue(field: ContentTypeField, value: ContentFieldValue): string {
  if (value === null || value === undefined || value === '') {
    return '—';
  }

  if (field.fieldType === FieldType.RichText && typeof value === 'string') {
    return richTextToPlainText(value);
  }

  if (Array.isArray(value)) {
    return value.length > 0 ? value.join(', ') : '—';
  }

  if (typeof value === 'object') {
    return JSON.stringify(value, null, 2);
  }

  return String(value);
}
</script>

<template>
  <dl class="entry-readonly">
    <div v-for="field in fields" :key="field.id" class="entry-readonly__item">
      <dt>
        {{ field.displayName }}
        <small>{{ getFieldTypeLabel(field.fieldType) }}</small>
      </dt>
      <dd>
        <pre v-if="field.fieldType === FieldType.Json">{{ formatValue(field, data[field.name]) }}</pre>
        <span v-else>{{ formatValue(field, data[field.name]) }}</span>
      </dd>
    </div>
  </dl>
</template>
