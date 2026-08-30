import { computed } from 'vue';
import { useAuthStore } from '@/stores/authStore';

export const CONTENT_PERMISSIONS = {
  read: 'content.read',
  create: 'content.create',
  update: 'content.update',
  delete: 'content.delete',
  publish: 'content.publish',
  archive: 'content.archive',
  restore: 'content.restore',
  review: 'content.review',
  versionRead: 'content.version.read',
  versionRestore: 'content.version.restore',
} as const;

export function useContentPermissions() {
  const authStore = useAuthStore();

  const canRead = computed(() => authStore.hasPermission(CONTENT_PERMISSIONS.read));
  const canCreate = computed(() => authStore.hasPermission(CONTENT_PERMISSIONS.create));
  const canUpdate = computed(() => authStore.hasPermission(CONTENT_PERMISSIONS.update));
  const canDelete = computed(() => authStore.hasPermission(CONTENT_PERMISSIONS.delete));
  const canPublish = computed(() => authStore.hasPermission(CONTENT_PERMISSIONS.publish));
  const canArchive = computed(() => authStore.hasPermission(CONTENT_PERMISSIONS.archive));
  const canRestore = computed(() => authStore.hasPermission(CONTENT_PERMISSIONS.restore));
  const canReview = computed(() => authStore.hasPermission(CONTENT_PERMISSIONS.review));
  const canReadVersions = computed(() => authStore.hasPermission(CONTENT_PERMISSIONS.versionRead));
  const canRestoreVersions = computed(() =>
    authStore.hasPermission(CONTENT_PERMISSIONS.versionRestore),
  );

  return {
    canRead,
    canCreate,
    canUpdate,
    canDelete,
    canPublish,
    canArchive,
    canRestore,
    canReview,
    canReadVersions,
    canRestoreVersions,
  };
}
