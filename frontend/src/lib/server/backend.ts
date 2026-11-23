import "server-only";
import { SESSION_COOKIE } from "@/lib/auth/constants";

export { SESSION_COOKIE };

const isProduction = process.env.NODE_ENV === "production";

/** Base URL of the ASP.NET API. Server-side only; never sent to the browser. */
export const API_BASE_URL = (
  process.env.API_BASE_URL ?? (isProduction ? "" : "http://localhost:5073")
).replace(/\/+$/, "");

if (isProduction && !API_BASE_URL && process.env.NEXT_PHASE !== "phase-production-build") {
  console.error(
    "[portal] API_BASE_URL is not set. Every API call will fail until it is.",
  );
}

const FALLBACK_LIFETIME_SECONDS = 60 * 60 * 8;

/**
 * Cookies are Secure in production (HTTPS only). To test a production build over
 * plain http on another machine, set COOKIE_SECURE=false. Never in real deployments.
 */
const secureCookie = process.env.COOKIE_SECURE
  ? process.env.COOKIE_SECURE === "true"
  : isProduction;

export function sessionCookieOptions(expiresAtUtc?: string) {
  const expires = expiresAtUtc ? new Date(expiresAtUtc) : undefined;
  const valid = expires && !Number.isNaN(expires.getTime());

  return {
    httpOnly: true, // JavaScript in the page can never read the token
    sameSite: "lax" as const,
    secure: secureCookie,
    path: "/",
    ...(valid ? { expires } : { maxAge: FALLBACK_LIFETIME_SECONDS }),
  };
}
