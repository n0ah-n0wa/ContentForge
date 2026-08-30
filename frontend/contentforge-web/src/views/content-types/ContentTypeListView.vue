<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { useRouter } from 'vue-router';
import AppAlert from '@/components/common/AppAlert.vue';
import AppButton from '@/components/common/AppButton.vue';
import AppSpinner from '@/components/common/AppSpinner.vue';
import { listContentTypes } from '@/api/contentTypes';
import { useContentTypePermissions } from '@/composables/useContentTypePermissions';
import { useApiErrorHandling } from '@/composables/useApiErrorHandling';
import type { ContentType } from '@/types/contentTypes';

const router = useRouter();
const { handleError } = useApiErrorHandling();
const { canCreate } = useContentTypePermissions();

const items = ref<ContentType[]>([]);
const loading = ref(true);
const errorMessage = ref<string | null>(null);
const search = ref('');
const activeFilter = ref<'all' | 'active' | 'inactive'>('all');

async function loadContentTypes(): Promise<void> {
  loading.value = true;
  errorMessage.value = null;

  try {
    const response = await listContentTypes({
      page: 1,
      pageSize: 100,
      sortBy: 'name',
      sortDirection: 'asc',
      search: search.value.trim() || undefined,
      isActive:
        activeFilter.value === 'all' ? undefined : activeFilter.value === 'active',
    });
    items.value = response.items;
  } catch (error) {
    errorMessage.value = 'Unable to load content types.';
    handleError(error, 'Failed to load content types');
  } finally {
    loading.value = false;
  }
}

onMounted(() => {
  void loadContentTypes();
});
</script>

<template>
  <section class="page-card">
    <header class="page-header">
      <div>
        <h2>Content types</h2>
        <p class="page-card__lead">
          Define dynamic schemas used to structure content entries across the CMS.
        </p>
      </div>
      <AppButton v-if="canCreate" @click="router.push({ name: 'content-type-create' })">
        New content type
      </AppButton>
    </header>

    <form class="toolbar" @submit.prevent="loadContentTypes">
      <label class="form-field">
        <span>Search</span>
        <input v-model="search" type="search" placeholder="Search by name or slug" />
      </label>
      <label class="form-field">
        <span>Status</span>
        <select v-model="activeFilter">
          <option value="all">All</option>
          <option value="active">Active</option>
          <option value="inactive">Inactive</option>
        </select>
      </label>
      <AppButton type="submit" variant="secondary">Apply</AppButton>
    </form>

    <AppAlert
      v-if="errorMessage"
      kind="error"
      title="Load failed"
      :message="errorMessage"
    />

    <div v-if="loading" class="inline-loading">
      <AppSpinner label="Loading content types" />
      <span>Loading content types…</span>
    </div>

    <div v-else-if="items.length === 0" class="empty-state">
      No content types match the current filters.
    </div>

    <div v-else class="data-table-wrap">
      <table class="data-table">
        <thead>
          <tr>
            <th scope="col">Display name</th>
            <th scope="col">System name</th>
            <th scope="col">Slug</th>
            <th scope="col">Fields</th>
            <th scope="col">Status</th>
            <th scope="col">Version</th>
            <th scope="col"><span class="sr-only">Actions</span></th>
          </tr>
        </thead>
        <tbody>
          <tr v-for="item in items" :key="item.id">
            <td>{{ item.displayName }}</td>
            <td><code>{{ item.name }}</code></td>
            <td>{{ item.slug }}</td>
            <td>{{ item.fields.length }}</td>
            <td>
              <span class="status-badge" :class="{ 'status-badge--inactive': !item.isActive }">
                {{ item.isActive ? 'Active' : 'Inactive' }}
              </span>
            </td>
            <td>{{ item.version }}</td>
            <td>
              <AppButton
                variant="secondary"
                @click="router.push({ name: 'content-type-edit', params: { id: item.id } })"
              >
                Manage
              </AppButton>
            </td>
          </tr>
        </tbody>
      </table>
    </div>
  </section>
</template>
