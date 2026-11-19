"use client";

import { useEffect, useRef } from "react";
import { SidebarContent } from "./sidebar-nav";

/** Slide-in navigation for small screens, on a native <dialog> (focus trap + Esc included). */
export function MobileDrawer({
  open,
  onClose,
}: {
  open: boolean;
  onClose: () => void;
}) {
  const ref = useRef<HTMLDialogElement>(null);

  useEffect(() => {
    const dialog = ref.current;
    if (!dialog) return;
    if (open && !dialog.open) dialog.showModal();
    if (!open && dialog.open) dialog.close();
  }, [open]);

  // If the window grows to desktop width while open, put the drawer away.
  useEffect(() => {
    const query = window.matchMedia("(min-width: 1024px)");
    const handle = () => {
      if (query.matches) onClose();
    };
    query.addEventListener("change", handle);
    return () => query.removeEventListener("change", handle);
  }, [onClose]);

  return (
    <dialog
      ref={ref}
      onClose={onClose}
      onClick={(e) => {
        if (e.target === ref.current) onClose();
      }}
      aria-label="Navigation"
      className="fixed inset-y-0 left-0 m-0 h-dvh max-h-none w-72 max-w-[85vw] bg-navy-900 p-0 text-white backdrop:bg-navy-950/60 lg:hidden"
    >
      <SidebarContent onNavigate={onClose} />
    </dialog>
  );
}
