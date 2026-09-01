import { ContentStatus } from '@/types/contentEntries';

export type LifecycleAction =
  'submit' | 'withdraw' | 'publish' | 'unpublish' | 'archive' | 'restore' | 'delete';

export const LIFECYCLE_ACTION_LABELS: Record<LifecycleAction, string> = {
  submit: 'Submit for review',
  withdraw: 'Withdraw from review',
  publish: 'Publish',
  unpublish: 'Unpublish',
  archive: 'Archive',
  restore: 'Restore to draft',
  delete: 'Delete',
};

export function getAvailableLifecycleActions(status: ContentStatus): LifecycleAction[] {
  switch (status) {
    case ContentStatus.Draft:
      return ['submit'];
    case ContentStatus.InReview:
      return ['withdraw', 'publish'];
    case ContentStatus.Published:
      return ['unpublish', 'archive'];
    case ContentStatus.Archived:
      return ['restore'];
    default:
      return [];
  }
}

export function isDestructiveLifecycleAction(action: LifecycleAction): boolean {
  return action === 'delete' || action === 'unpublish' || action === 'archive';
}

export function lifecycleActionRequiresChangeSummary(action: LifecycleAction): boolean {
  return (
    action === 'publish' || action === 'unpublish' || action === 'archive' || action === 'restore'
  );
}

export function getLifecycleConfirmationMessage(action: LifecycleAction, slug: string): string {
  switch (action) {
    case 'delete':
      return `Delete "${slug}"? This soft-deletes the entry and removes it from normal lists.`;
    case 'unpublish':
      return `Unpublish "${slug}"? The public snapshot will no longer be served after this action completes.`;
    case 'archive':
      return `Archive "${slug}"? Archived entries are removed from active workflows and lose their published snapshot.`;
    case 'restore':
      return `Restore "${slug}" to draft? The entry will become editable again.`;
    case 'publish':
      return `Publish "${slug}"? The current draft will become the live published version.`;
    case 'submit':
      return `Submit "${slug}" for review? Editors with review permission can publish it afterward.`;
    case 'withdraw':
      return `Withdraw "${slug}" from review and return it to draft?`;
    default:
      return `Apply "${LIFECYCLE_ACTION_LABELS[action]}" to "${slug}"?`;
  }
}
