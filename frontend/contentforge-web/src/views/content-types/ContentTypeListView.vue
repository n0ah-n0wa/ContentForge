<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useRouter } from 'vue-router';
import AppAlert from '@/components/common/AppAlert.vue';
import AppButton from '@/components/common/AppButton.vue';
import AppSpinner from '@/components/common/AppSpinner.vue';
import { listContentTypes } from '@/api/contentTypes';
import { useContentTypePermissions } from '@/composables/useContentTypePermissions';
import type { ContentType } from '@/types/contentTypes';

const router = useRouter();
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
      isActive: activeFilter.value === 'all' ? undefined : activeFilter.value === 'active',
    });
    items.value = response.items;
  } catch {
    errorMessage.value = 'Unable to load content types.';
  } finally {
    loading.value = false;
  }
}

function applyFilters(): void {
  void loadContentTypes();
}

function clearFilters(): void {
  search.value = '';
  activeFilter.value = 'all';
  void loadContentTypes();
}

const filtersActive = computed(() => Boolean(search.value.trim()) || activeFilter.value !== 'all');

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

    <form class="toolbar" @submit.prevent="applyFilters">
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
      <div class="toolbar__actions">
        <AppButton type="submit" variant="secondary">Apply</AppButton>
        <AppButton v-if="filtersActive" type="button" variant="ghost" @click="clearFilters">
          Clear
        </AppButton>
      </div>
    </form>

    <AppAlert v-if="errorMessage" kind="error" title="Unable to load" :message="errorMessage">
      <template #actions>
        <AppButton variant="secondary" type="button" @click="loadContentTypes">Retry</AppButton>
      </template>
    </AppAlert>

    <div v-if="loading" class="inline-loading">
      <AppSpinner label="Loading content types" />
      <span>Loading content types…</span>
    </div>

    <div v-else-if="items.length === 0" class="empty-state empty-state--stack">
      <p>
        {{
          filtersActive
            ? 'No content types match the current filters.'
            : 'No content types have been defined yet.'
        }}
      </p>
      <div class="toolbar__actions">
        <AppButton v-if="filtersActive" variant="secondary" type="button" @click="clearFilters">
          Clear filters
        </AppButton>
        <AppButton
          v-if="canCreate && !filtersActive"
          type="button"
          @click="router.push({ name: 'content-type-create' })"
        >
          New content type
        </AppButton>
      </div>
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
            <td>
              <code>{{ item.name }}</code>
            </td>
            <td>{{ item.slug }}</td>
            <td>{{ item.fieldCount ?? item.fields.length }}</td>
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
