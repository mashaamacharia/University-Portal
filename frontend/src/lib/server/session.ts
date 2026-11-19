import "server-only";
import { cache } from "react";
import { cookies } from "next/headers";
import { API_BASE_URL, SESSION_COOKIE } from "./backend";
import type { CurrentUser } from "@/types";

/**
 * The signed-in user, or null when there is no valid session.
 * Cached per request, so layouts and pages can all call it for free.
 * Throws only when the API itself is down or broken.
 */
export const getCurrentUser = cache(async (): Promise<CurrentUser | null> => {
  const token = (await cookies()).get(SESSION_COOKIE)?.value;
  if (!token) return null;

  let res: Response;
  try {
    res = await fetch(`${API_BASE_URL}/api/auth/me`, {
      headers: { authorization: `Bearer ${token}`, accept: "application/json" },
      cache: "no-store",
    });
  } catch {
    throw new Error("Can't reach the portal service. Check that the API is running.");
  }

  if (res.status === 401 || res.status === 403) return null;
  if (!res.ok) throw new Error(`The portal service returned an error (${res.status}).`);
  return (await res.json()) as CurrentUser;
});
