<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue';
import AppAlert from '@/components/common/AppAlert.vue';
import AppButton from '@/components/common/AppButton.vue';
import AppSpinner from '@/components/common/AppSpinner.vue';
import ConfirmActionPanel from '@/components/common/ConfirmActionPanel.vue';
import PaginationBar from '@/components/common/PaginationBar.vue';
import MediaAssetCard from '@/components/media/MediaAssetCard.vue';
import MediaAssetRow from '@/components/media/MediaAssetRow.vue';
import MediaMetadataForm from '@/components/media/MediaMetadataForm.vue';
import { deleteMedia, listMedia, updateMediaMetadata } from '@/api/media';
import { primeMediaAssetCache } from '@/composables/useMediaAssetCache';
import { useApiErrorHandling } from '@/composables/useApiErrorHandling';
import { useMediaPermissions } from '@/composables/useMediaPermissions';
import { MEDIA_CONTENT_TYPE_FILTERS, type MediaAsset } from '@/types/media';
import { formatMediaSize, isImageMedia, resolveMediaUrl } from '@/utils/mediaUrl';

const props = withDefaults(
  defineProps<{
    selectionMode?: boolean;
    multiple?: boolean;
    selectedIds?: string[];
  }>(),
  {
    selectionMode: false,
    multiple: false,
    selectedIds: () => [],
  },
);

const emit = defineEmits<{
  select: [asset: MediaAsset];
  confirmSelection: [ids: string[]];
  cancel: [];
}>();

const { handleError } = useApiErrorHandling();
const { canUpdate, canDelete } = useMediaPermissions();

const items = ref<MediaAsset[]>([]);
const loading = ref(true);
const errorMessage = ref<string | null>(null);
const search = ref('');
const contentTypeFilter = ref('');
const viewMode = ref<'grid' | 'list'>('grid');
const page = ref(1);
const pageSize = ref(24);
const totalItems = ref(0);
const totalPages = ref(1);
const selectedAssetId = ref<string | null>(null);
const selectedAsset = ref<MediaAsset | null>(null);
const savingMetadata = ref(false);
const deleting = ref(false);
const showDeleteConfirm = ref(false);
const metadata = ref({ altText: '', title: '', description: '' });
const pendingSelection = ref<string[]>([...props.selectedIds]);

const selectedSet = computed(() => new Set(pendingSelection.value));

const deleteConfirmMessage = computed(() => {
  if (!selectedAsset.value) {
    return '';
  }

  return `Delete "${selectedAsset.value.originalFileName}"? This removes the asset from the library.`;
});

async function loadMedia(): Promise<void> {
  loading.value = true;
  errorMessage.value = null;

  try {
    const response = await listMedia({
      page: page.value,
      pageSize: pageSize.value,
      sortBy: 'uploadedAt',
      sortDirection: 'desc',
      search: search.value.trim() || undefined,
      contentType: contentTypeFilter.value || undefined,
    });
    items.value = response.items;
    totalItems.value = response.totalItems;
    totalPages.value = Math.max(response.totalPages, 1);
    page.value = response.page;
  } catch (error) {
    errorMessage.value = 'Unable to load media library.';
    handleError(error, 'Failed to load media');
  } finally {
    loading.value = false;
  }
}

function applyFilters(): void {
  page.value = 1;
  void loadMedia();
}

function onAssetSelect(asset: MediaAsset): void {
  if (props.selectionMode) {
    toggleSelection(asset.id);
    emit('select', asset);
    return;
  }

  selectedAssetId.value = asset.id;
  primeMediaAssetCache(asset);
  selectedAsset.value = asset;
  metadata.value = {
    altText: asset.altText ?? '',
    title: asset.title ?? '',
    description: asset.description ?? '',
  };
}

function toggleSelection(id: string): void {
  if (props.multiple) {
    pendingSelection.value = selectedSet.value.has(id)
      ? pendingSelection.value.filter((item) => item !== id)
      : [...pendingSelection.value, id];
    return;
  }

  pendingSelection.value = [id];
}

async function saveMetadata(): Promise<void> {
  if (!selectedAsset.value || !canUpdate.value) {
    return;
  }

  savingMetadata.value = true;
  try {
    selectedAsset.value = await updateMediaMetadata(selectedAsset.value.id, {
      altText: metadata.value.altText || null,
      title: metadata.value.title || null,
      description: metadata.value.description || null,
    });
    await loadMedia();
  } catch (error) {
    handleError(error, 'Failed to update metadata');
  } finally {
    savingMetadata.value = false;
  }
}

async function confirmDelete(): Promise<void> {
  if (!selectedAsset.value || !canDelete.value) {
    return;
  }

  deleting.value = true;
  try {
    await deleteMedia(selectedAsset.value.id);
    selectedAssetId.value = null;
    selectedAsset.value = null;
    showDeleteConfirm.value = false;
    await loadMedia();
  } catch (error) {
    handleError(error, 'Failed to delete media');
  } finally {
    deleting.value = false;
  }
}

function confirmPickerSelection(): void {
  emit('confirmSelection', pendingSelection.value);
}

watch([page, pageSize], () => {
  void loadMedia();
});

watch(
  () => props.selectedIds,
  (value) => {
    pendingSelection.value = [...value];
  },
);

onMounted(() => {
  void loadMedia();
});

defineExpose({
  reload: loadMedia,
});
</script>

<template>
  <div class="media-library" :class="{ 'media-library--picker': selectionMode }">
    <form class="content-list-toolbar media-library__toolbar" @submit.prevent="applyFilters">
      <label class="form-field">
        <span>Search</span>
        <input v-model="search" type="search" placeholder="Search by file name or title" />
      </label>
      <label class="form-field">
        <span>Type</span>
        <select v-model="contentTypeFilter">
          <option
            v-for="option in MEDIA_CONTENT_TYPE_FILTERS"
            :key="option.value"
            :value="option.value"
          >
            {{ option.label }}
          </option>
        </select>
      </label>
      <div class="content-list-toolbar__actions">
        <AppButton
          variant="secondary"
          type="button"
          @click="viewMode = viewMode === 'grid' ? 'list' : 'grid'"
        >
          {{ viewMode === 'grid' ? 'List view' : 'Grid view' }}
        </AppButton>
        <AppButton type="submit" variant="secondary">Apply</AppButton>
      </div>
    </form>

    <AppAlert v-if="errorMessage" kind="error" title="Load failed" :message="errorMessage" />

    <div v-if="loading" class="inline-loading">
      <AppSpinner label="Loading media library" />
      <span>Loading media…</span>
    </div>

    <div v-else-if="items.length === 0" class="empty-state">
      No media assets match the current filters.
    </div>

    <template v-else>
      <div v-if="viewMode === 'grid'" class="media-library__grid">
        <MediaAssetCard
          v-for="asset in items"
          :key="asset.id"
          :asset="asset"
          :selected="selectionMode ? selectedSet.has(asset.id) : selectedAssetId === asset.id"
          :selectable="true"
          @select="onAssetSelect(asset)"
        />
      </div>

      <div v-else class="media-library__list">
        <MediaAssetRow
          v-for="asset in items"
          :key="asset.id"
          :asset="asset"
          :selected="selectionMode ? selectedSet.has(asset.id) : selectedAssetId === asset.id"
          @select="onAssetSelect(asset)"
        />
      </div>

      <PaginationBar
        v-model:page="page"
        v-model:page-size="pageSize"
        :total-items="totalItems"
        :total-pages="totalPages"
        :disabled="loading"
      />
    </template>

    <section v-if="!selectionMode && selectedAssetId" class="media-library__detail editor-panel">
      <template v-if="selectedAsset">
        <header class="section-header">
          <div>
            <h3>{{ selectedAsset.originalFileName }}</h3>
            <p>{{ selectedAsset.contentType }} · {{ formatMediaSize(selectedAsset.size) }}</p>
          </div>
          <AppButton
            v-if="canDelete"
            variant="secondary"
            type="button"
            :disabled="deleting"
            @click="showDeleteConfirm = true"
          >
            Delete
          </AppButton>
        </header>

        <div
          v-if="isImageMedia(selectedAsset.contentType) && resolveMediaUrl(selectedAsset.url)"
          class="media-library__preview"
        >
          <img
            :src="resolveMediaUrl(selectedAsset.url)!"
            :alt="selectedAsset.altText ?? selectedAsset.originalFileName"
          />
        </div>

        <MediaMetadataForm
          v-model:metadata="metadata"
          :disabled="!canUpdate || savingMetadata"
          @submit="saveMetadata"
        />
      </template>
    </section>

    <ConfirmActionPanel
      v-if="showDeleteConfirm && selectedAsset"
      title="Delete media"
      :message="deleteConfirmMessage"
      confirm-label="Delete media"
      :loading="deleting"
      @confirm="confirmDelete"
      @cancel="showDeleteConfirm = false"
    />

    <div v-if="selectionMode" class="media-picker__actions">
      <AppButton variant="secondary" type="button" @click="emit('cancel')">Cancel</AppButton>
      <AppButton
        type="button"
        :disabled="pendingSelection.length === 0"
        aria-label="Use selected"
        @click="confirmPickerSelection"
      >
        Use selected
      </AppButton>
    </div>
  </div>
</template>
