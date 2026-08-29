import { describe, expect, it } from 'vitest';
import { useAppStore } from '@/stores/appStore';
import { createPinia, setActivePinia } from 'pinia';

describe('appStore', () => {
  it('exposes the application title', () => {
    setActivePinia(createPinia());
    const store = useAppStore();

    expect(store.title).toBe('ContentForge Admin');
  });
});
