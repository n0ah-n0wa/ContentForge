const ACCESS_TOKEN_KEY = 'contentforge.accessToken';
const REFRESH_TOKEN_KEY = 'contentforge.refreshToken';
const ACCESS_TOKEN_EXPIRES_AT_KEY = 'contentforge.accessTokenExpiresAt';

export interface StoredTokens {
  accessToken: string | null;
  refreshToken: string | null;
  accessTokenExpiresAt: string | null;
}

export function readStoredTokens(): StoredTokens {
  return {
    accessToken: sessionStorage.getItem(ACCESS_TOKEN_KEY),
    refreshToken: sessionStorage.getItem(REFRESH_TOKEN_KEY),
    accessTokenExpiresAt: sessionStorage.getItem(ACCESS_TOKEN_EXPIRES_AT_KEY),
  };
}

export function writeStoredTokens(tokens: StoredTokens): void {
  setOrRemove(ACCESS_TOKEN_KEY, tokens.accessToken);
  setOrRemove(REFRESH_TOKEN_KEY, tokens.refreshToken);
  setOrRemove(ACCESS_TOKEN_EXPIRES_AT_KEY, tokens.accessTokenExpiresAt);
}

export function clearStoredTokens(): void {
  sessionStorage.removeItem(ACCESS_TOKEN_KEY);
  sessionStorage.removeItem(REFRESH_TOKEN_KEY);
  sessionStorage.removeItem(ACCESS_TOKEN_EXPIRES_AT_KEY);
}

function setOrRemove(key: string, value: string | null): void {
  if (value) {
    sessionStorage.setItem(key, value);
    return;
  }

  sessionStorage.removeItem(key);
}
