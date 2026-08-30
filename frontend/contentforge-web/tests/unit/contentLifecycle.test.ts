import { describe, expect, it } from 'vitest';
import { ContentStatus } from '@/types/contentEntries';
import {
  getAvailableLifecycleActions,
  getLifecycleConfirmationMessage,
  isDestructiveLifecycleAction,
  lifecycleActionRequiresChangeSummary,
} from '@/utils/contentLifecycle';

describe('contentLifecycle', () => {
  it('derives available actions from entry status', () => {
    expect(getAvailableLifecycleActions(ContentStatus.Draft)).toEqual(['submit']);
    expect(getAvailableLifecycleActions(ContentStatus.InReview)).toEqual(['withdraw', 'publish']);
    expect(getAvailableLifecycleActions(ContentStatus.Published)).toEqual(['unpublish', 'archive']);
    expect(getAvailableLifecycleActions(ContentStatus.Archived)).toEqual(['restore']);
  });

  it('marks destructive actions and change-summary requirements', () => {
    expect(isDestructiveLifecycleAction('archive')).toBe(true);
    expect(isDestructiveLifecycleAction('submit')).toBe(false);
    expect(lifecycleActionRequiresChangeSummary('publish')).toBe(true);
    expect(lifecycleActionRequiresChangeSummary('submit')).toBe(false);
  });

  it('provides confirmation copy for destructive operations', () => {
    expect(getLifecycleConfirmationMessage('delete', 'hello')).toContain('Delete "hello"');
    expect(getLifecycleConfirmationMessage('archive', 'hello')).toContain('Archive "hello"');
  });
});
