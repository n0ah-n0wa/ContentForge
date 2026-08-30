import { computed } from 'vue';
import { useAuthStore } from '@/stores/authStore';

export const CONTENT_TYPE_PERMISSIONS = {
  read: 'contentType.read',
  create: 'contentType.create',
  update: 'contentType.update',
  delete: 'contentType.delete',
} as const;

export function useContentTypePermissions() {
  const authStore = useAuthStore();

  const canRead = computed(() => authStore.hasPermission(CONTENT_TYPE_PERMISSIONS.read));
  const canCreate = computed(() => authStore.hasPermission(CONTENT_TYPE_PERMISSIONS.create));
  const canUpdate = computed(() => authStore.hasPermission(CONTENT_TYPE_PERMISSIONS.update));
  const canDelete = computed(() => authStore.hasPermission(CONTENT_TYPE_PERMISSIONS.delete));

  return {
    canRead,
    canCreate,
    canUpdate,
    canDelete,
  };
}
