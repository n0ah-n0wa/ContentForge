<script setup lang="ts">
import { computed, ref } from 'vue';
import AppButton from '@/components/common/AppButton.vue';
import ConfirmActionPanel from '@/components/common/ConfirmActionPanel.vue';
import { useContentEntryActions } from '@/composables/useContentEntryActions';
import { useContentPermissions } from '@/composables/useContentPermissions';
import { ContentStatus, type ContentEntry } from '@/types/contentEntries';
import {
  getAvailableLifecycleActions,
  getLifecycleConfirmationMessage,
  isDestructiveLifecycleAction,
  lifecycleActionRequiresChangeSummary,
  LIFECYCLE_ACTION_LABELS,
  type LifecycleAction,
} from '@/utils/contentLifecycle';
import type { ConcurrencyConflictProblem } from '@/api/concurrency';

const props = defineProps<{
  entry: ContentEntry;
  concurrencyToken: number;
  disabled?: boolean;
}>();

const emit = defineEmits<{
  updated: [entry: ContentEntry];
  deleted: [];
  conflict: [problem: ConcurrencyConflictProblem];
  error: [error: unknown, title: string];
}>();

const entryRef = computed(() => props.entry);
const pendingAction = ref<LifecycleAction | null>(null);
const confirmChangeSummary = ref('');

const { canReview, canPublish, canArchive, canRestore, canDelete } = useContentPermissions();

const { actionLoading, runLifecycleAction } = useContentEntryActions(
  entryRef,
  (updated) => emit('updated', updated),
  (problem) => emit('conflict', problem),
  (error, title) => emit('error', error, title),
);

const visibleActions = computed(() => {
  const actions = getAvailableLifecycleActions(props.entry.status);
  return actions.filter((action) => canPerformAction(action));
});

const pendingConfirmation = computed(() =>
  pendingAction.value
    ? {
        action: pendingAction.value,
        title: LIFECYCLE_ACTION_LABELS[pendingAction.value],
        message: getLifecycleConfirmationMessage(pendingAction.value, props.entry.slug),
        destructive: isDestructiveLifecycleAction(pendingAction.value),
        requireChangeSummary: lifecycleActionRequiresChangeSummary(pendingAction.value),
      }
    : null,
);

function canPerformAction(action: LifecycleAction): boolean {
  switch (action) {
    case 'submit':
    case 'withdraw':
      return canReview.value;
    case 'publish':
    case 'unpublish':
      return canPublish.value;
    case 'archive':
      return canArchive.value;
    case 'restore':
      return canRestore.value && props.entry.status === ContentStatus.Archived;
    case 'delete':
      return canDelete.value;
    default:
      return false;
  }
}

function requestAction(action: LifecycleAction): void {
  pendingAction.value = action;
  confirmChangeSummary.value = '';
}

function cancelAction(): void {
  pendingAction.value = null;
  confirmChangeSummary.value = '';
}

async function confirmAction(): Promise<void> {
  if (!pendingAction.value) {
    return;
  }

  const action = pendingAction.value;
  const success = await runLifecycleAction(
    action,
    props.concurrencyToken,
    lifecycleActionRequiresChangeSummary(action) ? confirmChangeSummary.value.trim() : undefined,
  );

  if (success) {
    if (action === 'delete') {
      emit('deleted');
    }
    cancelAction();
  }
}

defineExpose({
  requestDelete: () => requestAction('delete'),
});
</script>

<template>
  <div class="lifecycle-actions">
    <AppButton
      v-for="action in visibleActions"
      :key="action"
      variant="secondary"
      type="button"
      :disabled="disabled || actionLoading"
      @click="requestAction(action)"
    >
      {{ LIFECYCLE_ACTION_LABELS[action] }}
    </AppButton>

    <ConfirmActionPanel
      v-if="pendingConfirmation"
      v-model:change-summary="confirmChangeSummary"
      :title="pendingConfirmation.title"
      :message="pendingConfirmation.message"
      :confirm-label="pendingConfirmation.destructive ? 'Confirm destructive action' : 'Confirm'"
      :require-change-summary="pendingConfirmation.requireChangeSummary"
      :loading="actionLoading"
      @confirm="confirmAction"
      @cancel="cancelAction"
    />
  </div>
</template>
