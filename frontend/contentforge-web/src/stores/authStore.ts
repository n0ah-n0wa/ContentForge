import * as authApi from '@/api/auth';
import { ApiError, getValidationMessages, isValidationProblem } from '@/api/errors';
import type { AuthenticatedUser, LoginResult } from '@/types/auth';
import {
  extractPermissionsFromToken,
  isAccessTokenExpired,
} from '@/utils/jwt';
import { clearStoredTokens, readStoredTokens, writeStoredTokens } from '@/utils/tokenStorage';
import { defineStore } from 'pinia';
import { computed, ref } from 'vue';

export type SessionStatus = 'anonymous' | 'authenticated' | 'expired';

export const useAuthStore = defineStore('auth', () => {
  const user = ref<AuthenticatedUser | null>(null);
  const permissions = ref<string[]>([]);
  const accessToken = ref<string | null>(readStoredTokens().accessToken);
  const refreshToken = ref<string | null>(readStoredTokens().refreshToken);
  const accessTokenExpiresAt = ref<string | null>(readStoredTokens().accessTokenExpiresAt);
  const isInitialized = ref(false);
  const isAuthenticating = ref(false);
  const sessionStatus = ref<SessionStatus>('anonymous');

  const isAuthenticated = computed(() => sessionStatus.value === 'authenticated');

  function syncPermissionsFromToken(token: string | null): void {
    permissions.value = extractPermissionsFromToken(token);
  }

  function applyLoginResult(result: LoginResult): void {
    accessToken.value = result.accessToken;
    refreshToken.value = result.refreshToken;
    accessTokenExpiresAt.value = result.accessTokenExpiresAt;
    user.value = {
      id: result.userId,
      email: result.email,
      displayName: result.displayName,
      role: result.role,
    };
    syncPermissionsFromToken(result.accessToken);
    sessionStatus.value = 'authenticated';

    writeStoredTokens({
      accessToken: accessToken.value,
      refreshToken: refreshToken.value,
      accessTokenExpiresAt: accessTokenExpiresAt.value,
    });
  }

  function clearSession(reason: SessionStatus = 'anonymous'): void {
    user.value = null;
    permissions.value = [];
    accessToken.value = null;
    refreshToken.value = null;
    accessTokenExpiresAt.value = null;
    sessionStatus.value = reason;
    clearStoredTokens();
  }

  async function initialize(): Promise<void> {
    if (isInitialized.value) {
      return;
    }

    if (!accessToken.value) {
      sessionStatus.value = 'anonymous';
      isInitialized.value = true;
      return;
    }

    syncPermissionsFromToken(accessToken.value);

    if (isAccessTokenExpired(accessTokenExpiresAt.value) && refreshToken.value) {
      const refreshed = await refreshSession();
      if (!refreshed) {
        isInitialized.value = true;
        return;
      }
    }

    try {
      user.value = await authApi.getCurrentUser();
      sessionStatus.value = 'authenticated';
    } catch {
      clearSession('expired');
    } finally {
      isInitialized.value = true;
    }
  }

  async function login(email: string, password: string): Promise<void> {
    isAuthenticating.value = true;
    try {
      const result = await authApi.login({ email, password });
      applyLoginResult(result);
    } finally {
      isAuthenticating.value = false;
    }
  }

  async function refreshSession(): Promise<boolean> {
    if (!refreshToken.value) {
      clearSession('expired');
      return false;
    }

    try {
      const result = await authApi.refresh({ refreshToken: refreshToken.value });
      applyLoginResult(result);
      return true;
    } catch {
      clearSession('expired');
      return false;
    }
  }

  async function ensureSession(): Promise<boolean> {
    if (!accessToken.value) {
      return false;
    }

    if (!isAccessTokenExpired(accessTokenExpiresAt.value)) {
      return sessionStatus.value === 'authenticated' && user.value !== null;
    }

    return refreshSession();
  }

  async function logout(): Promise<void> {
    const tokenToInvalidate = refreshToken.value;
    try {
      if (accessToken.value) {
        await authApi.logout({ refreshToken: tokenToInvalidate });
      }
    } finally {
      clearSession();
    }
  }

  function hasPermission(permission: string): boolean {
    return permissions.value.includes(permission);
  }

  function hasPermissions(requiredPermissions: string[]): boolean {
    if (requiredPermissions.length === 0) {
      return isAuthenticated.value;
    }

    return requiredPermissions.every((permission) => hasPermission(permission));
  }

  function canAccess(requiredPermissions?: string[]): boolean {
    if (!isAuthenticated.value) {
      return false;
    }

    if (!requiredPermissions?.length) {
      return true;
    }

    return hasPermissions(requiredPermissions);
  }

  function getAccessToken(): string | null {
    return accessToken.value;
  }

  return {
    user,
    permissions,
    accessToken,
    refreshToken,
    accessTokenExpiresAt,
    isInitialized,
    isAuthenticating,
    sessionStatus,
    isAuthenticated,
    initialize,
    login,
    logout,
    refreshSession,
    ensureSession,
    hasPermission,
    hasPermissions,
    canAccess,
    getAccessToken,
    applyLoginResult,
    clearSession,
  };
});

export function describeAuthError(error: unknown): string {
  if (error instanceof ApiError) {
    if (error.isValidationError && isValidationProblem(error.problem)) {
      const messages = getValidationMessages(error.problem);
      if (messages.length > 0) {
        return messages.join(' ');
      }
    }

    if (error.isUnauthorized) {
      return 'Invalid email or password.';
    }

    return error.message;
  }

  if (error instanceof Error) {
    return error.message;
  }

  return 'An unexpected error occurred.';
}
