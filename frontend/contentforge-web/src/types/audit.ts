import type { PaginatedResponse } from '@/types/api';

/** Audit action values returned by the API (numeric enum serialization). */
export type AuditAction =
  | 0
  | 1
  | 2
  | 3
  | 4
  | 5
  | 6
  | 7
  | 8
  | 9
  | 10
  | 11
  | 12
  | 13
  | 14
  | 15
  | 16
  | 17
  | 18;

export interface AuditLogEntry {
  id: string;
  timestamp: string;
  userId: string | null;
  action: AuditAction;
  entityType: string;
  entityId: string;
  metadata: string | null;
  ipAddress: string | null;
  userAgent: string | null;
  correlationId: string | null;
}

export interface AuditLogListParams {
  page?: number;
  pageSize?: number;
  sortBy?: 'timestamp' | 'action' | 'entityType';
  sortDirection?: 'asc' | 'desc';
  userId?: string;
  action?: AuditActionName;
  entityType?: string;
  entityId?: string;
  from?: string;
  to?: string;
}

export type AuditLogListResponse = PaginatedResponse<AuditLogEntry>;

/** Action names accepted by the audit list API filter. */
export type AuditActionName =
  | 'UserCreated'
  | 'UserDisabled'
  | 'UserRoleChanged'
  | 'LoginSucceeded'
  | 'LoginFailed'
  | 'ContentCreated'
  | 'ContentUpdated'
  | 'ContentSubmittedForReview'
  | 'ContentWithdrawnFromReview'
  | 'ContentPublished'
  | 'ContentUnpublished'
  | 'ContentArchived'
  | 'ContentRestored'
  | 'ContentDeleted'
  | 'ContentTypeCreated'
  | 'ContentTypeUpdated'
  | 'MediaUploaded'
  | 'MediaDeleted'
  | 'UserEnabled';

export const AUDIT_ACTION_LABELS: Record<AuditAction, string> = {
  0: 'User created',
  1: 'User disabled',
  2: 'User role changed',
  3: 'Login succeeded',
  4: 'Login failed',
  5: 'Content created',
  6: 'Content updated',
  7: 'Content submitted for review',
  8: 'Content withdrawn from review',
  9: 'Content published',
  10: 'Content unpublished',
  11: 'Content archived',
  12: 'Content restored',
  13: 'Content deleted',
  14: 'Content type created',
  15: 'Content type updated',
  16: 'Media uploaded',
  17: 'Media deleted',
  18: 'User enabled',
};

export const AUDIT_ACTION_OPTIONS: { value: AuditActionName; label: string; action: AuditAction }[] = [
  { value: 'UserCreated', label: AUDIT_ACTION_LABELS[0], action: 0 },
  { value: 'UserDisabled', label: AUDIT_ACTION_LABELS[1], action: 1 },
  { value: 'UserRoleChanged', label: AUDIT_ACTION_LABELS[2], action: 2 },
  { value: 'LoginSucceeded', label: AUDIT_ACTION_LABELS[3], action: 3 },
  { value: 'LoginFailed', label: AUDIT_ACTION_LABELS[4], action: 4 },
  { value: 'ContentCreated', label: AUDIT_ACTION_LABELS[5], action: 5 },
  { value: 'ContentUpdated', label: AUDIT_ACTION_LABELS[6], action: 6 },
  { value: 'ContentSubmittedForReview', label: AUDIT_ACTION_LABELS[7], action: 7 },
  { value: 'ContentWithdrawnFromReview', label: AUDIT_ACTION_LABELS[8], action: 8 },
  { value: 'ContentPublished', label: AUDIT_ACTION_LABELS[9], action: 9 },
  { value: 'ContentUnpublished', label: AUDIT_ACTION_LABELS[10], action: 10 },
  { value: 'ContentArchived', label: AUDIT_ACTION_LABELS[11], action: 11 },
  { value: 'ContentRestored', label: AUDIT_ACTION_LABELS[12], action: 12 },
  { value: 'ContentDeleted', label: AUDIT_ACTION_LABELS[13], action: 13 },
  { value: 'ContentTypeCreated', label: AUDIT_ACTION_LABELS[14], action: 14 },
  { value: 'ContentTypeUpdated', label: AUDIT_ACTION_LABELS[15], action: 15 },
  { value: 'MediaUploaded', label: AUDIT_ACTION_LABELS[16], action: 16 },
  { value: 'MediaDeleted', label: AUDIT_ACTION_LABELS[17], action: 17 },
  { value: 'UserEnabled', label: AUDIT_ACTION_LABELS[18], action: 18 },
];

export const AUDIT_ENTITY_TYPE_OPTIONS = [
  { value: '', label: 'All entity types' },
  { value: 'User', label: 'User' },
  { value: 'ContentEntry', label: 'Content entry' },
  { value: 'ContentType', label: 'Content type' },
  { value: 'MediaAsset', label: 'Media asset' },
] as const;

export function getAuditActionLabel(action: AuditAction): string {
  return AUDIT_ACTION_LABELS[action] ?? `Action ${action}`;
}

export function formatAuditTimestamp(value: string): string {
  return new Date(value).toLocaleString();
}
