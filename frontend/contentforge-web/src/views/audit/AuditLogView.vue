<script setup lang="ts">
import { onMounted, ref, watch } from 'vue';
import AppAlert from '@/components/common/AppAlert.vue';
import AppButton from '@/components/common/AppButton.vue';
import AppSpinner from '@/components/common/AppSpinner.vue';
import PaginationBar from '@/components/common/PaginationBar.vue';
import AuditLogDetailPanel from '@/components/audit/AuditLogDetailPanel.vue';
import { getAuditLog, listAuditLogs } from '@/api/audit';
import { useApiErrorHandling } from '@/composables/useApiErrorHandling';
import { useAuditPermissions } from '@/composables/useAuditPermissions';
import {
  AUDIT_ACTION_OPTIONS,
  AUDIT_ENTITY_TYPE_OPTIONS,
  formatAuditTimestamp,
  getAuditActionLabel,
  type AuditActionName,
  type AuditLogEntry,
} from '@/types/audit';

const { handleError } = useApiErrorHandling();
const { canRead } = useAuditPermissions();

const items = ref<AuditLogEntry[]>([]);
const loading = ref(true);
const detailLoading = ref(false);
const errorMessage = ref<string | null>(null);
const selectedEntryId = ref<string | null>(null);
const selectedEntry = ref<AuditLogEntry | null>(null);

const actorFilter = ref('');
const actionFilter = ref<'' | AuditActionName>('');
const entityTypeFilter = ref('');
const entityIdFilter = ref('');
const fromFilter = ref('');
const toFilter = ref('');

const page = ref(1);
const pageSize = ref(25);
const totalItems = ref(0);
const totalPages = ref(1);

function toIsoDateTime(value: string): string | undefined {
  if (!value.trim()) {
    return undefined;
  }

  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? undefined : date.toISOString();
}

async function loadAuditLogs(): Promise<void> {
  if (!canRead.value) {
    loading.value = false;
    return;
  }

  loading.value = true;
  errorMessage.value = null;

  try {
    const response = await listAuditLogs({
      page: page.value,
      pageSize: pageSize.value,
      sortBy: 'timestamp',
      sortDirection: 'desc',
      userId: actorFilter.value.trim() || undefined,
      action: actionFilter.value || undefined,
      entityType: entityTypeFilter.value || undefined,
      entityId: entityIdFilter.value.trim() || undefined,
      from: toIsoDateTime(fromFilter.value),
      to: toIsoDateTime(toFilter.value),
    });

    items.value = response.items;
    totalItems.value = response.totalItems;
    totalPages.value = Math.max(response.totalPages, 1);
    page.value = response.page;

    if (selectedEntryId.value) {
      const stillVisible = response.items.some((item) => item.id === selectedEntryId.value);
      if (!stillVisible) {
        selectedEntryId.value = null;
        selectedEntry.value = null;
      }
    }
  } catch (error) {
    errorMessage.value = 'Unable to load audit log.';
    handleError(error, 'Failed to load audit log');
  } finally {
    loading.value = false;
  }
}

async function selectEntry(entry: AuditLogEntry): Promise<void> {
  selectedEntryId.value = entry.id;
  detailLoading.value = true;

  try {
    selectedEntry.value = await getAuditLog(entry.id);
  } catch (error) {
    selectedEntry.value = entry;
    handleError(error, 'Failed to load audit event details');
  } finally {
    detailLoading.value = false;
  }
}

function applyFilters(): void {
  page.value = 1;
  void loadAuditLogs();
}

function clearFilters(): void {
  actorFilter.value = '';
  actionFilter.value = '';
  entityTypeFilter.value = '';
  entityIdFilter.value = '';
  fromFilter.value = '';
  toFilter.value = '';
  page.value = 1;
  void loadAuditLogs();
}

onMounted(() => {
  void loadAuditLogs();
});

watch([page, pageSize], () => {
  void loadAuditLogs();
});
</script>

<template>
  <section class="page-card">
    <header class="page-header">
      <div>
        <h2>Audit log</h2>
        <p class="page-card__lead">
          Read-only history of security-sensitive and content-management actions. Sensitive values in metadata are redacted.
        </p>
      </div>
    </header>

    <AppAlert
      v-if="!canRead"
      kind="warning"
      title="Access restricted"
      message="You do not have permission to view the audit log."
    />

    <template v-else>
      <form class="toolbar audit-toolbar" @submit.prevent="applyFilters">
        <label class="form-field">
          <span>Actor (user ID)</span>
          <input v-model="actorFilter" type="text" placeholder="11111111-1111-1111-1111-111111111111" />
        </label>
        <label class="form-field">
          <span>Action</span>
          <select v-model="actionFilter">
            <option value="">All actions</option>
            <option v-for="option in AUDIT_ACTION_OPTIONS" :key="option.value" :value="option.value">
              {{ option.label }}
            </option>
          </select>
        </label>
        <label class="form-field">
          <span>Entity type</span>
          <select v-model="entityTypeFilter">
            <option v-for="option in AUDIT_ENTITY_TYPE_OPTIONS" :key="option.value" :value="option.value">
              {{ option.label }}
            </option>
          </select>
        </label>
        <label class="form-field">
          <span>Entity ID</span>
          <input v-model="entityIdFilter" type="text" placeholder="Entity identifier" />
        </label>
        <label class="form-field">
          <span>From</span>
          <input v-model="fromFilter" type="datetime-local" />
        </label>
        <label class="form-field">
          <span>To</span>
          <input v-model="toFilter" type="datetime-local" />
        </label>
        <div class="audit-toolbar__actions">
          <AppButton type="submit" variant="secondary">Apply filters</AppButton>
          <AppButton type="button" variant="ghost" @click="clearFilters">Clear</AppButton>
        </div>
      </form>

      <AppAlert
        v-if="errorMessage"
        kind="error"
        title="Load failed"
        :message="errorMessage"
      />

      <div class="audit-log-grid">
        <section class="panel-card audit-log-list">
          <div v-if="loading" class="inline-loading">
            <AppSpinner label="Loading audit log" />
            <span>Loading audit events…</span>
          </div>

          <div v-else-if="items.length === 0" class="empty-state">
            No audit events match the current filters.
          </div>

          <template v-else>
            <div class="data-table-wrap">
              <table class="data-table">
                <thead>
                  <tr>
                    <th scope="col">Timestamp</th>
                    <th scope="col">Action</th>
                    <th scope="col">Actor</th>
                    <th scope="col">Entity</th>
                  </tr>
                </thead>
                <tbody>
                  <tr
                    v-for="item in items"
                    :key="item.id"
                    class="audit-log-row"
                    :class="{ 'audit-log-row--selected': selectedEntryId === item.id }"
                  >
                    <td>
                      <button type="button" class="audit-log-row__button" @click="selectEntry(item)">
                        {{ formatAuditTimestamp(item.timestamp) }}
                      </button>
                    </td>
                    <td>{{ getAuditActionLabel(item.action) }}</td>
                    <td>{{ item.userId ?? '—' }}</td>
                    <td>{{ item.entityType }} · {{ item.entityId }}</td>
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

        <AuditLogDetailPanel :entry="selectedEntry" :loading="detailLoading" />
      </div>
    </template>
  </section>
</template>
