export interface ValidationProblemDetails extends ProblemDetails {
  errors?: Record<string, string[]>;
}

export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  traceId?: string;
  correlationId?: string;
  [key: string]: unknown;
}

export interface PaginatedResponse<TItem> {
  items: TItem[];
  page: number;
  pageSize: number;
  totalItems: number;
  totalPages: number;
}
