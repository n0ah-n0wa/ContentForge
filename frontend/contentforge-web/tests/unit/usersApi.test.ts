import { beforeEach, describe, expect, it, vi } from 'vitest';
import * as usersApi from '@/api/users';

vi.mock('@/api/client', () => ({
  apiRequest: vi.fn(),
}));

import { apiRequest } from '@/api/client';

describe('users API', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it('builds list query parameters', async () => {
    vi.mocked(apiRequest).mockResolvedValue({
      items: [],
      page: 1,
      pageSize: 25,
      totalItems: 0,
      totalPages: 1,
    });

    await usersApi.listUsers({
      page: 2,
      pageSize: 25,
      sortBy: 'email',
      sortDirection: 'asc',
      isActive: true,
      role: 'Editor',
      search: 'admin',
    });

    const url = vi.mocked(apiRequest).mock.calls[0]?.[0] as string;
    expect(url).toContain('page=2');
    expect(url).toContain('isActive=true');
    expect(url).toContain('role=Editor');
    expect(url).toContain('search=admin');
  });

  it('calls enable and disable endpoints', async () => {
    vi.mocked(apiRequest).mockResolvedValue({
      id: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
      email: 'user@example.com',
      displayName: 'User',
      isActive: false,
      role: 'Viewer',
      createdAt: '2026-01-01T00:00:00.000Z',
      updatedAt: '2026-01-01T00:00:00.000Z',
      lastLoginAt: null,
    });

    await usersApi.disableUser('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa');
    await usersApi.enableUser('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa');

    expect(apiRequest).toHaveBeenNthCalledWith(
      1,
      '/api/v1/users/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/disable',
      { method: 'POST' },
    );
    expect(apiRequest).toHaveBeenNthCalledWith(
      2,
      '/api/v1/users/aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa/enable',
      { method: 'POST' },
    );
  });
});
