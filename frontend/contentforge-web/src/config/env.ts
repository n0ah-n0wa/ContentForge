const DEFAULT_API_BASE_URL = '';

export interface AppEnvironment {
  apiBaseUrl: string;
  appName: string;
}

export function loadEnvironment(): AppEnvironment {
  const apiBaseUrl = import.meta.env.VITE_API_BASE_URL?.trim() ?? DEFAULT_API_BASE_URL;

  return {
    apiBaseUrl,
    appName: 'ContentForge',
  };
}

export const env = loadEnvironment();
