export interface ConcurrencyConflictProblem {
  status: number;
  title: string;
  detail: string;
  expectedVersion: number;
  actualVersion: number;
  updatedAt: string;
}

export function isConcurrencyConflict(payload: unknown): payload is ConcurrencyConflictProblem {
  if (typeof payload !== 'object' || payload === null) {
    return false;
  }

  const problem = payload as Record<string, unknown>;
  return (
    problem.status === 409 &&
    typeof problem.expectedVersion === 'number' &&
    typeof problem.actualVersion === 'number' &&
    typeof problem.updatedAt === 'string'
  );
}

export function describeConcurrencyConflict(problem: ConcurrencyConflictProblem): string {
  return `This content was updated by someone else (you had version ${problem.expectedVersion}, current is ${problem.actualVersion}). Reload before saving.`;
}
