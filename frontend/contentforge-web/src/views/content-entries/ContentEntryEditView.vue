<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { onBeforeRouteLeave, useRoute, useRouter } from 'vue-router';
import AppAlert from '@/components/common/AppAlert.vue';
import AppButton from '@/components/common/AppButton.vue';
import AppSpinner from '@/components/common/AppSpinner.vue';
import ConfirmActionPanel from '@/components/common/ConfirmActionPanel.vue';
import ConcurrencyConflictPanel from '@/components/content-entries/ConcurrencyConflictPanel.vue';
import ContentEntryForm from '@/components/content-entries/ContentEntryForm.vue';
import ContentEntryLifecycleActions from '@/components/content-entries/ContentEntryLifecycleActions.vue';
import ContentEntryMetadataBar from '@/components/content-entries/ContentEntryMetadataBar.vue';
import ContentVersionHistoryPanel from '@/components/content-entries/ContentVersionHistoryPanel.vue';
import { getContentEntry, updateContentEntry } from '@/api/content';
import { getContentType } from '@/api/contentTypes';
import { isConcurrencyConflict, type ConcurrencyConflictProblem } from '@/api/concurrency';
import { ApiError } from '@/api/errors';
import { useApiErrorHandling } from '@/composables/useApiErrorHandling';
import { useContentEntryActions } from '@/composables/useContentEntryActions';
import { useContentEntryForm } from '@/composables/useContentEntryForm';
import { useContentPermissions } from '@/composables/useContentPermissions';
import { useNotifications } from '@/composables/useNotifications';
import type { ContentEntry } from '@/types/contentEntries';
import type { ContentType } from '@/types/contentTypes';
import { getLifecycleConfirmationMessage } from '@/utils/contentLifecycle';
import { hasValidationErrors, serializeEntryDataForApi } from '@/utils/contentEntryValidation';
import { openContentPreview } from '@/utils/contentPreview';

const route = useRoute();
const router = useRouter();
const { handleError } = useApiErrorHandling();
const { notifySuccess } = useNotifications();
const { canUpdate, canDelete, canRead } = useContentPermissions();

const entryId = computed(() => route.params.entryId as string);
const contentTypeSlug = computed(() => route.params.contentTypeSlug as string);
const contentType = ref<ContentType | null>(null);
const entry = ref<ContentEntry | null>(null);
const loading = ref(true);
const saving = ref(false);
const pageErrorMessage = ref<string | null>(null);
const concurrencyConflict = ref<ConcurrencyConflictProblem | null>(null);
const showDeleteConfirm = ref(false);
const showLeaveConfirm = ref(false);
const previewLoading = ref(false);
const versionHistoryRef = ref<InstanceType<typeof ContentVersionHistoryPanel> | null>(null);
let pendingLeaveNext: ((value?: false) => void) | null = null;

const {
  slug,
  changeSummary,
  data,
  concurrencyToken,
  sortedFields,
  isDirty,
  validationErrors,
  applyEntry,
  markSaved,
  runClientValidation,
  setServerErrors,
  fieldErrors,
  generalErrors,
} = useContentEntryForm(contentType);

const validationMessage = computed(() => {
  if (hasValidationErrors(validationErrors.value)) {
    return 'Validation errors present';
  }
  if (isDirty.value) {
    return 'Unsaved changes';
  }
  return null;
});

const entryRef = computed(() => entry.value);
const { actionLoading, runLifecycleAction, restoreVersion } = useContentEntryActions(
  entryRef,
  (updated) => {
    entry.value = updated;
    if (contentType.value) {
      markSaved(updated, contentType.value.fields);
    }
    void versionHistoryRef.value?.reload();
  },
  (problem) => {
    concurrencyConflict.value = problem;
  },
  (error, title) => handleError(error, title),
);

async function loadPage(): Promise<void> {
  loading.value = true;
  pageErrorMessage.value = null;
  concurrencyConflict.value = null;

  try {
    const loadedEntry = await getContentEntry(entryId.value);
    entry.value = loadedEntry;
    contentType.value = await getContentType(loadedEntry.contentTypeId);

    if (contentType.value.slug !== contentTypeSlug.value) {
      await router.replace({
        name: 'content-entry-edit',
        params: {
          contentTypeSlug: contentType.value.slug,
          entryId: entryId.value,
        },
      });
    }

    applyEntry(loadedEntry, contentType.value.fields);
  } catch {
    pageErrorMessage.value = 'Unable to load content entry.';
    entry.value = null;
    contentType.value = null;
  } finally {
    loading.value = false;
  }
}

async function saveDraft(): Promise<void> {
  if (!entry.value || !contentType.value || !canUpdate.value) {
    return;
  }

  if (!runClientValidation(true)) {
    return;
  }

  saving.value = true;
  setServerErrors(undefined);
  concurrencyConflict.value = null;

  try {
    const updated = await updateContentEntry(entry.value.id, {
      slug: slug.value.trim(),
      data: serializeEntryDataForApi(contentType.value.fields, data.value),
      changeSummary: changeSummary.value.trim(),
      concurrencyToken: concurrencyToken.value,
    });
    entry.value = updated;
    markSaved(updated, contentType.value.fields);
    void versionHistoryRef.value?.reload();
    notifySuccess('Draft saved', 'Your changes were saved successfully.');
  } catch (error) {
    if (error instanceof ApiError) {
      if (error.isConcurrencyConflict && isConcurrencyConflict(error.problem)) {
        concurrencyConflict.value = error.problem;
        return;
      }
      if (error.validationErrors) {
        setServerErrors(error.validationErrors);
        return;
      }
    }
    handleError(error, 'Failed to save draft');
  } finally {
    saving.value = false;
  }
}

function onEntryUpdated(updated: ContentEntry): void {
  entry.value = updated;
  if (contentType.value) {
    markSaved(updated, contentType.value.fields);
  }
  void versionHistoryRef.value?.reload();
}

async function onEntryDeleted(): Promise<void> {
  await router.push({
    name: 'content-by-type',
    params: { contentTypeSlug: contentType.value?.slug ?? contentTypeSlug.value },
  });
}

async function confirmDelete(): Promise<void> {
  if (!entry.value) {
    return;
  }

  const success = await runLifecycleAction('delete', entry.value.concurrencyToken);
  showDeleteConfirm.value = false;
  if (success) {
    await onEntryDeleted();
  }
}

async function onRestoreVersion(versionNumber: number, summary: string): Promise<void> {
  if (!entry.value) {
    return;
  }

  const success = await restoreVersion(versionNumber, entry.value.concurrencyToken, summary);
  if (success) {
    await loadPage();
  }
}

async function reloadAfterConflict(): Promise<void> {
  concurrencyConflict.value = null;
  await loadPage();
}

async function openPreview(): Promise<void> {
  if (!entry.value || !canRead.value) {
    return;
  }

  previewLoading.value = true;
  try {
    await openContentPreview(entry.value.id);
  } catch (error) {
    handleError(error, 'Failed to open preview');
  } finally {
    previewLoading.value = false;
  }
}

onMounted(() => {
  void loadPage();
});

onBeforeRouteLeave((_to, _from, next) => {
  if (!isDirty.value) {
    next();
    return;
  }

  showLeaveConfirm.value = true;
  pendingLeaveNext = next;
});

function confirmLeave(): void {
  showLeaveConfirm.value = false;
  const next = pendingLeaveNext;
  pendingLeaveNext = null;
  next?.();
}

function cancelLeave(): void {
  showLeaveConfirm.value = false;
  const next = pendingLeaveNext;
  pendingLeaveNext = null;
  next?.(false);
}

function goBackToList(): void {
  void router.push({
    name: 'content-by-type',
    params: { contentTypeSlug: contentType.value?.slug ?? contentTypeSlug.value },
  });
}
</script>

<template>
  <section class="page-card">
    <header class="page-header">
      <div>
        <h2>{{ contentType?.displayName ?? 'Content entry' }}</h2>
        <p class="page-card__lead">
          Edit draft content, manage lifecycle actions, and review versions.
        </p>
      </div>
      <div class="entry-editor__actions">
        <AppButton
          variant="secondary"
          type="button"
          :disabled="saving || actionLoading"
          @click="goBackToList"
        >
          Back to list
        </AppButton>
        <AppButton
          v-if="canRead && entry"
          variant="secondary"
          type="button"
          :loading="previewLoading"
          :disabled="saving || actionLoading"
          @click="openPreview"
        >
          Preview draft
        </AppButton>
        <AppButton
          variant="secondary"
          type="button"
          @click="
            router.push({
              name: 'content-entry-view',
              params: { contentTypeSlug: contentType?.slug ?? contentTypeSlug, entryId },
            })
          "
        >
          View
        </AppButton>
        <ContentEntryLifecycleActions
          v-if="entry"
          :entry="entry"
          :concurrency-token="entry.concurrencyToken"
          :disabled="saving || actionLoading"
          @updated="onEntryUpdated"
          @deleted="onEntryDeleted"
          @conflict="concurrencyConflict = $event"
          @error="(error, title) => handleError(error, title)"
        />
        <AppButton
          v-if="canDelete && entry"
          variant="secondary"
          type="button"
          :disabled="saving || actionLoading"
          @click="showDeleteConfirm = true"
        >
          Delete
        </AppButton>
        <AppButton
          type="button"
          :loading="saving"
          :disabled="!canUpdate || !isDirty"
          @click="saveDraft"
        >
          Save draft
        </AppButton>
      </div>
    </header>

    <ConfirmActionPanel
      v-if="showDeleteConfirm && entry"
      title="Delete entry"
      :message="getLifecycleConfirmationMessage('delete', entry.slug)"
      confirm-label="Delete entry"
      :loading="actionLoading"
      @confirm="confirmDelete"
      @cancel="showDeleteConfirm = false"
    />

    <ConfirmActionPanel
      v-if="showLeaveConfirm"
      title="Unsaved changes"
      message="You have unsaved changes. Leave without saving?"
      confirm-label="Leave without saving"
      @confirm="confirmLeave"
      @cancel="cancelLeave"
    />

    <AppAlert
      v-if="pageErrorMessage"
      kind="error"
      title="Unable to load"
      :message="pageErrorMessage"
    >
      <template #actions>
        <AppButton variant="secondary" type="button" @click="loadPage">Retry</AppButton>
        <AppButton variant="ghost" type="button" @click="goBackToList">Back to list</AppButton>
      </template>
    </AppAlert>

    <ConcurrencyConflictPanel
      v-if="concurrencyConflict"
      :conflict="concurrencyConflict"
      :loading="loading"
      @reload="reloadAfterConflict"
      @dismiss="concurrencyConflict = null"
    />

    <div v-if="loading" class="inline-loading">
      <AppSpinner label="Loading entry" />
      <span>Loading entry…</span>
    </div>

    <template v-else-if="entry && contentType">
      <ContentEntryMetadataBar
        :entry="entry"
        :is-dirty="isDirty"
        :validation-message="validationMessage"
      />

      <form class="stack-form entry-editor" @submit.prevent="saveDraft">
        <AppAlert
          v-if="generalErrors().length"
          kind="error"
          title="Validation failed"
          :message="generalErrors().join(' ')"
        />

        <label class="form-field" :class="{ 'form-field--invalid': validationErrors.Slug?.length }">
          <span>Slug</span>
          <input
            id="content-entry-edit-slug"
            v-model="slug"
            type="text"
            :disabled="saving || !canUpdate"
            :aria-invalid="validationErrors.Slug?.length ? 'true' : undefined"
            :aria-describedby="
              validationErrors.Slug?.length ? 'content-entry-edit-slug-errors' : undefined
            "
          />
          <ul
            v-if="validationErrors.Slug?.length"
            id="content-entry-edit-slug-errors"
            class="entry-field__errors"
            role="alert"
          >
            <li v-for="message in validationErrors.Slug" :key="message">{{ message }}</li>
          </ul>
        </label>

        <label
          class="form-field"
          :class="{ 'form-field--invalid': validationErrors.ChangeSummary?.length }"
        >
          <span>Change summary</span>
          <input
            id="content-entry-change-summary"
            v-model="changeSummary"
            type="text"
            placeholder="Describe what changed in this draft save"
            :disabled="saving || !canUpdate"
            :aria-invalid="validationErrors.ChangeSummary?.length ? 'true' : undefined"
            :aria-describedby="
              validationErrors.ChangeSummary?.length
                ? 'content-entry-change-summary-errors'
                : undefined
            "
          />
          <ul
            v-if="validationErrors.ChangeSummary?.length"
            id="content-entry-change-summary-errors"
            class="entry-field__errors"
            role="alert"
          >
            <li v-for="message in validationErrors.ChangeSummary" :key="message">
              {{ message }}
            </li>
          </ul>
        </label>

        <ContentEntryForm
          v-model:data="data"
          :content-type="contentType"
          :fields="sortedFields"
          :disabled="saving || !canUpdate"
          :field-errors="fieldErrors"
        />
      </form>

      <ContentVersionHistoryPanel
        ref="versionHistoryRef"
        :entry-id="entry.id"
        :current-version="entry.currentVersion"
        :concurrency-token="entry.concurrencyToken"
        :disabled="saving || actionLoading"
        @restore="onRestoreVersion"
        @conflict="concurrencyConflict = $event"
        @error="(error, title) => handleError(error, title)"
      />
    </template>
  </section>
</template>
