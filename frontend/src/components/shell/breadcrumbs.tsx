"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { ChevronRight } from "lucide-react";
import { cn } from "@/lib/cn";

const LABELS: Record<string, string> = {
  student: "Student",
  officer: "Finance",
  admin: "Administration",
  invoices: "Invoices",
  payments: "Payments",
  statement: "Statement",
  outstanding: "Outstanding balances",
  account: "Account",
  password: "Change password",
  "no-access": "No access",
  new: "New invoice",
};

/** Segments with no page of their own: shown as text, not links. */
const NO_PAGE = new Set(["account"]);
const ROOT_LABELS: Record<string, string> = {
  student: "Dashboard",
  officer: "Dashboard",
  admin: "Users",
};
const UUID = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;

function labelFor(segment: string): string {
  if (LABELS[segment]) return LABELS[segment];
  if (UUID.test(segment)) return "Details";
  return decodeURIComponent(segment);
}

export function Breadcrumbs({ className }: { className?: string }) {
  const segments = usePathname().split("/").filter(Boolean);
  if (segments.length === 0) return null;

  // "/student" alone is the dashboard; "/admin" alone is the users page.
  const crumbs =
    segments.length === 1 && ROOT_LABELS[segments[0]]
      ? [{ label: ROOT_LABELS[segments[0]], href: `/${segments[0]}` }]
      : segments.map((segment, i) => ({
          label: labelFor(segment),
          href: `/${segments.slice(0, i + 1).join("/")}`,
          linkable: !NO_PAGE.has(segment),
        }));

  return (
    <nav aria-label="Breadcrumb" className={className}>
      <ol className="flex items-center gap-1.5 text-sm">
        {crumbs.map((crumb, i) => {
          const last = i === crumbs.length - 1;
          const linkable = "linkable" in crumb ? crumb.linkable : false;
          return (
            <li
              key={crumb.href}
              className={cn("flex min-w-0 items-center gap-1.5", !last && "hidden sm:flex")}
            >
              {i > 0 && <ChevronRight className="size-3.5 shrink-0 text-navy-300" aria-hidden />}
              {last ? (
                <span aria-current="page" className="truncate font-medium text-navy-900">
                  {crumb.label}
                </span>
              ) : linkable ? (
                <Link href={crumb.href} className="text-muted hover:text-navy-900">
                  {crumb.label}
                </Link>
              ) : (
                <span className="text-muted">{crumb.label}</span>
              )}
            </li>
          );
        })}
      </ol>
    </nav>
  );
}
