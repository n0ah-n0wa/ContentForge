<script setup lang="ts">
import { ref } from 'vue';
import AppButton from '@/components/common/AppButton.vue';
import MediaLibraryBrowser from '@/components/media/MediaLibraryBrowser.vue';
import type { MediaAsset } from '@/types/media';

const open = defineModel<boolean>('open', { default: false });

const props = withDefaults(
  defineProps<{
    multiple?: boolean;
    selectedIds?: string[];
  }>(),
  {
    multiple: false,
    selectedIds: () => [],
  },
);

const emit = defineEmits<{
  confirm: [ids: string[], assets: MediaAsset[]];
}>();

const selectedAssets = ref<MediaAsset[]>([]);

function onSelect(asset: MediaAsset): void {
  if (props.multiple) {
    const exists = selectedAssets.value.some((item) => item.id === asset.id);
    selectedAssets.value = exists
      ? selectedAssets.value.filter((item) => item.id !== asset.id)
      : [...selectedAssets.value, asset];
    return;
  }

  selectedAssets.value = [asset];
}

function onConfirm(ids: string[]): void {
  emit('confirm', ids, selectedAssets.value);
  open.value = false;
}

function close(): void {
  open.value = false;
}
</script>

<template>
  <div v-if="open" class="media-picker-overlay" role="dialog" aria-modal="true" aria-label="Select media">
    <section class="media-picker-dialog page-card">
      <header class="page-header">
        <div>
          <h2>Select media</h2>
          <p class="page-card__lead">Choose one or more assets from the library.</p>
        </div>
        <AppButton variant="secondary" type="button" @click="close">Close</AppButton>
      </header>

      <MediaLibraryBrowser
        selection-mode
        :multiple="multiple"
        :selected-ids="selectedIds"
        @select="onSelect"
        @confirm-selection="onConfirm"
        @cancel="close"
      />
    </section>
  </div>
</template>
