import { defineStore } from 'pinia';
import { ref } from 'vue';

export type NotificationKind = 'info' | 'success' | 'warning' | 'error';

export interface AppNotification {
  id: string;
  kind: NotificationKind;
  title: string;
  message?: string;
  timeoutMs: number;
}

let notificationSequence = 0;

export const useNotificationStore = defineStore('notifications', () => {
  const items = ref<AppNotification[]>([]);

  function push(kind: NotificationKind, title: string, message?: string, timeoutMs = 6000): string {
    const id = `notification-${++notificationSequence}`;
    items.value = [...items.value, { id, kind, title, message, timeoutMs }];
    return id;
  }

  function dismiss(id: string): void {
    items.value = items.value.filter((item) => item.id !== id);
  }

  function clear(): void {
    items.value = [];
  }

  function notifySuccess(title: string, message?: string): void {
    push('success', title, message);
  }

  function notifyError(title: string, message?: string): void {
    push('error', title, message, 8000);
  }

  function notifyWarning(title: string, message?: string): void {
    push('warning', title, message);
  }

  function notifyInfo(title: string, message?: string): void {
    push('info', title, message);
  }

  return {
    items,
    push,
    dismiss,
    clear,
    notifySuccess,
    notifyError,
    notifyWarning,
    notifyInfo,
  };
});
