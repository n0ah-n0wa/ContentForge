<script setup lang="ts">
import { computed } from 'vue';
import type { MediaAsset } from '@/types/media';
import { isImageMedia, resolveMediaUrl } from '@/utils/mediaUrl';

const props = defineProps<{
  asset: MediaAsset;
  selected?: boolean;
  selectable?: boolean;
}>();

const emit = defineEmits<{
  select: [];
}>();

const previewUrl = computed(() => resolveMediaUrl(props.asset.url));
const isImage = computed(() => isImageMedia(props.asset.contentType));
</script>

<template>
  <article
    class="media-asset-card"
    :class="{ 'media-asset-card--selected': selected }"
  >
    <button
      type="button"
      class="media-asset-card__button"
      :disabled="!selectable"
      @click="emit('select')"
    >
      <div class="media-asset-card__preview">
        <img
          v-if="isImage && previewUrl"
          :src="previewUrl"
          :alt="asset.altText ?? asset.originalFileName"
          loading="lazy"
        />
        <div v-else class="media-asset-card__placeholder">
          {{ asset.contentType.split('/')[1]?.toUpperCase() ?? 'FILE' }}
        </div>
      </div>
      <div class="media-asset-card__meta">
        <strong>{{ asset.title ?? asset.originalFileName }}</strong>
        <span>{{ asset.contentType }}</span>
      </div>
    </button>
  </article>
</template>
