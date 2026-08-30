import { apiRequest } from '@/api/client';
import type {
  AuthenticatedUser,
  LoginRequest,
  LoginResult,
  LogoutRequest,
  RefreshTokenRequest,
} from '@/types/auth';

const AUTH_BASE = '/api/v1/auth';

export function login(request: LoginRequest): Promise<LoginResult> {
  return apiRequest<LoginResult>(`${AUTH_BASE}/login`, {
    method: 'POST',
    body: request,
    auth: false,
  });
}

export function refresh(request: RefreshTokenRequest): Promise<LoginResult> {
  return apiRequest<LoginResult>(`${AUTH_BASE}/refresh`, {
    method: 'POST',
    body: request,
    auth: false,
    skipGlobalLoading: true,
  });
}

export function getCurrentUser(): Promise<AuthenticatedUser> {
  return apiRequest<AuthenticatedUser>(`${AUTH_BASE}/me`, {
    method: 'GET',
    skipGlobalLoading: true,
  });
}

export function logout(request: LogoutRequest = {}): Promise<void> {
  return apiRequest<void>(`${AUTH_BASE}/logout`, {
    method: 'POST',
    body: request,
  });
}
