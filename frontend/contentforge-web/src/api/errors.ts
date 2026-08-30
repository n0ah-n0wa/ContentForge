import type { ProblemDetails, ValidationProblemDetails } from '@/types/api';

export class ApiError extends Error {
  readonly status: number;
  readonly problem: ProblemDetails;
  readonly validationErrors?: Record<string, string[]>;

  constructor(
    status: number,
    problem: ProblemDetails,
    validationErrors?: Record<string, string[]>,
  ) {
    super(problem.detail ?? problem.title ?? 'Request failed.');
    this.name = 'ApiError';
    this.status = status;
    this.problem = problem;
    this.validationErrors = validationErrors;
  }

  static async fromResponse(response: Response): Promise<ApiError> {
    const contentType = response.headers.get('content-type') ?? '';
    let payload: unknown = null;

    if (contentType.includes('json')) {
      payload = await response.json();
    } else {
      const text = await response.text();
      payload = text ? { detail: text } : null;
    }

    const problem = normalizeProblemDetails(payload, response.status);
    const validationErrors = isValidationProblem(problem) ? problem.errors : undefined;
    return new ApiError(response.status, problem, validationErrors);
  }

  get isUnauthorized(): boolean {
    return this.status === 401;
  }

  get isForbidden(): boolean {
    return this.status === 403;
  }

  get isValidationError(): boolean {
    return this.status === 422;
  }

  get isConcurrencyConflict(): boolean {
    return this.status === 409;
  }
}

export function normalizeProblemDetails(payload: unknown, status: number): ProblemDetails {
  if (isProblemDetails(payload)) {
    return {
      ...payload,
      status: payload.status ?? status,
    };
  }

  return {
    status,
    title: 'Request failed',
    detail: typeof payload === 'string' ? payload : undefined,
  };
}

function isProblemDetails(payload: unknown): payload is ProblemDetails {
  return typeof payload === 'object' && payload !== null;
}

export function isValidationProblem(problem: ProblemDetails): problem is ValidationProblemDetails {
  return typeof problem.errors === 'object' && problem.errors !== null;
}

export function getValidationMessages(problem: ValidationProblemDetails): string[] {
  if (!problem.errors) {
    return [];
  }

  return Object.values(problem.errors).flat();
}
