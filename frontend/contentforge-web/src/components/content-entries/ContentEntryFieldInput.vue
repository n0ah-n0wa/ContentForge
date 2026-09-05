<script setup lang="ts">
import { computed, ref, useId, watch } from 'vue';
import { FieldType, type ContentTypeField } from '@/types/contentTypes';
import type { ContentFieldValue } from '@/types/contentEntries';
import { sanitizeRichText, richTextToPlainText } from '@/utils/richTextSanitizer';
import { getRelationOptions } from '@/composables/useRelationOptions';
import MediaFieldSelector from '@/components/media/MediaFieldSelector.vue';

const model = defineModel<ContentFieldValue>({ required: true });

const props = defineProps<{
  field: ContentTypeField;
  disabled?: boolean;
  errors?: string[];
  relationTargetTypeId?: string | null;
}>();

const emit = defineEmits<{
  blur: [];
}>();

const relationOptions = ref<Array<{ id: string; label: string }>>([]);
const relationLoading = ref(false);
const richTextMode = ref<'edit' | 'preview'>('edit');
const richTextEditor = ref<HTMLElement | null>(null);
const fieldId = useId();
const labelId = `${fieldId}-label`;
const errorsId = `${fieldId}-errors`;

const hasErrors = computed(() => (props.errors?.length ?? 0) > 0);
const describedBy = computed(() => (hasErrors.value ? errorsId : undefined));

const stringValue = computed({
  get: () =>
    typeof model.value === 'string' ? model.value : model.value == null ? '' : String(model.value),
  set: (value: string) => {
    model.value = value;
  },
});

const numberValue = computed({
  get: () =>
    model.value === null || model.value === undefined || model.value === ''
      ? ''
      : String(model.value),
  set: (value: string) => {
    model.value = value === '' ? null : Number(value);
  },
});

const booleanValue = computed({
  get: () => Boolean(model.value),
  set: (value: boolean) => {
    model.value = value;
  },
});

const jsonValue = computed({
  get: () => {
    if (typeof model.value === 'string') {
      return model.value;
    }
    if (model.value === null || model.value === undefined) {
      return '';
    }
    return JSON.stringify(model.value, null, 2);
  },
  set: (value: string) => {
    model.value = value;
  },
});

const multiSelectValue = computed({
  get: () => (Array.isArray(model.value) ? model.value.map(String) : []),
  set: (values: string[]) => {
    model.value = values;
  },
});

const mediaValue = computed({
  get: () => (typeof model.value === 'string' ? model.value : ''),
  set: (value: string) => {
    model.value = value;
  },
});

const mediaMultipleValue = computed({
  get: () => (Array.isArray(model.value) ? model.value.map(String) : []),
  set: (values: string[]) => {
    model.value = values;
  },
});

const sanitizedPreview = computed(() => richTextToPlainText(stringValue.value));

const selectOptions = computed(() => props.field.configuration.options);

const isComplexField = computed(() =>
  [
    FieldType.RichText,
    FieldType.Media,
    FieldType.MediaMultiple,
    FieldType.MultiSelect,
    FieldType.RelationMultiple,
    FieldType.Boolean,
  ].includes(props.field.fieldType),
);

async function loadRelationOptions(): Promise<void> {
  if (!props.relationTargetTypeId) {
    relationOptions.value = [];
    return;
  }

  relationLoading.value = true;
  try {
    relationOptions.value = await getRelationOptions(props.relationTargetTypeId);
  } catch {
    relationOptions.value = [];
  } finally {
    relationLoading.value = false;
  }
}

function toggleMultiSelect(option: string, checked: boolean): void {
  const current = new Set(multiSelectValue.value);
  if (checked) {
    current.add(option);
  } else {
    current.delete(option);
  }
  multiSelectValue.value = [...current];
}

function toggleGuidSelection(id: string, checked: boolean): void {
  const current = new Set(multiSelectValue.value);
  if (checked) {
    current.add(id);
  } else {
    current.delete(id);
  }
  multiSelectValue.value = [...current];
}

function onRichTextInput(): void {
  if (!richTextEditor.value) {
    return;
  }

  model.value = sanitizeRichText(richTextEditor.value.innerHTML);
}

function applyRichTextCommand(command: string): void {
  richTextEditor.value?.focus();
  document.execCommand(command, false);
}

watch(stringValue, (value) => {
  if (richTextEditor.value && richTextEditor.value.innerHTML !== value) {
    richTextEditor.value.innerHTML = sanitizeRichText(value);
  }
});

watch(
  () => [props.field.fieldType, props.relationTargetTypeId] as const,
  ([fieldType, relationTargetTypeId]) => {
    if (
      (fieldType === FieldType.Relation || fieldType === FieldType.RelationMultiple) &&
      relationTargetTypeId
    ) {
      void loadRelationOptions();
    }
  },
  { immediate: true },
);
</script>

<template>
  <div
    class="entry-field form-field"
    :class="{ 'form-field--invalid': hasErrors, 'entry-field--invalid': hasErrors }"
    :role="isComplexField ? 'group' : undefined"
    :aria-labelledby="isComplexField ? labelId : undefined"
    :aria-describedby="isComplexField ? describedBy : undefined"
    :aria-invalid="isComplexField && hasErrors ? 'true' : undefined"
  >
    <div :id="labelId" class="form-field__label">
      <label v-if="!isComplexField" :for="fieldId">
        {{ field.displayName }}
        <small v-if="field.configuration.isRequired">Required</small>
      </label>
      <template v-else>
        <span>
          {{ field.displayName }}
          <small v-if="field.configuration.isRequired">Required</small>
        </span>
      </template>
    </div>

    <input
      v-if="field.fieldType === FieldType.Text"
      :id="fieldId"
      v-model="stringValue"
      type="text"
      :disabled="disabled"
      :aria-invalid="hasErrors ? 'true' : undefined"
      :aria-describedby="describedBy"
      @blur="emit('blur')"
    />

    <textarea
      v-else-if="field.fieldType === FieldType.LongText"
      :id="fieldId"
      v-model="stringValue"
      rows="5"
      :disabled="disabled"
      :aria-invalid="hasErrors ? 'true' : undefined"
      :aria-describedby="describedBy"
      @blur="emit('blur')"
    />

    <div v-else-if="field.fieldType === FieldType.RichText" class="rich-text-field">
      <div
        class="rich-text-field__toolbar"
        role="toolbar"
        :aria-label="`${field.displayName} formatting`"
      >
        <button type="button" :disabled="disabled" @click="applyRichTextCommand('bold')">
          Bold
        </button>
        <button type="button" :disabled="disabled" @click="applyRichTextCommand('italic')">
          Italic
        </button>
        <button
          type="button"
          :disabled="disabled"
          @click="applyRichTextCommand('insertUnorderedList')"
        >
          List
        </button>
        <button
          type="button"
          :disabled="disabled"
          :aria-pressed="richTextMode === 'preview'"
          @click="richTextMode = richTextMode === 'edit' ? 'preview' : 'edit'"
        >
          {{ richTextMode === 'edit' ? 'Preview' : 'Edit' }}
        </button>
      </div>
      <div
        v-if="richTextMode === 'edit'"
        :id="fieldId"
        ref="richTextEditor"
        class="rich-text-field__editor"
        role="textbox"
        aria-multiline="true"
        :contenteditable="!disabled"
        :aria-labelledby="labelId"
        :aria-invalid="hasErrors ? 'true' : undefined"
        :aria-describedby="describedBy"
        @input="onRichTextInput"
        @blur="emit('blur')"
      />
      <div v-else class="rich-text-field__preview" :aria-labelledby="labelId">
        {{ sanitizedPreview }}
      </div>
    </div>

    <input
      v-else-if="field.fieldType === FieldType.Integer"
      :id="fieldId"
      v-model="numberValue"
      type="number"
      step="1"
      :disabled="disabled"
      :aria-invalid="hasErrors ? 'true' : undefined"
      :aria-describedby="describedBy"
      @blur="emit('blur')"
    />

    <input
      v-else-if="field.fieldType === FieldType.Decimal"
      :id="fieldId"
      v-model="numberValue"
      type="number"
      step="any"
      :disabled="disabled"
      :aria-invalid="hasErrors ? 'true' : undefined"
      :aria-describedby="describedBy"
      @blur="emit('blur')"
    />

    <label
      v-else-if="field.fieldType === FieldType.Boolean"
      class="form-field form-field--inline entry-field__checkbox"
      :for="fieldId"
    >
      <input
        :id="fieldId"
        v-model="booleanValue"
        type="checkbox"
        :disabled="disabled"
        :aria-invalid="hasErrors ? 'true' : undefined"
        :aria-describedby="describedBy"
        @blur="emit('blur')"
      />
      <span>Enabled</span>
    </label>

    <input
      v-else-if="field.fieldType === FieldType.Date"
      :id="fieldId"
      v-model="stringValue"
      type="date"
      :disabled="disabled"
      :aria-invalid="hasErrors ? 'true' : undefined"
      :aria-describedby="describedBy"
      @blur="emit('blur')"
    />

    <input
      v-else-if="field.fieldType === FieldType.DateTime"
      :id="fieldId"
      v-model="stringValue"
      type="datetime-local"
      :disabled="disabled"
      :aria-invalid="hasErrors ? 'true' : undefined"
      :aria-describedby="describedBy"
      @blur="emit('blur')"
    />

    <MediaFieldSelector
      v-else-if="field.fieldType === FieldType.Media"
      v-model="mediaValue"
      :disabled="disabled"
      :aria-labelledby="labelId"
      :aria-describedby="describedBy"
      :aria-invalid="hasErrors ? 'true' : undefined"
    />

    <MediaFieldSelector
      v-else-if="field.fieldType === FieldType.MediaMultiple"
      v-model="mediaMultipleValue"
      multiple
      :disabled="disabled"
      :aria-labelledby="labelId"
      :aria-describedby="describedBy"
      :aria-invalid="hasErrors ? 'true' : undefined"
    />

    <select
      v-else-if="field.fieldType === FieldType.Select"
      :id="fieldId"
      v-model="stringValue"
      :disabled="disabled"
      :aria-invalid="hasErrors ? 'true' : undefined"
      :aria-describedby="describedBy"
      @blur="emit('blur')"
    >
      <option value="">Select an option</option>
      <option v-for="option in selectOptions" :key="option" :value="option">
        {{ option }}
      </option>
    </select>

    <div
      v-else-if="field.fieldType === FieldType.MultiSelect"
      class="entry-field__options"
      role="group"
      :aria-labelledby="labelId"
    >
      <label v-for="option in selectOptions" :key="option" class="form-field form-field--inline">
        <input
          type="checkbox"
          :checked="multiSelectValue.includes(option)"
          :disabled="disabled"
          @change="toggleMultiSelect(option, ($event.target as HTMLInputElement).checked)"
        />
        <span>{{ option }}</span>
      </label>
    </div>

    <select
      v-else-if="field.fieldType === FieldType.Relation"
      :id="fieldId"
      v-model="stringValue"
      :disabled="disabled || relationLoading"
      :aria-invalid="hasErrors ? 'true' : undefined"
      :aria-describedby="describedBy"
      @blur="emit('blur')"
    >
      <option value="">Select related entry</option>
      <option v-for="option in relationOptions" :key="option.id" :value="option.id">
        {{ option.label }}
      </option>
    </select>

    <div
      v-else-if="field.fieldType === FieldType.RelationMultiple"
      class="entry-field__options"
      role="group"
      :aria-labelledby="labelId"
    >
      <p v-if="relationLoading" class="entry-field__hint">Loading related entries…</p>
      <label
        v-for="option in relationOptions"
        :key="option.id"
        class="form-field form-field--inline"
      >
        <input
          type="checkbox"
          :checked="multiSelectValue.includes(option.id)"
          :disabled="disabled"
          @change="toggleGuidSelection(option.id, ($event.target as HTMLInputElement).checked)"
        />
        <span>{{ option.label }}</span>
      </label>
    </div>

    <textarea
      v-else-if="field.fieldType === FieldType.Json"
      :id="fieldId"
      v-model="jsonValue"
      rows="8"
      spellcheck="false"
      :disabled="disabled"
      :aria-invalid="hasErrors ? 'true' : undefined"
      :aria-describedby="describedBy"
      @blur="emit('blur')"
    />

    <ul v-if="hasErrors" :id="errorsId" class="entry-field__errors" role="alert">
      <li v-for="message in errors" :key="message">{{ message }}</li>
    </ul>
  </div>
</template>
