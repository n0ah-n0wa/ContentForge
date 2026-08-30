export interface AppNavItem {
  label: string;
  name: string;
  permissions?: string[];
}

export const ROUTE_PERMISSIONS = {
  content: ['content.read'],
  contentTypes: ['contentType.read'],
  users: ['user.read'],
  audit: ['audit.read'],
} as const satisfies Record<string, readonly string[]>;

export const primaryNavItems: AppNavItem[] = [
  { label: 'Dashboard', name: 'dashboard' },
  { label: 'Content', name: 'content', permissions: [...ROUTE_PERMISSIONS.content] },
  { label: 'Content Types', name: 'content-types', permissions: [...ROUTE_PERMISSIONS.contentTypes] },
  { label: 'Media', name: 'media' },
  { label: 'Users', name: 'users', permissions: [...ROUTE_PERMISSIONS.users] },
  { label: 'Audit Log', name: 'audit', permissions: [...ROUTE_PERMISSIONS.audit] },
  { label: 'Settings', name: 'settings' },
  { label: 'Profile', name: 'profile' },
];
