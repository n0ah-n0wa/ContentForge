import { computed } from 'vue';
import { useAuthStore } from '@/stores/authStore';

export const USER_PERMISSIONS = {
  read: 'user.read',
  create: 'user.create',
  update: 'user.update',
  disable: 'user.disable',
} as const;

export function useUserPermissions() {
  const authStore = useAuthStore();

  const canRead = computed(() => authStore.hasPermission(USER_PERMISSIONS.read));
  const canCreate = computed(() => authStore.hasPermission(USER_PERMISSIONS.create));
  const canUpdate = computed(() => authStore.hasPermission(USER_PERMISSIONS.update));
  const canDisable = computed(() => authStore.hasPermission(USER_PERMISSIONS.disable));

  return {
    canRead,
    canCreate,
    canUpdate,
    canDisable,
  };
}
