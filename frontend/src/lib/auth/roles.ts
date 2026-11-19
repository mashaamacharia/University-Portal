import type { PortalRole } from "@/types";

const HOME: Record<PortalRole, string> = {
  Administrator: "/admin",
  FinanceOfficer: "/officer",
  Student: "/student",
};

// If someone holds several roles, the first match here decides where they land.
const PRIORITY: PortalRole[] = ["Administrator", "FinanceOfficer", "Student"];

export function hasRole(
  roles: readonly string[] | null | undefined,
  role: PortalRole,
): boolean {
  return !!roles?.includes(role);
}

/** Landing page for a signed-in user. */
export function homeForRoles(
  roles: readonly string[] | null | undefined,
): string {
  const match = PRIORITY.find((role) => hasRole(roles, role));
  return match ? HOME[match] : "/no-access";
}

/** Only allow same-site relative paths after sign-in (blocks open redirects). */
export function safeNext(next?: string | null): string | null {
  if (!next) return null;
  if (!next.startsWith("/") || next.startsWith("//") || next.startsWith("/\\"))
    return null;
  if (next.startsWith("/login") || next.startsWith("/api")) return null;
  return next;
}

const ROLE_LABELS: Record<PortalRole, string> = {
  Administrator: "Administrator",
  FinanceOfficer: "Finance officer",
  Student: "Student",
};

export function roleLabel(role: string): string {
  return ROLE_LABELS[role as PortalRole] ?? role;
}
