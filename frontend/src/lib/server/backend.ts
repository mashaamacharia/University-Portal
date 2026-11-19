import "server-only";
import { SESSION_COOKIE } from "@/lib/auth/constants";

export { SESSION_COOKIE };

/** Base URL of the ASP.NET API. Server-side only; never sent to the browser. */
export const API_BASE_URL = (
  process.env.API_BASE_URL ?? "http://localhost:5073"
).replace(/\/+$/, "");

const FALLBACK_LIFETIME_SECONDS = 60 * 60 * 8;

export function sessionCookieOptions(expiresAtUtc?: string) {
  const expires = expiresAtUtc ? new Date(expiresAtUtc) : undefined;
  const valid = expires && !Number.isNaN(expires.getTime());

  return {
    httpOnly: true, // JavaScript in the page can never read the token
    sameSite: "lax" as const,
    secure: process.env.NODE_ENV === "production",
    path: "/",
    ...(valid ? { expires } : { maxAge: FALLBACK_LIFETIME_SECONDS }),
  };
}
