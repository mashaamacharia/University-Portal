import {
  Banknote,
  FileText,
  HandCoins,
  LayoutDashboard,
  ScrollText,
  Users,
  type LucideIcon,
} from "lucide-react";
import { hasRole } from "@/lib/auth/roles";
import type { PortalRole } from "@/types";

export type NavItem = {
  label: string;
  href: string;
  icon: LucideIcon;
  /** Highlight only on this exact path (dashboards). Others also match sub-pages. */
  exact?: boolean;
};

export type NavGroup = { role: PortalRole; label: string; items: NavItem[] };

// Order matters: it is also the order sections appear for someone with several roles.
const NAV: NavGroup[] = [
  {
    role: "Administrator",
    label: "Administration",
    items: [{ label: "Users", href: "/admin", icon: Users }],
  },
  {
    role: "FinanceOfficer",
    label: "Finance office",
    items: [
      { label: "Dashboard", href: "/officer", icon: LayoutDashboard, exact: true },
      { label: "Invoices", href: "/officer/invoices", icon: FileText },
      { label: "Payments", href: "/officer/payments", icon: Banknote },
      {
        label: "Outstanding balances",
        href: "/officer/outstanding",
        icon: HandCoins,
      },
    ],
  },
  {
    role: "Student",
    label: "My fees",
    items: [
      { label: "Dashboard", href: "/student", icon: LayoutDashboard, exact: true },
      { label: "Invoices", href: "/student/invoices", icon: FileText },
      { label: "Payments", href: "/student/payments", icon: Banknote },
      { label: "Statement", href: "/student/statement", icon: ScrollText },
    ],
  },
];

export function navForRoles(roles: readonly string[]): NavGroup[] {
  return NAV.filter((group) => hasRole(roles, group.role));
}

export function isActive(item: NavItem, pathname: string): boolean {
  if (pathname === item.href) return true;
  return !item.exact && pathname.startsWith(`${item.href}/`);
}
