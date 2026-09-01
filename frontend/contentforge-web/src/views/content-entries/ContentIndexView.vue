<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { useRouter } from 'vue-router';
import AppAlert from '@/components/common/AppAlert.vue';
import AppButton from '@/components/common/AppButton.vue';
import AppSpinner from '@/components/common/AppSpinner.vue';
import { listContentTypes } from '@/api/contentTypes';
import { useApiErrorHandling } from '@/composables/useApiErrorHandling';
import { useContentPermissions } from '@/composables/useContentPermissions';
import type { ContentType } from '@/types/contentTypes';

const router = useRouter();
const { handleError } = useApiErrorHandling();
const { canRead } = useContentPermissions();

const items = ref<ContentType[]>([]);
const loading = ref(true);
const errorMessage = ref<string | null>(null);

async function loadContentTypes(): Promise<void> {
  loading.value = true;
  errorMessage.value = null;

  try {
    const response = await listContentTypes({
      page: 1,
      pageSize: 100,
      sortBy: 'name',
      sortDirection: 'asc',
      isActive: true,
    });
    items.value = response.items;
  } catch (error) {
    errorMessage.value = 'Unable to load content types.';
    handleError(error, 'Failed to load content');
  } finally {
    loading.value = false;
  }
}

onMounted(() => {
  if (!canRead.value) {
    return;
  }
  void loadContentTypes();
});
</script>

<template>
  <section class="page-card">
    <header class="page-header">
      <div>
        <h2>Content</h2>
        <p class="page-card__lead">Choose a content type to browse and edit entries.</p>
      </div>
    </header>

    <AppAlert v-if="errorMessage" kind="error" title="Load failed" :message="errorMessage" />

    <div v-if="loading" class="inline-loading">
      <AppSpinner label="Loading content types" />
      <span>Loading content types…</span>
    </div>

    <div v-else-if="items.length === 0" class="empty-state">
      No active content types are available.
    </div>

    <div v-else class="data-table-wrap">
      <table class="data-table">
        <thead>
          <tr>
            <th scope="col">Display name</th>
            <th scope="col">Slug</th>
            <th scope="col">Fields</th>
            <th scope="col"><span class="sr-only">Actions</span></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="item in items" :key="item.id">
            <td>{{ item.displayName }}</td>
            <td>{{ item.slug }}</td>
            <td>{{ item.fieldCount ?? item.fields.length }}</td>
            <td>
              <AppButton
                variant="secondary"
                @click="
                  router.push({ name: 'content-by-type', params: { contentTypeSlug: item.slug } })
                "
              >
                Open entries
              </AppButton>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </section>
</template>
