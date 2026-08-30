import { ContentStatus } from '@/types/contentEntries';

export interface DashboardContentStatistics {
  totalContent: number;
  draftCount: number;
  inReviewCount: number;
  publishedCount: number;
  archivedCount: number;
}

export interface DashboardRecentContentItem {
  id: string;
  contentTypeId: string;
  contentTypeSlug: string;
  contentTypeDisplayName: string;
  slug: string;
  status: ContentStatus;
  updatedAt: string;
}

export interface DashboardRecentActivityItem {
  id: string;
  timestamp: string;
  action: number;
  entityType: string;
  entityId: string;
  userId: string | null;
}

export interface DashboardData {
  contentStatistics: DashboardContentStatistics | null;
  recentContent: DashboardRecentContentItem[];
  recentActivity: DashboardRecentActivityItem[];
}

export const DASHBOARD_STAT_CARDS = [
  { key: 'totalContent', label: 'Total content', status: null },
  { key: 'draftCount', label: 'Drafts', status: ContentStatus.Draft },
  { key: 'inReviewCount', label: 'Pending review', status: ContentStatus.InReview },
  { key: 'publishedCount', label: 'Published', status: ContentStatus.Published },
  { key: 'archivedCount', label: 'Archived', status: ContentStatus.Archived },
] as const;

export const AUDIT_ACTION_LABELS: Record<number, string> = {
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
};
