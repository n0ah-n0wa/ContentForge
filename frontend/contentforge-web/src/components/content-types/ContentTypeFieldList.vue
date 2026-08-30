<script setup lang="ts">
import AppButton from '@/components/common/AppButton.vue';
import { getFieldTypeLabel } from '@/config/fieldTypeDefinitions';
import type { ContentTypeField } from '@/types/contentTypes';

const props = defineProps<{
  fields: ContentTypeField[];
  selectedFieldName: string | null;
  canUpdate: boolean;
  busyFieldName?: string | null;
}>();

const emit = defineEmits<{
  select: [fieldName: string];
  moveUp: [fieldName: string];
  moveDown: [fieldName: string];
  remove: [fieldName: string];
  rename: [fieldName: string];
}>();

function sortedFields(fields: ContentTypeField[]): ContentTypeField[] {
  return [...fields].sort((left, right) => left.sortOrder - right.sortOrder);
}

function canMoveUp(index: number): boolean {
  return index > 0 && props.canUpdate;
}

function canMoveDown(index: number, fields: ContentTypeField[]): boolean {
  return index < fields.length - 1 && props.canUpdate;
}
</script>

<template>
  <section class="field-list">
    <header class="section-header">
      <div>
        <h3>Fields</h3>
        <p>Configure schema fields and ordering for this content type.</p>
      </div>
    </header>

    <p v-if="fields.length === 0" class="empty-state">No fields defined yet.</p>

    <ul v-else class="field-list__items">
      <li
        v-for="(field, index) in sortedFields(fields)"
        :key="field.id"
        class="field-list__item"
        :class="{ 'field-list__item--selected': selectedFieldName === field.name }"
      >
        <button
          type="button"
          class="field-list__summary"
          @click="emit('select', field.name)"
        >
          <strong>{{ field.displayName }}</strong>
          <span>{{ field.name }} · {{ getFieldTypeLabel(field.fieldType) }}</span>
          <span>Order {{ field.sortOrder }}</span>
        </button>

        <div v-if="canUpdate" class="field-list__actions">
          <AppButton
            variant="ghost"
            type="button"
            :disabled="!canMoveUp(index) || busyFieldName === field.name"
            @click="emit('moveUp', field.name)"
          >
            Up
          </AppButton>
          <AppButton
            variant="ghost"
            type="button"
            :disabled="!canMoveDown(index, sortedFields(fields)) || busyFieldName === field.name"
            @click="emit('moveDown', field.name)"
          >
            Down
          </AppButton>
          <AppButton
            variant="ghost"
            type="button"
            :disabled="busyFieldName === field.name"
            @click="emit('rename', field.name)"
          >
            Rename
          </AppButton>
          <AppButton
            variant="ghost"
            type="button"
            :disabled="busyFieldName === field.name"
            @click="emit('remove', field.name)"
          >
            Remove
          </AppButton>
        </div>
      </li>
    </ul>
  </section>
</template>
