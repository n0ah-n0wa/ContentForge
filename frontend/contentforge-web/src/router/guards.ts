import type { NavigationGuard } from 'vue-router';
import { useAuthStore } from '@/stores/authStore';

export const authGuard: NavigationGuard = async (to) => {
  const authStore = useAuthStore();

  if (!authStore.isInitialized) {
    await authStore.initialize();
  }

  if (to.meta.guestOnly && authStore.isAuthenticated) {
    return { name: 'dashboard' };
  }

  if (to.meta.publicPreview) {
    return true;
  }

  if (to.meta.requiresAuth && !authStore.isAuthenticated) {
    const reason = authStore.sessionStatus === 'expired' ? 'expired' : undefined;
    return {
      name: 'login',
      query: {
        redirect: to.fullPath,
        ...(reason ? { reason } : {}),
      },
    };
  }

  return true;
};

export const permissionGuard: NavigationGuard = (to) => {
  const permissions = to.meta.permissions;

  if (!permissions?.length || to.meta.guestOnly || to.meta.publicPreview) {
    return true;
  }

  const authStore = useAuthStore();

  if (!authStore.canAccess(permissions)) {
    return { name: 'access-denied' };
  }

  return true;
};
