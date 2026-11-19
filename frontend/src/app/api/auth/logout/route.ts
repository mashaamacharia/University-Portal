import { NextResponse } from "next/server";
import { SESSION_COOKIE } from "@/lib/server/backend";

export async function POST() {
  const res = new NextResponse(null, { status: 204 });
  res.cookies.delete(SESSION_COOKIE);
  return res;
}
