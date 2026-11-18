export type FieldErrors = Record<string, string[]>;

export class ApiError extends Error {
  readonly status: number;
  /** Validation messages keyed by the API's field name (e.g. "NewPassword"). */
  readonly fieldErrors: FieldErrors;

  constructor(status: number, message: string, fieldErrors: FieldErrors = {}) {
    super(message);
    this.name = "ApiError";
    this.status = status;
    this.fieldErrors = fieldErrors;
  }

  get isUnauthorized() {
    return this.status === 401;
  }
  get isForbidden() {
    return this.status === 403;
  }
  get isNotFound() {
    return this.status === 404;
  }
}

function defaultMessage(status: number): string {
  if (status === 0 || status === 502)
    return "Can't reach the portal service. Check that the API is running.";
  if (status === 400) return "Check the details and try again.";
  if (status === 401) return "Your session has expired. Sign in again.";
  if (status === 403) return "You don't have access to this.";
  if (status === 404) return "We couldn't find that.";
  if (status === 409) return "That conflicts with existing data.";
  if (status >= 500) return "Something went wrong on our side. Try again.";
  return "The request failed. Try again.";
}

/** Reads an ASP.NET ProblemDetails / ValidationProblemDetails body (or plain text) into an ApiError. */
export async function toApiError(res: Response): Promise<ApiError> {
  let title: string | undefined;
  let detail: string | undefined;
  let errors: FieldErrors = {};

  const text = await res.text().catch(() => "");
  if (text) {
    try {
      const json = JSON.parse(text) as {
        title?: string;
        detail?: string;
        message?: string;
        errors?: FieldErrors;
      };
      title = json.title;
      detail = json.detail ?? json.message;
      if (json.errors && typeof json.errors === "object") errors = json.errors;
    } catch {
      if (text.length < 300) detail = text;
    }
  }

  const firstFieldError = Object.values(errors).flat()[0];
  const message =
    detail ?? firstFieldError ?? title ?? defaultMessage(res.status);
  return new ApiError(res.status, message, errors);
}

export function networkError(): ApiError {
  return new ApiError(0, defaultMessage(0));
}
