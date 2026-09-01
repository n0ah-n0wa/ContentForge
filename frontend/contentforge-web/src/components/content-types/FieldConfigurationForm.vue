<script setup lang="ts">
import { computed } from 'vue';
import type { FieldConfiguration } from '@/types/contentTypes';
import { FieldType, RELATION_CARDINALITY_LABELS, RelationCardinality } from '@/types/contentTypes';
import { getFieldTypeDefinition } from '@/config/fieldTypeDefinitions';
import type { ContentType } from '@/types/contentTypes';

const configuration = defineModel<FieldConfiguration>('configuration', { required: true });

const props = defineProps<{
  fieldType: FieldType;
  relationTargets: ContentType[];
  disabled?: boolean;
}>();

const definition = computed(() => getFieldTypeDefinition(props.fieldType));
const capabilities = computed(() => definition.value.capabilities);

const cardinalityOptions = Object.entries(RELATION_CARDINALITY_LABELS).map(([value, label]) => ({
  value: Number(value) as RelationCardinality,
  label,
}));

function updateOptions(raw: string): void {
  configuration.value = {
    ...configuration.value,
    options: raw
      .split('\n')
      .map((option) => option.trim())
      .filter(Boolean),
  };
}

const optionsText = computed({
  get: () => configuration.value.options.join('\n'),
  set: updateOptions,
});
</script>

<template>
  <div class="field-config-form">
    <label v-if="capabilities.required" class="form-field form-field--inline">
      <input v-model="configuration.isRequired" type="checkbox" :disabled="disabled" />
      <span>Required field</span>
    </label>

    <label v-if="capabilities.defaultValue" class="form-field">
      <span>Default value</span>
      <input
        v-model="configuration.defaultValue"
        type="text"
        :disabled="disabled"
        placeholder="Optional default"
      />
    </label>

    <div v-if="capabilities.minLength || capabilities.maxLength" class="form-grid">
      <label v-if="capabilities.minLength" class="form-field">
        <span>Minimum length</span>
        <input
          v-model.number="configuration.minLength"
          type="number"
          min="0"
          :disabled="disabled"
        />
      </label>
      <label v-if="capabilities.maxLength" class="form-field">
        <span>Maximum length</span>
        <input
          v-model.number="configuration.maxLength"
          type="number"
          min="0"
          :disabled="disabled"
        />
      </label>
    </div>

    <div v-if="capabilities.minValue || capabilities.maxValue" class="form-grid">
      <label v-if="capabilities.minValue" class="form-field">
        <span>Minimum value</span>
        <input
          v-model.number="configuration.minValue"
          type="number"
          step="any"
          :disabled="disabled"
        />
      </label>
      <label v-if="capabilities.maxValue" class="form-field">
        <span>Maximum value</span>
        <input
          v-model.number="configuration.maxValue"
          type="number"
          step="any"
          :disabled="disabled"
        />
      </label>
    </div>

    <label v-if="capabilities.pattern" class="form-field">
      <span>Validation pattern</span>
      <input
        v-model="configuration.pattern"
        type="text"
        :disabled="disabled"
        placeholder="Regular expression"
      />
    </label>

    <label v-if="capabilities.allowMultiple" class="form-field form-field--inline">
      <input
        v-model="configuration.allowMultiple"
        type="checkbox"
        :disabled="disabled || definition.requiresAllowMultiple"
      />
      <span>Allow multiple values</span>
    </label>

    <label v-if="capabilities.options" class="form-field">
      <span>Options (one per line)</span>
      <textarea
        v-model="optionsText"
        rows="4"
        :disabled="disabled"
        placeholder="Option A&#10;Option B"
      />
    </label>

    <template v-if="capabilities.relation">
      <label class="form-field">
        <span>Relation target content type</span>
        <select v-model="configuration.relationTarget" :disabled="disabled" required>
          <option :value="null" disabled>Select a content type</option>
          <option v-for="target in relationTargets" :key="target.id" :value="target.id">
            {{ target.displayName }} ({{ target.name }})
          </option>
        </select>
      </label>

      <label class="form-field">
        <span>Relation cardinality</span>
        <select v-model="configuration.relationCardinality" :disabled="disabled" required>
          <option :value="null" disabled>Select cardinality</option>
          <option v-for="option in cardinalityOptions" :key="option.value" :value="option.value">
            {{ option.label }}
          </option>
        </select>
      </label>
    </template>
  </div>
</template>
