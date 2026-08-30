/** Role values returned by the API (numeric enum serialization). */
export type RoleName = 0 | 1 | 2 | 3;

export interface AuthenticatedUser {
  id: string;
  email: string;
  displayName: string;
  role: RoleName;
}

export interface LoginResult {
  userId: string;
  email: string;
  displayName: string;
  role: RoleName;
  accessToken: string;
  accessTokenExpiresAt: string;
  refreshToken: string | null;
  refreshTokenExpiresAt: string | null;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RefreshTokenRequest {
  refreshToken: string;
}

export interface LogoutRequest {
  refreshToken?: string | null;
}

/** Presentation labels for API role values — not authorization logic. */
export const ROLE_DISPLAY_NAMES: Record<RoleName, string> = {
  0: 'Administrator',
  1: 'Editor',
  2: 'Author',
  3: 'Viewer',
};

export function getRoleDisplayName(role: RoleName): string {
  return ROLE_DISPLAY_NAMES[role] ?? 'Unknown';
}
