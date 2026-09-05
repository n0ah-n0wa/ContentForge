import { env } from '@/config/env';
import { ensureApiSession, notifyUnauthorizedSession } from '@/api/authSession';
import { ApiError } from '@/api/errors';
import { beginApiLoading, endApiLoading } from '@/api/loading';

export interface ApiRequestOptions extends Omit<RequestInit, 'body'> {
  body?: unknown;
  auth?: boolean;
  skipGlobalLoading?: boolean;
}

type AccessTokenProvider = () => string | null;
type RefreshHandler = () => Promise<boolean>;

let accessTokenProvider: AccessTokenProvider = () => null;
let refreshHandler: RefreshHandler = async () => false;

export function setAccessTokenProvider(provider: AccessTokenProvider): void {
  accessTokenProvider = provider;
}

export function setRefreshHandler(handler: RefreshHandler): void {
  refreshHandler = handler;
}

export function getRefreshHandler(): RefreshHandler {
  return refreshHandler;
}

export function getAccessToken(): string | null {
  return accessTokenProvider();
}

export async function apiRequest<TResponse>(
  path: string,
  options: ApiRequestOptions = {},
): Promise<TResponse> {
  const { auth = true, skipGlobalLoading = false, body, headers, ...init } = options;

  if (!skipGlobalLoading) {
    beginApiLoading();
  }

  try {
    return await executeRequest<TResponse>(path, { auth, body, headers, ...init });
  } finally {
    if (!skipGlobalLoading) {
      endApiLoading();
    }
  }
}

async function executeRequest<TResponse>(
  path: string,
  options: ApiRequestOptions,
  hasRetried = false,
): Promise<TResponse> {
  if (options.auth !== false) {
    const sessionIsValid = await ensureApiSession();
    if (!sessionIsValid) {
      notifyUnauthorizedSession('expired');
      throw await ApiError.fromResponse(
        new Response(
          JSON.stringify({
            title: 'Unauthorized',
            status: 401,
            detail: 'Your session has expired. Sign in again.',
          }),
          {
            status: 401,
            headers: { 'Content-Type': 'application/problem+json' },
          },
        ),
      );
    }
  }

  const response = await fetch(buildUrl(path), buildRequestInit(options));

  if (response.status === 401 && options.auth !== false && !hasRetried) {
    const refreshed = await refreshHandler();
    if (refreshed) {
      return executeRequest<TResponse>(path, options, true);
    }

    notifyUnauthorizedSession('invalid');
    throw await ApiError.fromResponse(response);
  }

  if (response.status === 403 && options.auth !== false) {
    throw await ApiError.fromResponse(response);
  }

  if (!response.ok) {
    throw await ApiError.fromResponse(response);
  }

  if (response.status === 204) {
    return undefined as TResponse;
  }

  return (await response.json()) as TResponse;
}

function buildUrl(path: string): string {
  if (path.startsWith('http://') || path.startsWith('https://')) {
    return path;
  }

  const baseUrl = env.apiBaseUrl.replace(/\/$/, '');
  const normalizedPath = path.startsWith('/') ? path : `/${path}`;
  return `${baseUrl}${normalizedPath}`;
}

function buildRequestInit(options: ApiRequestOptions): RequestInit {
  const headers = new Headers(options.headers);
  if (!headers.has('Accept')) {
    headers.set('Accept', 'application/json');
  }

  if (options.body !== undefined && !(options.body instanceof FormData)) {
    headers.set('Content-Type', 'application/json');
  }

  if (options.auth !== false) {
    const accessToken = accessTokenProvider();
    if (accessToken) {
      headers.set('Authorization', `Bearer ${accessToken}`);
    }
  }

  return {
    ...options,
    headers,
    body: serializeBody(options.body),
  };
}

function serializeBody(body: unknown): BodyInit | undefined {
  if (body === undefined || body === null) {
    return undefined;
  }

  if (body instanceof FormData || typeof body === 'string') {
    return body;
  }

  return JSON.stringify(body);
}
