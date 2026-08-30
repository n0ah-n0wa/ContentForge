<script setup lang="ts">
import type { MediaAsset } from '@/types/media';
import { formatMediaSize, isImageMedia, resolveMediaUrl } from '@/utils/mediaUrl';
import AppButton from '@/components/common/AppButton.vue';

defineProps<{
  asset: MediaAsset;
  selected?: boolean;
}>();

const emit = defineEmits<{
  select: [];
}>();
</script>

<template>
  <article
    class="media-asset-row"
    :class="{ 'media-asset-row--selected': selected }"
  >
    <button type="button" class="media-asset-row__button" @click="emit('select')">
      <div class="media-asset-row__preview">
        <img
          v-if="isImageMedia(asset.contentType) && resolveMediaUrl(asset.url)"
          :src="resolveMediaUrl(asset.url)!"
          :alt="asset.altText ?? asset.originalFileName"
          loading="lazy"
        />
        <span v-else>{{ asset.contentType }}</span>
      </div>
      <div class="media-asset-row__meta">
        <strong>{{ asset.title ?? asset.originalFileName }}</strong>
        <span>{{ asset.originalFileName }}</span>
        <span>{{ formatMediaSize(asset.size) }} · {{ new Date(asset.uploadedAt).toLocaleString() }}</span>
      </div>
    </button>
    <AppButton variant="ghost" type="button" @click="emit('select')">Details</AppButton>
  </article>
</template>
