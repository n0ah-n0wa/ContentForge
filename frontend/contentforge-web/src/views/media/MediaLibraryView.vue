<script setup lang="ts">
import { onMounted, ref } from 'vue';
import AppAlert from '@/components/common/AppAlert.vue';
import MediaLibraryBrowser from '@/components/media/MediaLibraryBrowser.vue';
import MediaUploadPanel from '@/components/media/MediaUploadPanel.vue';
import { useMediaPermissions } from '@/composables/useMediaPermissions';

const { canRead, canUpload } = useMediaPermissions();
const browserRef = ref<InstanceType<typeof MediaLibraryBrowser> | null>(null);

function onUploaded(): void {
  void browserRef.value?.reload();
}

onMounted(() => {
  if (!canRead.value) {
    return;
  }
});
</script>

<template>
  <section class="page-card">
    <header class="page-header">
      <div>
        <h2>Media library</h2>
        <p class="page-card__lead">
          Upload, search, and manage media assets. Public URLs are shown; internal storage paths are never exposed.
        </p>
      </div>
    </header>

    <AppAlert
      v-if="!canRead"
      kind="warning"
      title="Access restricted"
      message="You do not have permission to view the media library."
    />

    <template v-else>
      <MediaUploadPanel v-if="canUpload" @uploaded="onUploaded" />
      <MediaLibraryBrowser ref="browserRef" />
    </template>
  </section>
</template>
