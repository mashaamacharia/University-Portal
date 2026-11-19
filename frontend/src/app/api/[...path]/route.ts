import { NextRequest, NextResponse } from "next/server";
import { API_BASE_URL, SESSION_COOKIE } from "@/lib/server/backend";

/**
 * Same-origin proxy: the browser calls /api/<anything> on this app, and we
 * forward it to the ASP.NET API with the JWT from the httpOnly cookie.
 * Paths match the backend exactly, so they line up with src/types/api.d.ts.
 * (/api/auth/login and /api/auth/logout have their own routes and win over this one.)
 */
const FORWARDED_HEADERS = ["content-type", "accept", "idempotency-key"];

async function proxy(
  req: NextRequest,
  ctx: { params: Promise<{ path: string[] }> },
) {
  const { path } = await ctx.params;
  if (path.some((segment) => segment === ".." || segment === ".")) {
    return NextResponse.json({ status: 400, title: "Bad path." }, { status: 400 });
  }

  const url = `${API_BASE_URL}/api/${path.map(encodeURIComponent).join("/")}${req.nextUrl.search}`;

  const headers = new Headers();
  for (const name of FORWARDED_HEADERS) {
    const value = req.headers.get(name);
    if (value) headers.set(name, value);
  }
  const token = req.cookies.get(SESSION_COOKIE)?.value;
  if (token) headers.set("authorization", `Bearer ${token}`);

  const hasBody = req.method !== "GET" && req.method !== "HEAD";

  let upstream: Response;
  try {
    upstream = await fetch(url, {
      method: req.method,
      headers,
      body: hasBody ? await req.arrayBuffer() : undefined,
      cache: "no-store",
      redirect: "manual",
    });
  } catch {
    return NextResponse.json(
      { status: 502, title: "Can't reach the portal service." },
      { status: 502 },
    );
  }

  const responseHeaders = new Headers();
  const contentType = upstream.headers.get("content-type");
  if (contentType) responseHeaders.set("content-type", contentType);

  const res = new NextResponse(upstream.body, {
    status: upstream.status,
    headers: responseHeaders,
  });

  // Expired or invalid token: drop the cookie so the app sends the user to sign in.
  if (upstream.status === 401) res.cookies.delete(SESSION_COOKIE);
  return res;
}

export {
  proxy as GET,
  proxy as POST,
  proxy as PUT,
  proxy as PATCH,
  proxy as DELETE,
};
