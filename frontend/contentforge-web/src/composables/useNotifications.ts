import { storeToRefs } from 'pinia';
import { useNotificationStore } from '@/stores/notificationStore';

export function useNotifications() {
  const notifications = useNotificationStore();
  const { items } = storeToRefs(notifications);

  return {
    notifySuccess: notifications.notifySuccess,
    notifyError: notifications.notifyError,
    notifyWarning: notifications.notifyWarning,
    notifyInfo: notifications.notifyInfo,
    dismiss: notifications.dismiss,
    clear: notifications.clear,
    items,
  };
}
