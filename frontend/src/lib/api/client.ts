import { ApiError, networkError, toApiError } from "./errors";

type Method = "GET" | "POST" | "PUT" | "DELETE";

type RequestOptions = {
  method?: Method;
  query?: object;
  body?: unknown;
  headers?: Record<string, string>;
  signal?: AbortSignal;
  /** Set false for calls where a 401 means "wrong credentials", not "session expired". */
  redirectOn401?: boolean;
};

function buildQuery(query?: object): string {
  if (!query) return "";
  const params = new URLSearchParams();
  for (const [key, value] of Object.entries(query)) {
    if (value === undefined || value === null || value === "") continue;
    params.set(key, String(value));
  }
  const qs = params.toString();
  return qs ? `?${qs}` : "";
}

/**
 * Browser-side fetch wrapper. Calls our own origin (/api/...), where the
 * Next.js proxy adds the JWT from the httpOnly cookie. Throws ApiError.
 */
export async function apiFetch<T>(
  path: string,
  { method = "GET", query, body, headers, signal, redirectOn401 = true }: RequestOptions = {},
): Promise<T> {
  let res: Response;
  try {
    res = await fetch(`${path}${buildQuery(query)}`, {
      method,
      signal,
      credentials: "same-origin",
      headers: {
        accept: "application/json",
        ...(body !== undefined ? { "content-type": "application/json" } : {}),
        ...headers,
      },
      body: body !== undefined ? JSON.stringify(body) : undefined,
    });
  } catch (error) {
    if (error instanceof DOMException && error.name === "AbortError") throw error;
    throw networkError();
  }

  if (
    res.status === 401 &&
    redirectOn401 &&
    typeof window !== "undefined" &&
    !window.location.pathname.startsWith("/login")
  ) {
    // Session expired: the proxy has already cleared the cookie.
    window.location.assign("/login?reason=expired");
  }

  if (!res.ok) throw await toApiError(res);

  const text = await res.text();
  return (text ? JSON.parse(text) : undefined) as T;
}

export { ApiError };
