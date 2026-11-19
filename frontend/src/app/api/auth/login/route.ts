import { NextResponse } from "next/server";
import {
  API_BASE_URL,
  SESSION_COOKIE,
  sessionCookieOptions,
} from "@/lib/server/backend";
import type { components } from "@/types/api";

type LoginResponseShape = components["schemas"]["LoginResponse"];

/**
 * Signs in against the ASP.NET API, then keeps the JWT in an httpOnly cookie.
 * The browser only ever receives the user profile.
 */
export async function POST(req: Request) {
  let upstream: Response;
  try {
    upstream = await fetch(`${API_BASE_URL}/api/auth/login`, {
      method: "POST",
      headers: { "content-type": "application/json", accept: "application/json" },
      body: await req.text(),
      cache: "no-store",
    });
  } catch {
    return NextResponse.json(
      { status: 502, title: "Can't reach the portal service." },
      { status: 502 },
    );
  }

  if (!upstream.ok) {
    // Pass the API's error (wrong password, validation, ...) straight through.
    return new NextResponse(await upstream.text(), {
      status: upstream.status,
      headers: {
        "content-type":
          upstream.headers.get("content-type") ?? "application/json",
      },
    });
  }

  const data = (await upstream.json()) as LoginResponseShape;
  if (!data.accessToken || !data.user) {
    return NextResponse.json(
      { status: 502, title: "The portal service returned an unexpected response." },
      { status: 502 },
    );
  }

  const res = NextResponse.json({
    user: data.user,
    expiresAtUtc: data.expiresAtUtc,
  });
  res.cookies.set(
    SESSION_COOKIE,
    data.accessToken,
    sessionCookieOptions(data.expiresAtUtc),
  );
  return res;
}
