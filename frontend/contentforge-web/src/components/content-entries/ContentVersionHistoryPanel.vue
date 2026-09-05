<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue';
import AppAlert from '@/components/common/AppAlert.vue';
import AppButton from '@/components/common/AppButton.vue';
import AppSpinner from '@/components/common/AppSpinner.vue';
import ConfirmActionPanel from '@/components/common/ConfirmActionPanel.vue';
import { compareContentVersions, getContentVersion, listContentVersions } from '@/api/content';
import { useContentPermissions } from '@/composables/useContentPermissions';
import { CONTENT_STATUS_LABELS, type ContentVersion } from '@/types/contentEntries';
import type { ConcurrencyConflictProblem } from '@/api/concurrency';

const props = defineProps<{
  entryId: string;
  currentVersion: number;
  concurrencyToken: number;
  disabled?: boolean;
}>();

const emit = defineEmits<{
  restored: [];
  conflict: [problem: ConcurrencyConflictProblem];
  error: [error: unknown, title: string];
  restore: [versionNumber: number, changeSummary: string];
}>();

const { canReadVersions, canRestoreVersions } = useContentPermissions();

const versions = ref<ContentVersion[]>([]);
const loading = ref(false);
const errorMessage = ref<string | null>(null);
const selectedVersionNumber = ref<number | null>(null);
const selectedVersion = ref<ContentVersion | null>(null);
const comparisonSummary = ref<string | null>(null);
const restoreTarget = ref<number | null>(null);
const restoreChangeSummary = ref('');
const detailLoading = ref(false);

const sortedVersions = computed(() =>
  [...versions.value].sort((left, right) => right.versionNumber - left.versionNumber),
);

async function loadVersions(): Promise<void> {
  if (!canReadVersions.value) {
    return;
  }

  loading.value = true;
  errorMessage.value = null;

  try {
    versions.value = await listContentVersions(props.entryId);
  } catch {
    errorMessage.value = 'Unable to load version history.';
  } finally {
    loading.value = false;
  }
}

async function inspectVersion(versionNumber: number): Promise<void> {
  selectedVersionNumber.value = versionNumber;
  detailLoading.value = true;
  comparisonSummary.value = null;

  try {
    selectedVersion.value = await getContentVersion(props.entryId, versionNumber);
    if (versionNumber !== props.currentVersion) {
      const comparison = await compareContentVersions(
        props.entryId,
        versionNumber,
        props.currentVersion,
      );
      comparisonSummary.value = `${comparison.changes.length} difference(s) compared to current version ${props.currentVersion}.`;
    }
  } catch (error) {
    selectedVersion.value = null;
    emit('error', error, 'Failed to load version details');
  } finally {
    detailLoading.value = false;
  }
}

function requestRestore(versionNumber: number): void {
  restoreTarget.value = versionNumber;
  restoreChangeSummary.value = '';
}

function cancelRestore(): void {
  restoreTarget.value = null;
  restoreChangeSummary.value = '';
}

function confirmRestore(): void {
  if (!restoreTarget.value || !restoreChangeSummary.value.trim()) {
    return;
  }

  emit('restore', restoreTarget.value, restoreChangeSummary.value.trim());
  cancelRestore();
}

watch(
  () => props.entryId,
  () => {
    void loadVersions();
  },
);

onMounted(() => {
  void loadVersions();
});

defineExpose({
  reload: loadVersions,
});
</script>

<template>
  <section class="version-history editor-panel">
    <header class="section-header">
      <div>
        <h3>Version history</h3>
        <p>Review previous snapshots and restore an older version when permitted.</p>
      </div>
    </header>

    <AppAlert
      v-if="!canReadVersions"
      kind="warning"
      title="Versions unavailable"
      message="You do not have permission to view version history."
    />

    <div v-else-if="loading" class="inline-loading">
      <AppSpinner label="Loading versions" />
      <span>Loading versions…</span>
    </div>

    <AppAlert v-else-if="errorMessage" kind="error" title="Unable to load" :message="errorMessage">
      <template #actions>
        <AppButton variant="secondary" type="button" @click="loadVersions">Retry</AppButton>
      </template>
    </AppAlert>

    <div v-else-if="sortedVersions.length === 0" class="empty-state">No versions recorded yet.</div>

    <div v-else class="version-history__layout">
      <ul class="version-history__list">
        <li
          v-for="version in sortedVersions"
          :key="version.id"
          class="version-history__item"
          :data-testid="`version-item-${version.versionNumber}`"
        >
          <button
            type="button"
            class="version-history__summary"
            :class="{
              'version-history__summary--selected': selectedVersionNumber === version.versionNumber,
            }"
            :data-testid="`version-summary-${version.versionNumber}`"
            :aria-current="selectedVersionNumber === version.versionNumber ? 'true' : undefined"
            @click="inspectVersion(version.versionNumber)"
          >
            <strong>Version {{ version.versionNumber }}</strong>
            <span>{{ CONTENT_STATUS_LABELS[version.status] }} · {{ version.slug }}</span>
            <span>{{ new Date(version.createdAt).toLocaleString() }}</span>
            <span>{{ version.changeSummary }}</span>
          </button>
          <AppButton
            v-if="canRestoreVersions && version.versionNumber !== currentVersion"
            variant="ghost"
            type="button"
            :disabled="disabled"
            @click="requestRestore(version.versionNumber)"
          >
            Restore
          </AppButton>
        </li>
      </ul>

      <div
        v-if="selectedVersionNumber !== null"
        class="version-history__detail"
        data-testid="version-detail"
      >
        <div v-if="detailLoading" class="inline-loading">
          <AppSpinner label="Loading version detail" />
          <span>Loading version detail…</span>
        </div>
        <template v-else-if="selectedVersion">
          <h4>Version {{ selectedVersion.versionNumber }}</h4>
          <p v-if="comparisonSummary" data-testid="version-comparison">{{ comparisonSummary }}</p>
          <pre class="version-history__json" data-testid="version-json">{{
            JSON.stringify(selectedVersion.data, null, 2)
          }}</pre>
        </template>
      </div>
    </div>

    <ConfirmActionPanel
      v-if="restoreTarget !== null"
      v-model:change-summary="restoreChangeSummary"
      title="Restore version"
      :message="`Restore version ${restoreTarget}? This creates a new draft version based on the selected snapshot.`"
      confirm-label="Restore version"
      require-change-summary
      @confirm="confirmRestore"
      @cancel="cancelRestore"
    />
  </section>
</template>
