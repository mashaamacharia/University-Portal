"use client";

import { useEffect, useRef, useState } from "react";
import Link from "next/link";
import { ChevronDown, KeyRound, LogOut } from "lucide-react";
import { useAuth } from "@/components/auth/auth-provider";
import { roleLabel } from "@/lib/auth/roles";

function initials(name: string): string {
  const parts = name.trim().split(/\s+/).filter(Boolean);
  if (parts.length === 0) return "?";
  const first = parts[0][0];
  const last = parts.length > 1 ? parts[parts.length - 1][0] : "";
  return (first + last).toUpperCase();
}

export function UserMenu() {
  const { user, roles, signOut } = useAuth();
  const [open, setOpen] = useState(false);
  const [busy, setBusy] = useState(false);
  const ref = useRef<HTMLDivElement>(null);

  const displayName = user.fullName || user.email || "Account";

  useEffect(() => {
    if (!open) return;
    const onPointerDown = (e: PointerEvent) => {
      if (!ref.current?.contains(e.target as Node)) setOpen(false);
    };
    const onKeyDown = (e: KeyboardEvent) => {
      if (e.key === "Escape") setOpen(false);
    };
    document.addEventListener("pointerdown", onPointerDown);
    document.addEventListener("keydown", onKeyDown);
    return () => {
      document.removeEventListener("pointerdown", onPointerDown);
      document.removeEventListener("keydown", onKeyDown);
    };
  }, [open]);

  return (
    <div ref={ref} className="relative">
      <button
        type="button"
        onClick={() => setOpen((v) => !v)}
        aria-haspopup="menu"
        aria-expanded={open}
        className="flex items-center gap-2 rounded-lg py-1 pl-1 pr-2 hover:bg-navy-50"
      >
        <span className="grid size-8 place-items-center rounded-full bg-navy-900 text-xs font-semibold text-white">
          {initials(displayName)}
        </span>
        <span className="hidden max-w-40 truncate text-sm font-medium text-navy-900 sm:block">
          {displayName}
        </span>
        <ChevronDown className="size-4 text-muted" aria-hidden />
      </button>

      {open && (
        <div
          role="menu"
          className="absolute right-0 z-30 mt-2 w-64 rounded-xl border border-line bg-white p-1 shadow-pop"
        >
          <div className="border-b border-line px-3 py-3">
            <p className="truncate text-sm font-semibold text-navy-900">{displayName}</p>
            {user.email && user.fullName && (
              <p className="truncate text-sm text-muted">{user.email}</p>
            )}
            {user.studentNumber && (
              <p className="mt-0.5 text-sm text-muted">{user.studentNumber}</p>
            )}
            <p className="mt-1.5 text-xs text-forest-700">
              {roles.map(roleLabel).join(" · ")}
            </p>
          </div>

          <Link
            href="/account/password"
            role="menuitem"
            onClick={() => setOpen(false)}
            className="mt-1 flex items-center gap-2.5 rounded-lg px-3 py-2 text-sm text-navy-900 hover:bg-navy-50"
          >
            <KeyRound className="size-4 text-muted" aria-hidden />
            Change password
          </Link>
          <button
            type="button"
            role="menuitem"
            disabled={busy}
            onClick={async () => {
              setBusy(true);
              await signOut();
            }}
            className="flex w-full items-center gap-2.5 rounded-lg px-3 py-2 text-left text-sm text-navy-900 hover:bg-navy-50 disabled:opacity-50"
          >
            <LogOut className="size-4 text-muted" aria-hidden />
            Sign out
          </button>
        </div>
      )}
    </div>
  );
}
