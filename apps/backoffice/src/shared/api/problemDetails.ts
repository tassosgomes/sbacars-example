import type { ProblemDetails, ProblemaSuspensao } from './types';

export class ApiError extends Error {
  readonly status: number;
  readonly problem: ProblemDetails;

  constructor(status: number, problem: ProblemDetails) {
    super(problem.detail ?? problem.title ?? `Erro HTTP ${status}`);
    this.name = 'ApiError';
    this.status = status;
    this.problem = problem;
  }

  get traceId(): string | undefined {
    return this.problem.traceId;
  }

  isSuspensaoNaoConfirmada(): this is ApiError & { problem: ProblemaSuspensao } {
    return (
      this.status === 409 &&
      (this.problem as ProblemaSuspensao).codigo === 'suspensao-nao-confirmada'
    );
  }
}

export async function parseProblemDetails(response: Response): Promise<ApiError> {
  const status = response.status;
  try {
    const data = await response.json();
    if (data && typeof data === 'object' && ('title' in data || 'detail' in data || 'type' in data)) {
      return new ApiError(status, data as ProblemDetails);
    }
    return new ApiError(status, {
      type: `https://httpstatuses.io/${status}`,
      title: response.statusText || 'Erro inesperado',
      status,
      detail: typeof data === 'string' ? data : JSON.stringify(data),
    });
  } catch {
    return new ApiError(status, {
      type: `https://httpstatuses.io/${status}`,
      title: response.statusText || 'Erro inesperado',
      status,
      detail: 'Não foi possível ler a resposta do servidor.',
    });
  }
}
