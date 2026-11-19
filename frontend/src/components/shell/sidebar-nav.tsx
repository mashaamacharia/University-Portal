"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { Brand } from "@/components/brand";
import { useAuth } from "@/components/auth/auth-provider";
import { cn } from "@/lib/cn";
import { isActive, navForRoles } from "./nav-config";

/** Brand + navigation. Used in the fixed desktop sidebar and inside the mobile drawer. */
export function SidebarContent({ onNavigate }: { onNavigate?: () => void }) {
  const { roles } = useAuth();
  const pathname = usePathname();
  const groups = navForRoles(roles);
  const showGroupLabels = groups.length > 1;

  return (
    <div className="flex h-full flex-col">
      <div className="flex h-14 shrink-0 items-center px-5">
        <Brand tone="light" />
      </div>

      <nav aria-label="Main" className="flex-1 space-y-6 overflow-y-auto px-3 py-4">
        {groups.map((group) => (
          <div key={group.role}>
            {showGroupLabels && (
              <p className="mb-1.5 px-3 text-xs font-medium text-navy-300">
                {group.label}
              </p>
            )}
            <ul className="space-y-0.5">
              {group.items.map((item) => {
                const Icon = item.icon;

                if (item.soon) {
                  return (
                    <li key={item.href}>
                      <span
                        aria-disabled="true"
                        className="flex cursor-default items-center gap-3 rounded-lg px-3 py-2 text-sm text-navy-400"
                      >
                        <Icon className="size-[18px]" aria-hidden />
                        <span className="flex-1">{item.label}</span>
                        <span className="rounded-full bg-white/10 px-2 py-0.5 text-[11px] text-navy-200">
                          Soon
                        </span>
                      </span>
                    </li>
                  );
                }

                const active = isActive(item, pathname);
                return (
                  <li key={item.href}>
                    <Link
                      href={item.href}
                      onClick={onNavigate}
                      aria-current={active ? "page" : undefined}
                      className={cn(
                        "relative flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium transition-colors",
                        active
                          ? "bg-white/10 text-white"
                          : "text-navy-200 hover:bg-white/5 hover:text-white",
                      )}
                    >
                      {active && (
                        <span
                          aria-hidden
                          className="absolute inset-y-1.5 left-0 w-[3px] rounded-full bg-forest-400"
                        />
                      )}
                      <Icon className="size-[18px]" aria-hidden />
                      {item.label}
                    </Link>
                  </li>
                );
              })}
            </ul>
          </div>
        ))}
      </nav>
    </div>
  );
}
