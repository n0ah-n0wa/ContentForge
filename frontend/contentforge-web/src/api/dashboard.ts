import { apiRequest } from '@/api/client';
import type { DashboardData } from '@/types/dashboard';

const BASE = '/api/v1/dashboard';

export function getDashboard(): Promise<DashboardData> {
  return apiRequest<DashboardData>(BASE);
}
