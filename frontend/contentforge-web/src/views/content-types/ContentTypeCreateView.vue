<script setup lang="ts">
import { ref } from 'vue';
import { useRouter } from 'vue-router';
import AppAlert from '@/components/common/AppAlert.vue';
import AppButton from '@/components/common/AppButton.vue';
import ContentTypeMetadataForm from '@/components/content-types/ContentTypeMetadataForm.vue';
import { createContentType } from '@/api/contentTypes';
import { ApiError, getValidationMessages, isValidationProblem } from '@/api/errors';
import { useContentTypePermissions } from '@/composables/useContentTypePermissions';
import { useApiErrorHandling } from '@/composables/useApiErrorHandling';

const router = useRouter();
const { handleError } = useApiErrorHandling();
const { canCreate } = useContentTypePermissions();

const metadata = ref({
  name: '',
  displayName: '',
  slug: '',
  description: '',
});
const saving = ref(false);
const errorMessage = ref<string | null>(null);

async function onSubmit(): Promise<void> {
  if (!canCreate.value) {
    return;
  }

  saving.value = true;
  errorMessage.value = null;

  try {
    const created = await createContentType({
      name: metadata.value.name.trim(),
      displayName: metadata.value.displayName.trim(),
      slug: metadata.value.slug.trim(),
      description: metadata.value.description.trim() || null,
    });
    await router.push({ name: 'content-type-edit', params: { id: created.id } });
  } catch (error) {
    if (
      error instanceof ApiError &&
      error.isValidationError &&
      isValidationProblem(error.problem)
    ) {
      errorMessage.value = getValidationMessages(error.problem).join(' ');
    } else if (error instanceof Error) {
      errorMessage.value = error.message;
    } else {
      errorMessage.value = 'Unable to create content type.';
    }
    handleError(error, 'Create content type failed');
  } finally {
    saving.value = false;
  }
}
</script>

<template>
  <section class="page-card">
    <header class="page-header">
      <div>
        <h2>Create content type</h2>
        <p class="page-card__lead">
          Define the metadata for a new dynamic content type. Fields are configured after creation.
        </p>
      </div>
    </header>

    <AppAlert
      v-if="!canCreate"
      kind="warning"
      title="Insufficient permissions"
      message="You need the contentType.create permission to create content types."
    />

    <AppAlert v-if="errorMessage" kind="error" title="Create failed" :message="errorMessage" />

    <form class="stack-form" @submit.prevent="onSubmit">
      <ContentTypeMetadataForm v-model="metadata" mode="create" :disabled="!canCreate || saving" />

      <div class="form-actions">
        <AppButton
          variant="secondary"
          type="button"
          @click="router.push({ name: 'content-types' })"
        >
          Cancel
        </AppButton>
        <AppButton type="submit" :loading="saving" :disabled="!canCreate">
          Create content type
        </AppButton>
      </div>
    </form>
  </section>
</template>
