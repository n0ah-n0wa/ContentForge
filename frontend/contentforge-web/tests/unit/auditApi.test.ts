import { beforeEach, describe, expect, it, vi } from 'vitest';
import * as auditApi from '@/api/audit';

vi.mock('@/api/client', () => ({
  apiRequest: vi.fn(),
}));

import { apiRequest } from '@/api/client';

describe('audit API', () => {
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

    await auditApi.listAuditLogs({
      page: 2,
      pageSize: 25,
      sortBy: 'timestamp',
      sortDirection: 'desc',
      userId: '11111111-1111-1111-1111-111111111111',
      action: 'ContentPublished',
      entityType: 'ContentEntry',
      entityId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
      from: '2026-01-01T00:00:00.000Z',
      to: '2026-01-31T23:59:59.000Z',
    });

    const url = vi.mocked(apiRequest).mock.calls[0]?.[0] as string;
    expect(url).toContain('page=2');
    expect(url).toContain('userId=11111111-1111-1111-1111-111111111111');
    expect(url).toContain('action=ContentPublished');
    expect(url).toContain('entityType=ContentEntry');
    expect(url).toContain('entityId=aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa');
    expect(url).toContain('from=2026-01-01');
    expect(url).toContain('to=2026-01-31');
  });

  it('loads a single audit event by id', async () => {
    vi.mocked(apiRequest).mockResolvedValue({
      id: 'cccccccc-cccc-cccc-cccc-cccccccccccc',
      timestamp: '2026-01-01T00:00:00.000Z',
      userId: '11111111-1111-1111-1111-111111111111',
      action: 9,
      entityType: 'ContentEntry',
      entityId: 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
      metadata: null,
      ipAddress: '127.0.0.1',
      userAgent: 'test-agent',
      correlationId: 'corr-1',
    });

    await auditApi.getAuditLog('cccccccc-cccc-cccc-cccc-cccccccccccc');

    expect(apiRequest).toHaveBeenCalledWith('/api/v1/audit/cccccccc-cccc-cccc-cccc-cccccccccccc');
  });
});
