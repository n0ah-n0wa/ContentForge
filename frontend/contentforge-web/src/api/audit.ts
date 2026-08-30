import { apiRequest } from '@/api/client';
import type { AuditLogEntry, AuditLogListParams, AuditLogListResponse } from '@/types/audit';

const BASE = '/api/v1/audit';

export function listAuditLogs(params: AuditLogListParams = {}): Promise<AuditLogListResponse> {
  const query = new URLSearchParams();
  if (params.page) query.set('page', String(params.page));
  if (params.pageSize) query.set('pageSize', String(params.pageSize));
  if (params.sortBy) query.set('sortBy', params.sortBy);
  if (params.sortDirection) query.set('sortDirection', params.sortDirection);
  if (params.userId) query.set('userId', params.userId);
  if (params.action) query.set('action', params.action);
  if (params.entityType) query.set('entityType', params.entityType);
  if (params.entityId) query.set('entityId', params.entityId);
  if (params.from) query.set('from', params.from);
  if (params.to) query.set('to', params.to);

  const suffix = query.size > 0 ? `?${query.toString()}` : '';
  return apiRequest<AuditLogListResponse>(`${BASE}${suffix}`);
}

export function getAuditLog(id: string): Promise<AuditLogEntry> {
  return apiRequest<AuditLogEntry>(`${BASE}/${id}`);
}
