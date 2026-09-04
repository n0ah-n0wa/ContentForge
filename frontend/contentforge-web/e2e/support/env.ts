/** Shared E2E credentials and environment endpoints. */

export const e2eEnv = {
  baseURL: process.env.E2E_BASE_URL ?? 'http://localhost:28080',
  apiBaseURL: process.env.E2E_API_BASE_URL ?? 'http://localhost:25080',
} as const;

/** Development seed administrator (docker-compose.e2e.yml). */
export const seededAdmin = {
  email: 'admin@contentforge.local',
  password: 'AdminPassword123!',
} as const;

export const rolePasswords = {
  Administrator: 'AdminPassword123!',
  Editor: 'EditorPassword123!',
  Author: 'AuthorPassword123!',
  Viewer: 'ViewerPassword123!',
  Disabled: 'DisabledPassword123!',
} as const;

export type ManagedRole = keyof typeof rolePasswords;
