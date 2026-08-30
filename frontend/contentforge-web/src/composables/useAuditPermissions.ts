import { computed } from 'vue';
import { useAuthStore } from '@/stores/authStore';

export const AUDIT_PERMISSIONS = {
  read: 'audit.read',
} as const;

export function useAuditPermissions() {
  const authStore = useAuthStore();

  const canRead = computed(() => authStore.hasPermission(AUDIT_PERMISSIONS.read));

  return {
    canRead,
  };
}
