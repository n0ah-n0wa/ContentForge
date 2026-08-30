import { computed } from 'vue';
import { useAuthStore } from '@/stores/authStore';

export const MEDIA_PERMISSIONS = {
  read: 'media.read',
  upload: 'media.upload',
  update: 'media.update',
  delete: 'media.delete',
} as const;

export function useMediaPermissions() {
  const authStore = useAuthStore();

  const canRead = computed(() => authStore.hasPermission(MEDIA_PERMISSIONS.read));
  const canUpload = computed(() => authStore.hasPermission(MEDIA_PERMISSIONS.upload));
  const canUpdate = computed(() => authStore.hasPermission(MEDIA_PERMISSIONS.update));
  const canDelete = computed(() => authStore.hasPermission(MEDIA_PERMISSIONS.delete));

  return {
    canRead,
    canUpload,
    canUpdate,
    canDelete,
  };
}
