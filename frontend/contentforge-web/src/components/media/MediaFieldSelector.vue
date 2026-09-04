<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import AppButton from '@/components/common/AppButton.vue';
import MediaPickerDialog from '@/components/media/MediaPickerDialog.vue';
import { getCachedMediaAssets } from '@/composables/useMediaAssetCache';
import type { MediaAsset } from '@/types/media';
import { formatMediaSize, isImageMedia, resolveMediaUrl } from '@/utils/mediaUrl';

const model = defineModel<string | string[]>({ required: true });

const props = defineProps<{
  multiple?: boolean;
  disabled?: boolean;
}>();

const pickerOpen = ref(false);
const loadedAssets = ref<MediaAsset[]>([]);
const loadingAssets = ref(false);

const selectedIds = computed(() => {
  if (props.multiple) {
    return Array.isArray(model.value) ? model.value : [];
  }

  return typeof model.value === 'string' && model.value ? [model.value] : [];
});

async function loadAssets(ids: string[]): Promise<void> {
  if (ids.length === 0) {
    loadedAssets.value = [];
    return;
  }

  loadingAssets.value = true;
  try {
    loadedAssets.value = await getCachedMediaAssets(ids);
  } catch {
    loadedAssets.value = ids.map((id) => ({
      id,
      fileName: id,
      originalFileName: id,
      contentType: 'application/octet-stream',
      size: 0,
      url: null,
      width: null,
      height: null,
      altText: null,
      title: null,
      description: null,
      uploadedBy: '',
      uploadedAt: '',
      isDeleted: false,
    }));
  } finally {
    loadingAssets.value = false;
  }
}

function onConfirm(ids: string[]): void {
  if (props.multiple) {
    model.value = ids;
  } else {
    model.value = ids[0] ?? '';
  }
}

function clearSelection(): void {
  model.value = props.multiple ? [] : '';
}

watch(
  selectedIds,
  (ids) => {
    void loadAssets(ids);
  },
  { immediate: true },
);
</script>

<template>
  <div class="media-field-selector">
    <div v-if="loadingAssets" class="entry-field__hint">Loading selected media…</div>

    <ul
      v-else-if="loadedAssets.length"
      class="media-field-selector__selected"
      data-testid="media-field-selected"
    >
      <li v-for="asset in loadedAssets" :key="asset.id" class="media-field-selector__item">
        <div class="media-field-selector__preview">
          <img
            v-if="isImageMedia(asset.contentType) && resolveMediaUrl(asset.url)"
            :src="resolveMediaUrl(asset.url)!"
            :alt="asset.altText ?? asset.originalFileName"
          />
          <span v-else>{{ asset.originalFileName }}</span>
        </div>
        <div>
          <strong data-testid="media-field-selected-title">{{
            asset.title ?? asset.originalFileName
          }}</strong>
          <span>{{ formatMediaSize(asset.size) }}</span>
        </div>
      </li>
    </ul>

    <p v-else class="empty-state" data-testid="media-field-empty">No media selected.</p>

    <div class="media-field-selector__actions">
      <AppButton
        variant="secondary"
        type="button"
        :disabled="disabled || pickerOpen"
        :aria-label="multiple ? 'Choose media' : 'Choose media asset'"
        @click="pickerOpen = true"
      >
        {{ multiple ? 'Choose media' : 'Choose media asset' }}
      </AppButton>
      <AppButton
        v-if="selectedIds.length"
        variant="ghost"
        type="button"
        :disabled="disabled || pickerOpen"
        aria-label="Clear selected media"
        @click="clearSelection"
      >
        Clear
      </AppButton>
    </div>

    <MediaPickerDialog
      v-model:open="pickerOpen"
      :multiple="multiple"
      :selected-ids="selectedIds"
      @confirm="onConfirm"
    />
  </div>
</template>
