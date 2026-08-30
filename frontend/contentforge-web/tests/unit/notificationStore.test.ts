import { describe, expect, it } from 'vitest';
import { createPinia, setActivePinia } from 'pinia';
import { useNotificationStore } from '@/stores/notificationStore';

describe('notificationStore', () => {
  it('queues and dismisses notifications', () => {
    setActivePinia(createPinia());
    const store = useNotificationStore();

    store.notifyError('Save failed', 'Try again.');
    const id = store.items[0]?.id;
    expect(store.items).toHaveLength(1);
    expect(store.items[0]?.title).toBe('Save failed');

    if (id) {
      store.dismiss(id);
    }

    expect(store.items).toHaveLength(0);
  });
});
