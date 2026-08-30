<script setup lang="ts">
import { reactive, watch } from 'vue';

export interface ContentTypeMetadataFormModel {
  name: string;
  displayName: string;
  slug: string;
  description: string;
}

const model = defineModel<ContentTypeMetadataFormModel>({ required: true });

const props = defineProps<{
  mode: 'create' | 'edit';
  disabled?: boolean;
}>();

const local = reactive({ ...model.value });

watch(
  () => model.value,
  (value) => {
    Object.assign(local, value);
  },
  { deep: true },
);

function sync(): void {
  model.value = { ...local };
}

function slugify(value: string): void {
  if (props.mode !== 'create') {
    return;
  }

  local.slug = value
    .trim()
    .toLowerCase()
    .replace(/[^a-z0-9]+/g, '-')
    .replace(/^-+|-+$/g, '');
  sync();
}
</script>

<template>
  <div class="metadata-form">
    <label v-if="mode === 'create'" class="form-field">
      <span>System name</span>
      <input
        v-model="local.name"
        type="text"
        name="name"
        required
        pattern="[A-Za-z][A-Za-z0-9]*"
        :disabled="disabled"
        @input="sync"
      />
      <small>Unique identifier used in APIs. Cannot be changed later.</small>
    </label>

    <label class="form-field">
      <span>Display name</span>
      <input
        v-model="local.displayName"
        type="text"
        name="displayName"
        required
        :disabled="disabled"
        @input="
          slugify(local.displayName);
          sync();
        "
      />
    </label>

    <label class="form-field">
      <span>Slug</span>
      <input
        v-model="local.slug"
        type="text"
        name="slug"
        required
        :disabled="disabled"
        @input="sync"
      />
    </label>

    <label class="form-field">
      <span>Description</span>
      <textarea
        v-model="local.description"
        rows="3"
        name="description"
        :disabled="disabled"
        @input="sync"
      />
    </label>
  </div>
</template>
