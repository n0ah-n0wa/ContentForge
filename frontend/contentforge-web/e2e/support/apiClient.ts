import { e2eEnv } from './env';

export interface ApiResponse<T = unknown> {
  status: number;
  ok: boolean;
  body: T | null;
  text: string;
}

export class ApiClient {
  private readonly baseUrl: string;
  private accessToken: string | null;

  constructor(baseUrl: string = e2eEnv.apiBaseURL, accessToken: string | null = null) {
    this.baseUrl = baseUrl;
    this.accessToken = accessToken;
  }

  withToken(token: string | null): ApiClient {
    return new ApiClient(this.baseUrl, token);
  }

  async login(
    email: string,
    password: string,
  ): Promise<{ accessToken: string; refreshToken: string }> {
    const response = await this.request<{ accessToken: string; refreshToken: string }>(
      'POST',
      '/api/v1/auth/login',
      { email, password },
      { auth: false },
    );
    this.accessToken = response.accessToken;
    return response;
  }

  async request<T>(
    method: string,
    path: string,
    body?: unknown,
    options: { auth?: boolean; formData?: FormData } = {},
  ): Promise<T> {
    const response = await this.requestRaw<T>(method, path, body, options);
    if (!response.ok) {
      throw new Error(`${method} ${path} failed (${response.status}): ${response.text}`);
    }
    return response.body as T;
  }

  /** Non-throwing request for assertions on status codes (e.g. public API 404). */
  async requestRaw<T = unknown>(
    method: string,
    path: string,
    body?: unknown,
    options: { auth?: boolean; formData?: FormData } = {},
  ): Promise<ApiResponse<T>> {
    const headers: Record<string, string> = {};
    if (options.auth !== false && this.accessToken) {
      headers.Authorization = `Bearer ${this.accessToken}`;
    }

    let requestBody: BodyInit | undefined;
    if (options.formData) {
      requestBody = options.formData;
    } else if (body !== undefined) {
      headers['Content-Type'] = 'application/json';
      requestBody = JSON.stringify(body);
    }

    const response = await fetch(`${this.baseUrl}${path}`, {
      method,
      headers,
      body: requestBody,
    });

    const text = await response.text();
    let parsed: T | null = null;
    const contentType = response.headers.get('content-type') ?? '';
    if (text && contentType.includes('application/json')) {
      parsed = JSON.parse(text) as T;
    }

    return {
      status: response.status,
      ok: response.ok,
      body: parsed,
      text,
    };
  }
}
