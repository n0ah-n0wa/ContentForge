<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import AppAlert from '@/components/common/AppAlert.vue';
import AppSpinner from '@/components/common/AppSpinner.vue';
import DashboardRecentActivityList from '@/components/dashboard/DashboardRecentActivityList.vue';
import DashboardRecentContentList from '@/components/dashboard/DashboardRecentContentList.vue';
import DashboardStatCard from '@/components/dashboard/DashboardStatCard.vue';
import { getDashboard } from '@/api/dashboard';
import { useApiErrorHandling } from '@/composables/useApiErrorHandling';
import { useContentPermissions } from '@/composables/useContentPermissions';
import { useAuthStore } from '@/stores/authStore';
import { getRoleDisplayName } from '@/types/auth';
import {
  DASHBOARD_STAT_CARDS,
  type DashboardContentStatistics,
  type DashboardData,
} from '@/types/dashboard';
import { ROUTE_PERMISSIONS } from '@/config/navigation';

const authStore = useAuthStore();
const { handleError } = useApiErrorHandling();
const { canRead: canReadContent } = useContentPermissions();
const canReadAudit = computed(() => authStore.hasPermission('audit.read'));

const loading = ref(true);
const errorMessage = ref<string | null>(null);
const dashboard = ref<DashboardData | null>(null);

const welcomeName = computed(() => authStore.user?.displayName ?? 'Administrator');
const roleLabel = computed(() => (authStore.user ? getRoleDisplayName(authStore.user.role) : ''));

const statistics = computed<DashboardContentStatistics | null>(
  () => dashboard.value?.contentStatistics ?? null,
);

const quickLinks = computed(() =>
  [
    { label: 'Content', name: 'content', permissions: ROUTE_PERMISSIONS.content },
    { label: 'Content types', name: 'content-types', permissions: ROUTE_PERMISSIONS.contentTypes },
    { label: 'Media', name: 'media' },
    { label: 'Audit log', name: 'audit', permissions: ROUTE_PERMISSIONS.audit },
  ].filter((link) => {
    if (!link.permissions?.length) {
      return true;
    }

    return link.permissions.every((permission) => authStore.hasPermission(permission));
  }),
);

function statValue(key: keyof DashboardContentStatistics): number {
  return statistics.value?.[key] ?? 0;
}

async function loadDashboard(): Promise<void> {
  loading.value = true;
  errorMessage.value = null;

  try {
    dashboard.value = await getDashboard();
  } catch (error) {
    errorMessage.value = 'Unable to load dashboard statistics.';
    handleError(error, 'Failed to load dashboard');
  } finally {
    loading.value = false;
  }
}

onMounted(() => {
  void loadDashboard();
});
</script>

<template>
  <section class="page-card dashboard-page">
    <header class="page-header">
      <div>
        <h2>Welcome, {{ welcomeName }}</h2>
        <p class="page-card__lead">
          Operational overview sourced from backend statistics and activity feeds.
        </p>
      </div>
    </header>

    <dl class="summary-list dashboard-summary">
      <div>
        <dt>Signed in as</dt>
        <dd>{{ authStore.user?.email }}</dd>
      </div>
      <div>
        <dt>Role</dt>
        <dd>{{ roleLabel }}</dd>
      </div>
    </dl>

    <nav class="dashboard-quick-links" aria-label="Quick navigation">
      <router-link
        v-for="link in quickLinks"
        :key="link.name"
        class="dashboard-quick-links__item"
        :to="{ name: link.name }"
      >
        {{ link.label }}
      </router-link>
    </nav>

    <AppAlert
      v-if="errorMessage"
      kind="error"
      title="Dashboard unavailable"
      :message="errorMessage"
    />

    <section aria-labelledby="dashboard-stats-heading">
      <h3 id="dashboard-stats-heading" class="sr-only">Content statistics</h3>

      <div v-if="loading" class="inline-loading">
        <AppSpinner label="Loading dashboard statistics" />
        <span>Loading dashboard statistics…</span>
      </div>

      <p v-else-if="!canReadContent" class="empty-state">
        Content statistics are hidden because you do not have content read permission.
      </p>

      <div v-else class="dashboard-stats-grid">
        <DashboardStatCard
          v-for="card in DASHBOARD_STAT_CARDS"
          :key="card.key"
          :label="card.label"
          :value="statValue(card.key)"
        />
      </div>
    </section>

    <div class="dashboard-panels">
      <DashboardRecentContentList
        :items="dashboard?.recentContent ?? []"
        :can-read-content="canReadContent"
      />
      <DashboardRecentActivityList
        :items="dashboard?.recentActivity ?? []"
        :can-read-audit="canReadAudit"
      />
    </div>
  </section>
</template>
