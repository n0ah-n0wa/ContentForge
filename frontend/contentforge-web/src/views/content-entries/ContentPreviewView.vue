<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useRoute } from 'vue-router';
import AppAlert from '@/components/common/AppAlert.vue';
import AppSpinner from '@/components/common/AppSpinner.vue';
import ContentEntryReadOnlyDisplay from '@/components/content-entries/ContentEntryReadOnlyDisplay.vue';
import { getContentPreview } from '@/api/content';
import { ApiError } from '@/api/errors';
import { useApiErrorHandling } from '@/composables/useApiErrorHandling';
import { CONTENT_STATUS_LABELS, type ContentPreview } from '@/types/contentEntries';
import type { ContentTypeField } from '@/types/contentTypes';
import { FieldType } from '@/types/contentTypes';

const route = useRoute();
const { handleError } = useApiErrorHandling();

const preview = ref<ContentPreview | null>(null);
const loading = ref(true);
const errorMessage = ref<string | null>(null);

const previewToken = computed(() => route.params.token as string);

const displayFields = computed<ContentTypeField[]>(() => {
  if (!preview.value) {
    return [];
  }

  return preview.value.fields
    .map((field) => ({
      id: field.name,
      name: field.name,
      displayName: field.displayName,
      fieldType: field.fieldType as FieldType,
      sortOrder: field.sortOrder,
      configuration: {
        isRequired: false,
        minLength: null,
        maxLength: null,
        minValue: null,
        maxValue: null,
        pattern: null,
        allowMultiple: false,
        defaultValue: null,
        options: [],
        relationTarget: null,
        relationCardinality: null,
      },
    }))
    .sort((left, right) => left.sortOrder - right.sortOrder);
});

const expiresLabel = computed(() => {
  if (!preview.value) {
    return null;
  }

  return new Date(preview.value.expiresAt).toLocaleString();
});

async function loadPreview(): Promise<void> {
  loading.value = true;
  errorMessage.value = null;

  try {
    preview.value = await getContentPreview(previewToken.value);
  } catch (error) {
    if (error instanceof ApiError && error.status === 401) {
      errorMessage.value = 'This preview link is invalid or has expired.';
    } else {
      errorMessage.value = 'Unable to load preview.';
      handleError(error, 'Failed to load preview');
    }
  } finally {
    loading.value = false;
  }
}

onMounted(() => {
  void loadPreview();
});
</script>

<template>
  <main class="content-preview-page">
    <div class="content-preview-page__inner">
      <header class="content-preview-page__header">
        <p class="content-preview-page__eyebrow">Secure draft preview</p>
        <h1 v-if="preview">{{ preview.slug }}</h1>
        <p v-if="preview" class="content-preview-page__meta">
          {{ preview.contentTypeSlug }} · {{ CONTENT_STATUS_LABELS[preview.status] }}
          <span v-if="expiresLabel"> · Expires {{ expiresLabel }}</span>
        </p>
      </header>

      <AppSpinner v-if="loading" />

      <AppAlert
        v-else-if="errorMessage"
        kind="error"
        title="Preview unavailable"
        :message="errorMessage"
      />

      <section v-else-if="preview" class="content-preview-page__body">
        <ContentEntryReadOnlyDisplay :fields="displayFields" :data="preview.data" />
      </section>
    </div>
  </main>
</template>

<style scoped>
.content-preview-page {
  min-height: 100vh;
  background: var(--surface-muted, #f5f6f8);
  padding: 2rem 1rem 3rem;
}

.content-preview-page__inner {
  max-width: 960px;
  margin: 0 auto;
}

.content-preview-page__header {
  margin-bottom: 1.5rem;
}

.content-preview-page__eyebrow {
  margin: 0 0 0.5rem;
  font-size: 0.875rem;
  font-weight: 600;
  letter-spacing: 0.04em;
  text-transform: uppercase;
  color: var(--text-muted, #5f6368);
}

.content-preview-page__header h1 {
  margin: 0 0 0.5rem;
}

.content-preview-page__meta {
  margin: 0;
  color: var(--text-muted, #5f6368);
}

.content-preview-page__body {
  background: #fff;
  border: 1px solid var(--border-subtle, #d9dee7);
  border-radius: 12px;
  padding: 1.5rem;
}
</style>
