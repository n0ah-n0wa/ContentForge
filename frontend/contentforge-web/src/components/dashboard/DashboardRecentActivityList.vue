<script setup lang="ts">
import { useRouter } from 'vue-router';
import AppButton from '@/components/common/AppButton.vue';
import { AUDIT_ACTION_LABELS, type DashboardRecentActivityItem } from '@/types/dashboard';

defineProps<{
  items: DashboardRecentActivityItem[];
  canReadAudit: boolean;
}>();

const router = useRouter();

function actionLabel(action: number): string {
  return AUDIT_ACTION_LABELS[action] ?? `Action ${action}`;
}
</script>

<template>
  <section class="dashboard-panel" aria-labelledby="recent-activity-heading">
    <header class="section-header">
      <div>
        <h3 id="recent-activity-heading">Recent activity</h3>
        <p>Latest auditable actions recorded by the platform.</p>
      </div>
      <AppButton
        v-if="canReadAudit"
        variant="secondary"
        @click="router.push({ name: 'audit' })"
      >
        Open audit log
      </AppButton>
    </header>

    <p v-if="!canReadAudit" class="empty-state">
      You do not have permission to view audit activity.
    </p>

    <p v-else-if="items.length === 0" class="empty-state">
      No audit activity has been recorded yet.
    </p>

    <ul v-else class="dashboard-list">
      <li v-for="item in items" :key="item.id" class="dashboard-list__item">
        <div class="dashboard-list__summary">
          <strong>{{ actionLabel(item.action) }}</strong>
          <span>{{ item.entityType }} · {{ item.entityId }}</span>
          <span>{{ new Date(item.timestamp).toLocaleString() }}</span>
        </div>
      </li>
    </ul>
  </section>
</template>
