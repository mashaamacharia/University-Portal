import "server-only";
import { redirect } from "next/navigation";
import { getCurrentUser } from "./session";
import { hasRole, homeForRoles } from "@/lib/auth/roles";
import type { PortalRole } from "@/types";

/** Use in server layouts and pages: signed-in users only. */
export async function requireUser() {
  const user = await getCurrentUser();
  if (!user) redirect("/login");
  return user;
}

/** Signed in AND holding the role; otherwise send them to their own home. */
export async function requireRole(role: PortalRole) {
  const user = await requireUser();
  if (!hasRole(user.roles, role)) redirect(homeForRoles(user.roles));
  return user;
}
