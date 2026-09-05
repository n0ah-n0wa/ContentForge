<script setup lang="ts">
import { ref } from 'vue';
import AppAlert from '@/components/common/AppAlert.vue';
import AppButton from '@/components/common/AppButton.vue';
import { uploadMedia } from '@/api/media';
import { useNotifications } from '@/composables/useNotifications';
import type { MediaAsset } from '@/types/media';
import { MEDIA_MAX_FILE_SIZE_BYTES } from '@/types/media';
import { formatMediaSize } from '@/utils/mediaUrl';

defineProps<{
  disabled?: boolean;
}>();

const emit = defineEmits<{
  uploaded: [asset: MediaAsset];
}>();

const { notifySuccess } = useNotifications();

const fileInput = ref<HTMLInputElement | null>(null);
const selectedFile = ref<File | null>(null);
const altText = ref('');
const title = ref('');
const description = ref('');
const uploading = ref(false);
const progress = ref(0);
const errorMessage = ref<string | null>(null);

function openFilePicker(): void {
  fileInput.value?.click();
}

function onFileSelected(event: Event): void {
  const input = event.target as HTMLInputElement;
  const file = input.files?.[0] ?? null;
  errorMessage.value = null;
  selectedFile.value = file;

  if (file && file.size > MEDIA_MAX_FILE_SIZE_BYTES) {
    errorMessage.value = `File exceeds the ${formatMediaSize(MEDIA_MAX_FILE_SIZE_BYTES)} upload limit.`;
    selectedFile.value = null;
    input.value = '';
  }
}

async function uploadSelectedFile(): Promise<void> {
  if (!selectedFile.value) {
    return;
  }

  uploading.value = true;
  progress.value = 0;
  errorMessage.value = null;

  try {
    const asset = await uploadMedia(
      selectedFile.value,
      {
        altText: altText.value.trim() || undefined,
        title: title.value.trim() || undefined,
        description: description.value.trim() || undefined,
      },
      (value) => {
        progress.value = value;
      },
    );
    selectedFile.value = null;
    altText.value = '';
    title.value = '';
    description.value = '';
    if (fileInput.value) {
      fileInput.value.value = '';
    }
    notifySuccess('Upload complete', `"${asset.originalFileName}" was added to the library.`);
    emit('uploaded', asset);
  } catch (error) {
    if (error instanceof Error) {
      errorMessage.value = error.message;
    } else {
      errorMessage.value = 'Upload failed.';
    }
  } finally {
    uploading.value = false;
    progress.value = 0;
  }
}
</script>

<template>
  <section class="media-upload-panel editor-panel">
    <header class="section-header">
      <div>
        <h3>Upload media</h3>
        <p>Select a file to upload. Storage paths and credentials are never exposed in the UI.</p>
      </div>
      <AppButton
        type="button"
        variant="secondary"
        :disabled="disabled || uploading"
        @click="openFilePicker"
      >
        Choose file
      </AppButton>
    </header>

    <input
      ref="fileInput"
      class="sr-only"
      type="file"
      aria-label="Choose file to upload"
      :disabled="disabled || uploading"
      @change="onFileSelected"
    />

    <p v-if="selectedFile" class="media-upload-panel__file">
      Selected: {{ selectedFile.name }} ({{ formatMediaSize(selectedFile.size) }})
    </p>

    <div
      v-if="uploading"
      class="media-upload-panel__progress"
      role="progressbar"
      aria-label="Upload progress"
      :aria-valuenow="progress"
      aria-valuemin="0"
      aria-valuemax="100"
    >
      <div class="media-upload-panel__progress-bar" :style="{ width: `${progress}%` }" />
      <span>{{ progress }}%</span>
    </div>

    <form v-if="selectedFile" class="stack-form" @submit.prevent="uploadSelectedFile">
      <label class="form-field">
        <span>Title</span>
        <input v-model="title" type="text" :disabled="uploading" />
      </label>
      <label class="form-field">
        <span>Alt text</span>
        <input v-model="altText" type="text" :disabled="uploading" />
      </label>
      <label class="form-field">
        <span>Description</span>
        <textarea v-model="description" rows="3" :disabled="uploading" />
      </label>
      <AppButton type="submit" :loading="uploading" :disabled="disabled">
        {{ uploading ? 'Uploading…' : 'Upload' }}
      </AppButton>
    </form>

    <AppAlert v-if="errorMessage" kind="error" title="Upload failed" :message="errorMessage" />
  </section>
</template>
