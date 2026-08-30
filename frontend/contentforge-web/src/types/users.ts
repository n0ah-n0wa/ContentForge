import type { PaginatedResponse } from '@/types/api';

/** Role names returned by the users API. */
export type UserRoleName = 'Administrator' | 'Editor' | 'Author' | 'Viewer';

export interface UserAccount {
  id: string;
  email: string;
  displayName: string;
  isActive: boolean;
  role: UserRoleName;
  createdAt: string;
  updatedAt: string;
  lastLoginAt: string | null;
}

export interface RoleDefinition {
  name: UserRoleName;
  permissions: string[];
}

export interface UserListParams {
  page?: number;
  pageSize?: number;
  sortBy?: 'email' | 'displayName' | 'createdAt' | 'lastLoginAt';
  sortDirection?: 'asc' | 'desc';
  isActive?: boolean;
  role?: UserRoleName;
  search?: string;
}

export interface CreateUserRequest {
  email: string;
  displayName: string;
  password: string;
  role: UserRoleName;
}

export interface UpdateUserRequest {
  displayName: string;
  role: UserRoleName;
}

export type UserListResponse = PaginatedResponse<UserAccount>;

export const USER_ROLE_OPTIONS: { value: UserRoleName; label: string }[] = [
  { value: 'Administrator', label: 'Administrator' },
  { value: 'Editor', label: 'Editor' },
  { value: 'Author', label: 'Author' },
  { value: 'Viewer', label: 'Viewer' },
];

export function getUserRoleLabel(role: UserRoleName): string {
  return USER_ROLE_OPTIONS.find((option) => option.value === role)?.label ?? role;
}

export function formatUserTimestamp(value: string | null): string {
  if (!value) {
    return 'Never';
  }

  return new Date(value).toLocaleString();
}
