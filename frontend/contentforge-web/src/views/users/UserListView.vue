<script setup lang="ts">
import { onMounted, ref, watch, computed } from 'vue';
import { useRouter } from 'vue-router';
import AppAlert from '@/components/common/AppAlert.vue';
import AppButton from '@/components/common/AppButton.vue';
import AppSpinner from '@/components/common/AppSpinner.vue';
import PaginationBar from '@/components/common/PaginationBar.vue';
import { listUsers } from '@/api/users';
import { useUserPermissions } from '@/composables/useUserPermissions';
import {
  formatUserTimestamp,
  getUserRoleLabel,
  type UserAccount,
  type UserRoleName,
  USER_ROLE_OPTIONS,
} from '@/types/users';

const router = useRouter();
const { canRead, canCreate } = useUserPermissions();

const items = ref<UserAccount[]>([]);
const loading = ref(true);
const errorMessage = ref<string | null>(null);
const search = ref('');
const activeFilter = ref<'all' | 'active' | 'inactive'>('all');
const roleFilter = ref<'' | UserRoleName>('');
const page = ref(1);
const pageSize = ref(25);
const totalItems = ref(0);
const totalPages = ref(1);

async function loadUsers(): Promise<void> {
  if (!canRead.value) {
    loading.value = false;
    return;
  }

  loading.value = true;
  errorMessage.value = null;

  try {
    const response = await listUsers({
      page: page.value,
      pageSize: pageSize.value,
      sortBy: 'email',
      sortDirection: 'asc',
      search: search.value.trim() || undefined,
      isActive: activeFilter.value === 'all' ? undefined : activeFilter.value === 'active',
      role: roleFilter.value || undefined,
    });
    items.value = response.items;
    totalItems.value = response.totalItems;
    totalPages.value = Math.max(response.totalPages, 1);
    page.value = response.page;
  } catch {
    errorMessage.value = 'Unable to load users.';
  } finally {
    loading.value = false;
  }
}

function applyFilters(): void {
  page.value = 1;
  void loadUsers();
}

function clearFilters(): void {
  search.value = '';
  activeFilter.value = 'all';
  roleFilter.value = '';
  page.value = 1;
  void loadUsers();
}

const filtersActive = computed(
  () => Boolean(search.value.trim()) || activeFilter.value !== 'all' || Boolean(roleFilter.value),
);

onMounted(() => {
  void loadUsers();
});

watch([page, pageSize], () => {
  void loadUsers();
});
</script>

<template>
  <section class="page-card">
    <header class="page-header">
      <div>
        <h2>Users</h2>
        <p class="page-card__lead">
          Manage CMS user accounts, roles, and access. Authorization is enforced by the backend on
          every request.
        </p>
      </div>
      <AppButton v-if="canCreate" @click="router.push({ name: 'user-create' })">
        New user
      </AppButton>
    </header>

    <AppAlert
      v-if="!canRead"
      kind="warning"
      title="Access restricted"
      message="You do not have permission to view users."
    />

    <template v-else>
      <form class="toolbar" @submit.prevent="applyFilters">
        <label class="form-field">
          <span>Search</span>
          <input v-model="search" type="search" placeholder="Search by email or name" />
        </label>
        <label class="form-field">
          <span>Status</span>
          <select v-model="activeFilter">
            <option value="all">All</option>
            <option value="active">Active</option>
            <option value="inactive">Disabled</option>
          </select>
        </label>
        <label class="form-field">
          <span>Role</span>
          <select v-model="roleFilter">
            <option value="">All roles</option>
            <option v-for="option in USER_ROLE_OPTIONS" :key="option.value" :value="option.value">
              {{ option.label }}
            </option>
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
          <AppButton variant="secondary" type="button" @click="loadUsers">Retry</AppButton>
        </template>
      </AppAlert>

      <div v-if="loading" class="inline-loading">
        <AppSpinner label="Loading users" />
        <span>Loading users…</span>
      </div>

      <div v-else-if="items.length === 0" class="empty-state empty-state--stack">
        <p>
          {{
            filtersActive
              ? 'No users match the current filters.'
              : 'No users have been created yet.'
          }}
        </p>
        <div class="toolbar__actions">
          <AppButton v-if="filtersActive" variant="secondary" type="button" @click="clearFilters">
            Clear filters
          </AppButton>
          <AppButton
            v-if="canCreate && !filtersActive"
            type="button"
            @click="router.push({ name: 'user-create' })"
          >
            New user
          </AppButton>
        </div>
      </div>

      <template v-else>
        <div class="data-table-wrap">
          <table class="data-table">
            <thead>
              <tr>
                <th scope="col">Email</th>
                <th scope="col">Display name</th>
                <th scope="col">Role</th>
                <th scope="col">Status</th>
                <th scope="col">Last login</th>
                <th scope="col"><span class="sr-only">Actions</span></th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="item in items" :key="item.id">
                <td>{{ item.email }}</td>
                <td>{{ item.displayName }}</td>
                <td>{{ getUserRoleLabel(item.role) }}</td>
                <td>
                  <span class="status-badge" :class="{ 'status-badge--inactive': !item.isActive }">
                    {{ item.isActive ? 'Active' : 'Disabled' }}
                  </span>
                </td>
                <td>{{ formatUserTimestamp(item.lastLoginAt) }}</td>
                <td>
                  <AppButton
                    variant="secondary"
                    @click="router.push({ name: 'user-edit', params: { id: item.id } })"
                  >
                    Manage
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
    </template>
  </section>
</template>
