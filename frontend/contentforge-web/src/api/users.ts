import { apiRequest } from '@/api/client';
import type {
  CreateUserRequest,
  UpdateUserRequest,
  UserAccount,
  UserListParams,
  UserListResponse,
} from '@/types/users';

const BASE = '/api/v1/users';

export function listUsers(params: UserListParams = {}): Promise<UserListResponse> {
  const query = new URLSearchParams();
  if (params.page) query.set('page', String(params.page));
  if (params.pageSize) query.set('pageSize', String(params.pageSize));
  if (params.sortBy) query.set('sortBy', params.sortBy);
  if (params.sortDirection) query.set('sortDirection', params.sortDirection);
  if (params.isActive !== undefined) query.set('isActive', String(params.isActive));
  if (params.role) query.set('role', params.role);
  if (params.search) query.set('search', params.search);

  const suffix = query.size > 0 ? `?${query.toString()}` : '';
  return apiRequest<UserListResponse>(`${BASE}${suffix}`);
}

export function getUser(id: string): Promise<UserAccount> {
  return apiRequest<UserAccount>(`${BASE}/${id}`);
}

export function createUser(request: CreateUserRequest): Promise<UserAccount> {
  return apiRequest<UserAccount>(BASE, {
    method: 'POST',
    body: request,
  });
}

export function updateUser(id: string, request: UpdateUserRequest): Promise<UserAccount> {
  return apiRequest<UserAccount>(`${BASE}/${id}`, {
    method: 'PUT',
    body: request,
  });
}

export function disableUser(id: string): Promise<UserAccount> {
  return apiRequest<UserAccount>(`${BASE}/${id}/disable`, {
    method: 'POST',
  });
}

export function enableUser(id: string): Promise<UserAccount> {
  return apiRequest<UserAccount>(`${BASE}/${id}/enable`, {
    method: 'POST',
  });
}
