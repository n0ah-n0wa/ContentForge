<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import AppAlert from '@/components/common/AppAlert.vue';
import AppButton from '@/components/common/AppButton.vue';
import AppSpinner from '@/components/common/AppSpinner.vue';
import ConfirmActionPanel from '@/components/common/ConfirmActionPanel.vue';
import ContentEntryLifecycleActions from '@/components/content-entries/ContentEntryLifecycleActions.vue';
import ContentEntryMetadataBar from '@/components/content-entries/ContentEntryMetadataBar.vue';
import ContentEntryReadOnlyDisplay from '@/components/content-entries/ContentEntryReadOnlyDisplay.vue';
import ContentVersionHistoryPanel from '@/components/content-entries/ContentVersionHistoryPanel.vue';
import ConcurrencyConflictPanel from '@/components/content-entries/ConcurrencyConflictPanel.vue';
import { getContentEntry } from '@/api/content';
import { getContentType } from '@/api/contentTypes';
import type { ConcurrencyConflictProblem } from '@/api/concurrency';
import { useApiErrorHandling } from '@/composables/useApiErrorHandling';
import { useContentEntryActions } from '@/composables/useContentEntryActions';
import { useContentPermissions } from '@/composables/useContentPermissions';
import { normalizeEntryData } from '@/utils/contentEntryValidation';
import { getLifecycleConfirmationMessage } from '@/utils/contentLifecycle';
import type { ContentEntry } from '@/types/contentEntries';
import type { ContentType } from '@/types/contentTypes';

const route = useRoute();
const router = useRouter();
const { handleError } = useApiErrorHandling();
const { canUpdate, canDelete } = useContentPermissions();

const entryId = computed(() => route.params.entryId as string);
const contentTypeSlug = computed(() => route.params.contentTypeSlug as string);
const contentType = ref<ContentType | null>(null);
const entry = ref<ContentEntry | null>(null);
const loading = ref(true);
const pageErrorMessage = ref<string | null>(null);
const concurrencyConflict = ref<ConcurrencyConflictProblem | null>(null);
const showDeleteConfirm = ref(false);
const versionHistoryRef = ref<InstanceType<typeof ContentVersionHistoryPanel> | null>(null);

const sortedFields = computed(() => {
  if (!contentType.value) {
    return [];
  }

  return [...contentType.value.fields].sort((left, right) => left.sortOrder - right.sortOrder);
});

const displayData = computed(() => {
  if (!entry.value || !contentType.value) {
    return {};
  }

  return normalizeEntryData(contentType.value.fields, entry.value.draftData);
});

const entryRef = computed(() => entry.value);
const { actionLoading, runLifecycleAction, restoreVersion } = useContentEntryActions(
  entryRef,
  (updated) => {
    entry.value = updated;
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
        name: 'content-entry-view',
        params: {
          contentTypeSlug: contentType.value.slug,
          entryId: entryId.value,
        },
      });
    }
  } catch (error) {
    pageErrorMessage.value = 'Unable to load content entry.';
    handleError(error, 'Failed to load content entry');
  } finally {
    loading.value = false;
  }
}

function onEntryUpdated(updated: ContentEntry): void {
  entry.value = updated;
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

async function onRestoreVersion(versionNumber: number, changeSummary: string): Promise<void> {
  if (!entry.value) {
    return;
  }

  const success = await restoreVersion(versionNumber, entry.value.concurrencyToken, changeSummary);
  if (success) {
    await loadPage();
  }
}

async function reloadAfterConflict(): Promise<void> {
  concurrencyConflict.value = null;
  await loadPage();
}

onMounted(() => {
  void loadPage();
});
</script>

<template>
  <section class="page-card">
    <header class="page-header">
      <div>
        <h2>{{ contentType?.displayName ?? 'Content entry' }}</h2>
        <p class="page-card__lead">Read-only view of the current draft snapshot.</p>
      </div>
      <div class="entry-editor__actions">
        <ContentEntryLifecycleActions
          v-if="entry"
          :entry="entry"
          :concurrency-token="entry.concurrencyToken"
          :disabled="actionLoading"
          @updated="onEntryUpdated"
          @deleted="onEntryDeleted"
          @conflict="concurrencyConflict = $event"
          @error="(error, title) => handleError(error, title)"
        />
        <AppButton
          v-if="canDelete && entry"
          variant="secondary"
          type="button"
          :disabled="actionLoading"
          @click="showDeleteConfirm = true"
        >
          Delete
        </AppButton>
        <AppButton
          v-if="canUpdate && contentType"
          @click="
            router.push({
              name: 'content-entry-edit',
              params: { contentTypeSlug: contentType.slug, entryId },
            })
          "
        >
          Edit entry
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

    <AppAlert
      v-if="pageErrorMessage"
      kind="error"
      title="Unable to continue"
      :message="pageErrorMessage"
    />

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
      <ContentEntryMetadataBar :entry="entry" :is-dirty="false" />

      <section class="entry-editor stack-form">
        <div class="form-field">
          <span>Slug</span>
          <strong>{{ entry.slug }}</strong>
        </div>

        <ContentEntryReadOnlyDisplay :fields="sortedFields" :data="displayData" />
      </section>

      <ContentVersionHistoryPanel
        ref="versionHistoryRef"
        :entry-id="entry.id"
        :current-version="entry.currentVersion"
        :concurrency-token="entry.concurrencyToken"
        :disabled="actionLoading"
        @restore="onRestoreVersion"
        @conflict="concurrencyConflict = $event"
        @error="(error, title) => handleError(error, title)"
      />
    </template>
  </section>
</template>
