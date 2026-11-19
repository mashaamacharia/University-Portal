"use client";

import { useState } from "react";
import { Menu } from "lucide-react";
import { Breadcrumbs } from "./breadcrumbs";
import { MobileDrawer } from "./mobile-drawer";
import { SidebarContent } from "./sidebar-nav";
import { UserMenu } from "./user-menu";

/** Sidebar + top bar + content area for everything signed-in. */
export function AppShell({ children }: { children: React.ReactNode }) {
  const [drawerOpen, setDrawerOpen] = useState(false);

  return (
    <div className="min-h-screen">
      <a
        href="#main"
        className="sr-only print:hidden focus:not-sr-only focus:fixed focus:left-3 focus:top-3 focus:z-50 focus:rounded-lg focus:bg-white focus:px-3 focus:py-2 focus:text-sm focus:shadow-pop"
      >
        Skip to content
      </a>

      <aside className="fixed inset-y-0 left-0 z-30 hidden w-64 bg-navy-900 lg:block print:hidden">
        <SidebarContent />
      </aside>

      <MobileDrawer open={drawerOpen} onClose={() => setDrawerOpen(false)} />

      <div className="lg:pl-64 print:pl-0">
        <header className="sticky top-0 z-20 flex h-14 print:hidden items-center gap-3 border-b border-line bg-white/90 px-4 backdrop-blur sm:px-6">
          <button
            type="button"
            onClick={() => setDrawerOpen(true)}
            aria-label="Open menu"
            className="-ml-1 grid size-9 place-items-center rounded-lg text-navy-900 hover:bg-navy-50 lg:hidden"
          >
            <Menu className="size-5" aria-hidden />
          </button>
          <Breadcrumbs className="min-w-0 flex-1" />
          <UserMenu />
        </header>

        <main
          id="main"
          className="mx-auto w-full max-w-6xl px-4 py-6 sm:px-6 sm:py-8"
        >
          {children}
        </main>
      </div>
    </div>
  );
}
