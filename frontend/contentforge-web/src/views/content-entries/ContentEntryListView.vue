<script setup lang="ts">
import { computed, onMounted, ref, watch } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import AppAlert from '@/components/common/AppAlert.vue';
import AppButton from '@/components/common/AppButton.vue';
import AppSpinner from '@/components/common/AppSpinner.vue';
import PaginationBar from '@/components/common/PaginationBar.vue';
import { listContentEntries, searchContentEntries } from '@/api/content';
import { listContentTypes } from '@/api/contentTypes';
import { useContentPermissions } from '@/composables/useContentPermissions';
import {
  CONTENT_STATUS_FILTER_OPTIONS,
  CONTENT_STATUS_LABELS,
  type ContentEntry,
} from '@/types/contentEntries';
import type { ContentType } from '@/types/contentTypes';

const SORT_OPTIONS = [
  { value: 'updatedAt', label: 'Last updated' },
  { value: 'createdAt', label: 'Created' },
  { value: 'slug', label: 'Slug' },
  { value: 'status', label: 'Status' },
  { value: 'publishedAt', label: 'Published' },
] as const;

const route = useRoute();
const router = useRouter();
const { canCreate, canRead, canUpdate } = useContentPermissions();

const contentTypeSlug = computed(() => route.params.contentTypeSlug as string);
const contentType = ref<ContentType | null>(null);
const items = ref<ContentEntry[]>([]);
const loading = ref(true);
const errorMessage = ref<string | null>(null);
const search = ref('');
const statusFilter = ref('');
const sortBy = ref<(typeof SORT_OPTIONS)[number]['value']>('updatedAt');
const sortDirection = ref<'asc' | 'desc'>('desc');
const page = ref(1);
const pageSize = ref(25);
const totalItems = ref(0);
const totalPages = ref(1);
const useAdvancedSearch = ref(false);

async function resolveContentType(): Promise<ContentType | null> {
  const types = await listContentTypes({
    page: 1,
    pageSize: 100,
    sortBy: 'name',
    sortDirection: 'asc',
    search: contentTypeSlug.value,
  });

  return (
    types.items.find((item) => item.slug === contentTypeSlug.value) ??
    types.items.find((item) => item.name === contentTypeSlug.value) ??
    null
  );
}

async function loadEntries(): Promise<void> {
  if (!contentType.value) {
    return;
  }

  loading.value = true;
  errorMessage.value = null;

  try {
    const commonParams = {
      page: page.value,
      pageSize: pageSize.value,
      sortBy: sortBy.value,
      sortDirection: sortDirection.value,
      contentTypeId: contentType.value.id,
      status: statusFilter.value || undefined,
    };

    const response = useAdvancedSearch.value
      ? await searchContentEntries({
          ...commonParams,
          keyword: search.value.trim() || undefined,
        })
      : await listContentEntries({
          ...commonParams,
          search: search.value.trim() || undefined,
        });

    items.value = response.items;
    totalItems.value = response.totalItems;
    totalPages.value = Math.max(response.totalPages, 1);
    page.value = response.page;
  } catch {
    errorMessage.value = 'Unable to load content entries.';
  } finally {
    loading.value = false;
  }
}

async function initializePage(): Promise<void> {
  loading.value = true;
  errorMessage.value = null;

  try {
    contentType.value = await resolveContentType();
    if (!contentType.value) {
      errorMessage.value = 'Content type not found.';
      items.value = [];
      return;
    }

    await loadEntries();
  } catch {
    errorMessage.value = 'Unable to load content entries.';
    loading.value = false;
  }
}

function applyFilters(): void {
  page.value = 1;
  void loadEntries();
}

function clearFilters(): void {
  search.value = '';
  statusFilter.value = '';
  sortBy.value = 'updatedAt';
  sortDirection.value = 'desc';
  useAdvancedSearch.value = false;
  page.value = 1;
  void loadEntries();
}

const filtersActive = computed(
  () =>
    Boolean(search.value.trim()) ||
    Boolean(statusFilter.value) ||
    useAdvancedSearch.value ||
    sortBy.value !== 'updatedAt' ||
    sortDirection.value !== 'desc',
);

function toggleSortDirection(): void {
  sortDirection.value = sortDirection.value === 'asc' ? 'desc' : 'asc';
  applyFilters();
}

watch(contentTypeSlug, () => {
  page.value = 1;
  void initializePage();
});

watch(page, () => {
  void loadEntries();
});

watch(pageSize, (newSize, oldSize) => {
  if (newSize === oldSize) {
    return;
  }

  if (page.value !== 1) {
    page.value = 1;
    return;
  }

  void loadEntries();
});

onMounted(() => {
  void initializePage();
});
</script>

<template>
  <section class="page-card">
    <header class="page-header">
      <div>
        <h2>{{ contentType?.displayName ?? 'Content entries' }}</h2>
        <p class="page-card__lead">Browse, filter, and manage entries for this content type.</p>
      </div>
      <AppButton
        v-if="canCreate && contentType"
        @click="
          router.push({
            name: 'content-entry-create',
            params: { contentTypeSlug: contentType.slug },
          })
        "
      >
        New entry
      </AppButton>
    </header>

    <form class="content-list-toolbar" @submit.prevent="applyFilters">
      <label class="form-field">
        <span>Search</span>
        <input
          v-model="search"
          type="search"
          :placeholder="useAdvancedSearch ? 'Keyword search' : 'Search by slug'"
        />
      </label>
      <label class="form-field form-field--inline">
        <input v-model="useAdvancedSearch" type="checkbox" />
        <span>Advanced keyword search</span>
      </label>
      <label class="form-field">
        <span>Status</span>
        <select v-model="statusFilter">
          <option value="">All statuses</option>
          <option
            v-for="option in CONTENT_STATUS_FILTER_OPTIONS"
            :key="option.value"
            :value="option.value"
          >
            {{ option.label }}
          </option>
        </select>
      </label>
      <label class="form-field">
        <span>Sort by</span>
        <select v-model="sortBy">
          <option v-for="option in SORT_OPTIONS" :key="option.value" :value="option.value">
            {{ option.label }}
          </option>
        </select>
      </label>
      <div class="content-list-toolbar__actions">
        <AppButton variant="secondary" type="button" @click="toggleSortDirection">
          Sort {{ sortDirection === 'asc' ? 'ascending' : 'descending' }}
        </AppButton>
        <AppButton type="submit" variant="secondary">Apply</AppButton>
        <AppButton v-if="filtersActive" type="button" variant="ghost" @click="clearFilters">
          Clear
        </AppButton>
      </div>
    </form>

    <AppAlert v-if="errorMessage" kind="error" title="Unable to load" :message="errorMessage">
      <template #actions>
        <AppButton variant="secondary" type="button" @click="initializePage">Retry</AppButton>
      </template>
    </AppAlert>

    <div v-if="loading" class="inline-loading">
      <AppSpinner label="Loading content entries" />
      <span>Loading content entries…</span>
    </div>

    <div v-else-if="items.length === 0" class="empty-state empty-state--stack">
      <p>
        {{
          filtersActive
            ? 'No entries match the current filters.'
            : 'No entries yet for this content type.'
        }}
      </p>
      <div class="content-list-toolbar__actions">
        <AppButton v-if="filtersActive" variant="secondary" type="button" @click="clearFilters">
          Clear filters
        </AppButton>
        <AppButton
          v-if="canCreate && contentType && !filtersActive"
          type="button"
          @click="
            router.push({
              name: 'content-entry-create',
              params: { contentTypeSlug: contentType.slug },
            })
          "
        >
          New entry
        </AppButton>
      </div>
    </div>

    <template v-else>
      <div class="data-table-wrap">
        <table class="data-table">
          <thead>
            <tr>
              <th scope="col">Slug</th>
              <th scope="col">Status</th>
              <th scope="col">Version</th>
              <th scope="col">Updated</th>
              <th scope="col">Published</th>
              <th scope="col"><span class="sr-only">Actions</span></th>
            </tr>
          </thead>
          <tbody>
            <tr v-for="item in items" :key="item.id">
              <td>{{ item.slug }}</td>
              <td>{{ CONTENT_STATUS_LABELS[item.status] }}</td>
              <td>{{ item.currentVersion }}</td>
              <td>{{ new Date(item.updatedAt).toLocaleString() }}</td>
              <td>
                {{ item.publishedAt ? new Date(item.publishedAt).toLocaleString() : '—' }}
              </td>
              <td class="content-list-actions">
                <AppButton
                  v-if="canRead"
                  variant="ghost"
                  @click="
                    router.push({
                      name: 'content-entry-view',
                      params: {
                        contentTypeSlug: contentType?.slug ?? contentTypeSlug,
                        entryId: item.id,
                      },
                    })
                  "
                >
                  View
                </AppButton>
                <AppButton
                  v-if="canUpdate"
                  variant="secondary"
                  @click="
                    router.push({
                      name: 'content-entry-edit',
                      params: {
                        contentTypeSlug: contentType?.slug ?? contentTypeSlug,
                        entryId: item.id,
                      },
                    })
                  "
                >
                  Edit
                </AppButton>
              </td>
            </tr>
          </tbody>
        </table>
      </div>

      <PaginationBar
        v-model:page="page"
        v-model:page-size="pageSize"
        :total-items="totalItems"
        :total-pages="totalPages"
        :disabled="loading"
      />
    </template>
  </section>
</template>
