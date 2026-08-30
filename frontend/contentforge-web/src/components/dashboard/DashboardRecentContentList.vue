<script setup lang="ts">
import { useRouter } from 'vue-router';
import AppButton from '@/components/common/AppButton.vue';
import { CONTENT_STATUS_LABELS, ContentStatus } from '@/types/contentEntries';
import type { DashboardRecentContentItem } from '@/types/dashboard';

defineProps<{
  items: DashboardRecentContentItem[];
  canReadContent: boolean;
}>();

const router = useRouter();

function statusClass(status: ContentStatus): string {
  switch (status) {
    case ContentStatus.Published:
      return 'entry-status--published';
    case ContentStatus.InReview:
      return 'entry-status--review';
    case ContentStatus.Archived:
      return 'entry-status--archived';
    default:
      return 'entry-status--draft';
  }
}

function openEntry(item: DashboardRecentContentItem): void {
  router.push({
    name: 'content-entry-view',
    params: {
      contentTypeSlug: item.contentTypeSlug,
      entryId: item.id,
    },
  });
}
</script>

<template>
  <section class="dashboard-panel" aria-labelledby="recent-content-heading">
    <header class="section-header">
      <div>
        <h3 id="recent-content-heading">Recent content</h3>
        <p>Latest updated entries from the CMS.</p>
      </div>
      <AppButton
        v-if="canReadContent"
        variant="secondary"
        @click="router.push({ name: 'content' })"
      >
        Browse content
      </AppButton>
    </header>

    <p v-if="!canReadContent" class="empty-state">
      You do not have permission to view content entries.
    </p>

    <p v-else-if="items.length === 0" class="empty-state">
      No content entries yet. Create a content type and add your first entry.
    </p>

    <ul v-else class="dashboard-list">
      <li v-for="item in items" :key="item.id" class="dashboard-list__item">
        <div class="dashboard-list__summary">
          <strong>{{ item.slug }}</strong>
          <span>{{ item.contentTypeDisplayName }}</span>
          <span>{{ new Date(item.updatedAt).toLocaleString() }}</span>
        </div>
        <span class="entry-status" :class="statusClass(item.status)">
          {{ CONTENT_STATUS_LABELS[item.status] }}
        </span>
        <AppButton variant="ghost" @click="openEntry(item)">View</AppButton>
      </li>
    </ul>
  </section>
</template>
