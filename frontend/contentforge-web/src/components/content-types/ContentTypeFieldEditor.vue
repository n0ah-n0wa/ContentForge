<script setup lang="ts">
import { computed, reactive, watch } from 'vue';
import AppAlert from '@/components/common/AppAlert.vue';
import AppButton from '@/components/common/AppButton.vue';
import FieldConfigurationForm from '@/components/content-types/FieldConfigurationForm.vue';
import { FIELD_TYPE_DEFINITIONS, getFieldTypeDefinition } from '@/config/fieldTypeDefinitions';
import {
  createDefaultFieldConfiguration,
  FieldType,
  type ContentType,
  type ContentTypeField,
  type FieldConfiguration,
} from '@/types/contentTypes';

const props = defineProps<{
  mode: 'add' | 'edit';
  contentType: ContentType;
  relationTargets: ContentType[];
  field?: ContentTypeField;
  nextSortOrder: number;
  disabled?: boolean;
  loading?: boolean;
  errorMessage?: string | null;
}>();

const emit = defineEmits<{
  submit: [
    payload: {
      name: string;
      fieldType: FieldType;
      displayName: string;
      sortOrder: number;
      configuration: FieldConfiguration;
    },
  ];
  cancel: [];
}>();

const form = reactive({
  name: props.field?.name ?? '',
  fieldType: props.field?.fieldType ?? FieldType.Text,
  displayName: props.field?.displayName ?? '',
  sortOrder: props.field?.sortOrder ?? props.nextSortOrder,
  configuration: structuredClone(
    props.field?.configuration ?? createDefaultFieldConfiguration(FieldType.Text),
  ) as FieldConfiguration,
});

watch(
  () => form.fieldType,
  (fieldType) => {
    if (props.mode === 'add') {
      form.configuration = createDefaultFieldConfiguration(fieldType);
    }
  },
);

const selectedDefinition = computed(() => getFieldTypeDefinition(form.fieldType));

function onSubmit(): void {
  emit('submit', {
    name: form.name.trim(),
    fieldType: form.fieldType,
    displayName: form.displayName.trim(),
    sortOrder: form.sortOrder,
    configuration: structuredClone(form.configuration),
  });
}

const fieldTypeOptions = FIELD_TYPE_DEFINITIONS;
</script>

<template>
  <form class="field-editor" @submit.prevent="onSubmit">
    <header class="section-header">
      <div>
        <h3>{{ mode === 'add' ? 'Add field' : `Edit field: ${field?.name}` }}</h3>
        <p>{{ selectedDefinition.description }}</p>
      </div>
    </header>

    <AppAlert
      v-if="errorMessage"
      kind="error"
      title="Unable to save field"
      :message="errorMessage"
    />

    <label v-if="mode === 'add'" class="form-field">
      <span>Field name</span>
      <input
        v-model="form.name"
        type="text"
        required
        pattern="[A-Za-z][A-Za-z0-9]*"
        :disabled="disabled"
      />
    </label>

    <label v-if="mode === 'add'" class="form-field">
      <span>Field type</span>
      <select v-model="form.fieldType" :disabled="disabled" required>
        <option v-for="option in fieldTypeOptions" :key="option.type" :value="option.type">
          {{ option.label }}
        </option>
      </select>
    </label>

    <p v-else class="field-editor__type">
      Type: <strong>{{ selectedDefinition.label }}</strong>
    </p>

    <label class="form-field">
      <span>Display name</span>
      <input v-model="form.displayName" type="text" required :disabled="disabled" />
    </label>

    <label class="form-field">
      <span>Sort order</span>
      <input v-model.number="form.sortOrder" type="number" min="0" required :disabled="disabled" />
    </label>

    <section class="field-editor__configuration">
      <h4>Validation &amp; behavior</h4>
      <FieldConfigurationForm
        v-model:configuration="form.configuration"
        :field-type="form.fieldType"
        :relation-targets="relationTargets.filter((target) => target.id !== contentType.id)"
        :disabled="disabled"
      />
    </section>

    <div class="form-actions">
      <AppButton variant="secondary" type="button" :disabled="loading" @click="emit('cancel')">
        Cancel
      </AppButton>
      <AppButton type="submit" :loading="loading" :disabled="disabled">
        {{ mode === 'add' ? 'Add field' : 'Save field' }}
      </AppButton>
    </div>
  </form>
</template>
