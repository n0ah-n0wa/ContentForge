<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import AppAlert from '@/components/common/AppAlert.vue';
import AppButton from '@/components/common/AppButton.vue';
import AppSpinner from '@/components/common/AppSpinner.vue';
import ContentEntryForm from '@/components/content-entries/ContentEntryForm.vue';
import { createContentEntry } from '@/api/content';
import { getContentType, listContentTypes } from '@/api/contentTypes';
import { ApiError } from '@/api/errors';
import { useApiErrorHandling } from '@/composables/useApiErrorHandling';
import { useContentEntryForm } from '@/composables/useContentEntryForm';
import { useContentPermissions } from '@/composables/useContentPermissions';
import { useNotifications } from '@/composables/useNotifications';
import type { ContentType } from '@/types/contentTypes';
import { serializeEntryDataForApi } from '@/utils/contentEntryValidation';

const route = useRoute();
const router = useRouter();
const { handleError } = useApiErrorHandling();
const { notifySuccess } = useNotifications();
const { canCreate } = useContentPermissions();

const contentTypeSlug = computed(() => route.params.contentTypeSlug as string);
const contentType = ref<ContentType | null>(null);
const loading = ref(true);
const saving = ref(false);
const pageErrorMessage = ref<string | null>(null);

const {
  slug,
  data,
  sortedFields,
  validationErrors,
  initializeForCreate,
  runClientValidation,
  setServerErrors,
  fieldErrors,
  generalErrors,
} = useContentEntryForm(contentType);

async function loadContentType(): Promise<void> {
  loading.value = true;
  pageErrorMessage.value = null;

  try {
    const response = await listContentTypes({
      page: 1,
      pageSize: 100,
      sortBy: 'name',
      sortDirection: 'asc',
      search: contentTypeSlug.value,
    });
    contentType.value =
      response.items.find((item) => item.slug === contentTypeSlug.value) ??
      response.items.find((item) => item.name === contentTypeSlug.value) ??
      null;

    if (!contentType.value) {
      pageErrorMessage.value = 'Content type not found.';
      return;
    }

    // List payloads omit field definitions (fieldCount only) — load the full type for the form.
    contentType.value = await getContentType(contentType.value.id);

    initializeForCreate(contentType.value);
  } catch {
    pageErrorMessage.value = 'Unable to load content type.';
  } finally {
    loading.value = false;
  }
}

async function createEntry(): Promise<void> {
  if (!contentType.value || !canCreate.value) {
    return;
  }

  if (!runClientValidation(false)) {
    return;
  }

  saving.value = true;
  setServerErrors(undefined);

  try {
    const created = await createContentEntry({
      contentTypeId: contentType.value.id,
      slug: slug.value.trim(),
      data: serializeEntryDataForApi(contentType.value.fields, data.value),
    });

    notifySuccess('Draft created', `"${created.slug}" is ready to edit.`);
    await router.push({
      name: 'content-entry-edit',
      params: {
        contentTypeSlug: contentType.value.slug,
        entryId: created.id,
      },
    });
  } catch (error) {
    if (error instanceof ApiError && error.validationErrors) {
      setServerErrors(error.validationErrors);
    } else {
      handleError(error, 'Failed to create entry');
    }
  } finally {
    saving.value = false;
  }
}

onMounted(() => {
  void loadContentType();
});
</script>

<template>
  <section class="page-card">
    <header class="page-header">
      <div>
        <h2>Create entry</h2>
        <p class="page-card__lead">
          {{ contentType?.displayName ?? 'New content entry' }}
        </p>
      </div>
    </header>

    <AppAlert
      v-if="pageErrorMessage"
      kind="error"
      title="Unable to load"
      :message="pageErrorMessage"
    >
      <template #actions>
        <AppButton variant="secondary" type="button" @click="loadContentType">Retry</AppButton>
        <AppButton
          variant="ghost"
          type="button"
          @click="router.push({ name: 'content-by-type', params: { contentTypeSlug } })"
        >
          Back to list
        </AppButton>
      </template>
    </AppAlert>

    <div v-if="loading" class="inline-loading">
      <AppSpinner label="Preparing editor" />
      <span>Preparing editor…</span>
    </div>

    <form v-else-if="contentType" class="stack-form entry-editor" @submit.prevent="createEntry">
      <AppAlert
        v-if="generalErrors().length"
        kind="error"
        title="Validation failed"
        :message="generalErrors().join(' ')"
      />

      <label class="form-field" :class="{ 'form-field--invalid': validationErrors.Slug?.length }">
        <span>Slug</span>
        <input
          id="content-entry-slug"
          v-model="slug"
          type="text"
          required
          :disabled="saving"
          :aria-invalid="validationErrors.Slug?.length ? 'true' : undefined"
          :aria-describedby="
            validationErrors.Slug?.length ? 'content-entry-slug-errors' : undefined
          "
        />
        <ul
          v-if="validationErrors.Slug?.length"
          id="content-entry-slug-errors"
          class="entry-field__errors"
          role="alert"
        >
          <li v-for="message in validationErrors.Slug" :key="message">{{ message }}</li>
        </ul>
      </label>

      <ContentEntryForm
        v-model:data="data"
        :content-type="contentType"
        :fields="sortedFields"
        :disabled="saving"
        :field-errors="fieldErrors"
      />

      <div class="form-actions">
        <AppButton
          variant="secondary"
          type="button"
          @click="router.push({ name: 'content-by-type', params: { contentTypeSlug } })"
        >
          Cancel
        </AppButton>
        <AppButton type="submit" :loading="saving" :disabled="!canCreate"> Create draft </AppButton>
      </div>
    </form>
  </section>
</template>
