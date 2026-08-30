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
