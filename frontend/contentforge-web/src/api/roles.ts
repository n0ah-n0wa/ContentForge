import { apiRequest } from '@/api/client';
import type { RoleDefinition } from '@/types/users';

const BASE = '/api/v1/roles';

export function listRoles(): Promise<RoleDefinition[]> {
  return apiRequest<RoleDefinition[]>(BASE);
}
