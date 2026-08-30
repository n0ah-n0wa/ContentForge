<script setup lang="ts">
import { computed } from 'vue';
import AppSpinner from '@/components/common/AppSpinner.vue';
import type { AuditLogEntry } from '@/types/audit';
import { formatAuditTimestamp, getAuditActionLabel } from '@/types/audit';
import { sanitizeAuditMetadataForDisplay, truncateAuditUserAgent } from '@/utils/auditMetadata';

const props = defineProps<{
  entry: AuditLogEntry | null;
  loading?: boolean;
}>();

const displayMetadata = computed(() => sanitizeAuditMetadataForDisplay(props.entry?.metadata ?? null));
const displayUserAgent = computed(() => truncateAuditUserAgent(props.entry?.userAgent ?? null));
</script>

<template>
  <section class="panel-card audit-detail-panel" aria-live="polite">
    <h3>Event details</h3>

    <p v-if="!entry && !loading" class="empty-state">
      Select an audit event to inspect its details. Audit records are read-only.
    </p>

    <div v-else-if="loading" class="inline-loading">
      <AppSpinner label="Loading audit event" />
      <span>Loading event…</span>
    </div>

    <template v-else-if="entry">
      <dl class="metadata-list">
        <div>
          <dt>Timestamp</dt>
          <dd>{{ formatAuditTimestamp(entry.timestamp) }}</dd>
        </div>
        <div>
          <dt>Action</dt>
          <dd>{{ getAuditActionLabel(entry.action) }}</dd>
        </div>
        <div>
          <dt>Actor</dt>
          <dd>{{ entry.userId ?? 'System / unauthenticated' }}</dd>
        </div>
        <div>
          <dt>Entity</dt>
          <dd>{{ entry.entityType }} · {{ entry.entityId }}</dd>
        </div>
        <div v-if="entry.correlationId">
          <dt>Correlation ID</dt>
          <dd><code>{{ entry.correlationId }}</code></dd>
        </div>
        <div v-if="entry.ipAddress">
          <dt>IP address</dt>
          <dd>{{ entry.ipAddress }}</dd>
        </div>
        <div v-if="displayUserAgent">
          <dt>User agent</dt>
          <dd :title="entry.userAgent ?? undefined">{{ displayUserAgent }}</dd>
        </div>
      </dl>

      <div v-if="displayMetadata" class="audit-detail-panel__metadata">
        <h4>Metadata</h4>
        <pre>{{ displayMetadata }}</pre>
      </div>
      <p v-else class="entry-field__hint">No metadata was recorded for this event.</p>
    </template>
  </section>
</template>
