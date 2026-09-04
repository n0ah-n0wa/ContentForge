<script setup lang="ts">
import { CONTENT_STATUS_LABELS, ContentStatus, type ContentEntry } from '@/types/contentEntries';

defineProps<{
  entry: ContentEntry | null;
  isDirty: boolean;
  validationMessage?: string | null;
}>();

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

function formatDate(value: string | null): string {
  if (!value) {
    return '—';
  }

  return new Intl.DateTimeFormat(undefined, {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(new Date(value));
}
</script>

<template>
  <section class="entry-metadata">
    <div class="entry-metadata__grid">
      <div>
        <span class="entry-metadata__label">Status</span>
        <span
          v-if="entry"
          class="entry-status"
          :class="statusClass(entry.status)"
          data-testid="entry-status"
        >
          {{ CONTENT_STATUS_LABELS[entry.status] }}
        </span>
        <span v-else>—</span>
      </div>
      <div>
        <span class="entry-metadata__label">Current version</span>
        <strong data-testid="entry-current-version">{{ entry?.currentVersion ?? '—' }}</strong>
      </div>
      <div>
        <span class="entry-metadata__label">Last updated</span>
        <strong>{{ formatDate(entry?.updatedAt ?? null) }}</strong>
      </div>
      <div>
        <span class="entry-metadata__label">Updated by</span>
        <strong>{{ entry?.updatedBy ?? '—' }}</strong>
      </div>
      <div>
        <span class="entry-metadata__label">Validation</span>
        <strong
          data-testid="entry-validation"
          :class="{ 'entry-metadata__warning': validationMessage }"
        >
          {{ validationMessage ?? (isDirty ? 'Unsaved changes' : 'Saved') }}
        </strong>
      </div>
    </div>
  </section>
</template>
