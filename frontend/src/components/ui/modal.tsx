"use client";

import { useEffect, useRef } from "react";
import { X } from "lucide-react";

type ModalProps = {
  open: boolean;
  onClose: () => void;
  title: string;
  description?: string;
  children?: React.ReactNode;
  footer?: React.ReactNode;
};

/** Built on the native <dialog>: focus trapping and Esc come for free. */
export function Modal({
  open,
  onClose,
  title,
  description,
  children,
  footer,
}: ModalProps) {
  const ref = useRef<HTMLDialogElement>(null);

  useEffect(() => {
    const dialog = ref.current;
    if (!dialog) return;
    if (open && !dialog.open) dialog.showModal();
    if (!open && dialog.open) dialog.close();
  }, [open]);

  return (
    <dialog
      ref={ref}
      onClose={onClose}
      onClick={(e) => {
        // A click on the dialog box itself (not its content) is the backdrop.
        if (e.target === ref.current) onClose();
      }}
      className="m-auto w-[calc(100%-2rem)] max-w-lg rounded-2xl bg-white p-0 text-ink shadow-pop backdrop:bg-navy-950/50"
    >
      <div className="flex items-start justify-between gap-4 px-6 pt-5">
        <div>
          <h2 className="text-lg font-semibold text-navy-900">{title}</h2>
          {description && (
            <p className="mt-1 text-sm text-muted">{description}</p>
          )}
        </div>
        <button
          type="button"
          onClick={onClose}
          aria-label="Close"
          className="rounded-md p-1 text-muted hover:bg-navy-100 hover:text-navy-900"
        >
          <X className="size-5" aria-hidden />
        </button>
      </div>
      {children && <div className="px-6 py-4">{children}</div>}
      {footer && (
        <div className="flex justify-end gap-2 border-t border-line px-6 py-4">
          {footer}
        </div>
      )}
    </dialog>
  );
}
