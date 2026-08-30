export interface AppNavItem {
  label: string;
  name: string;
  permissions?: string[];
}

export const ROUTE_PERMISSIONS = {
  users: ['user.read'],
  audit: ['audit.read'],
} as const satisfies Record<string, readonly string[]>;

export const primaryNavItems: AppNavItem[] = [
  { label: 'Dashboard', name: 'dashboard' },
  { label: 'Content', name: 'content' },
  { label: 'Content Types', name: 'content-types' },
  { label: 'Media', name: 'media' },
  { label: 'Users', name: 'users', permissions: [...ROUTE_PERMISSIONS.users] },
  { label: 'Audit Log', name: 'audit', permissions: [...ROUTE_PERMISSIONS.audit] },
  { label: 'Settings', name: 'settings' },
  { label: 'Profile', name: 'profile' },
];
