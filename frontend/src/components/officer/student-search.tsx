"use client";

import { useEffect, useId, useRef, useState } from "react";
import { Loader2, UserRound } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { useStudentSearch } from "@/lib/api/hooks/officer";
import { cn } from "@/lib/cn";
import { useDebouncedValue } from "@/lib/hooks/use-debounced-value";
import { list } from "@/lib/utils";
import type { StudentLookup } from "@/types";

/** Type-ahead student picker (ARIA combobox: arrow keys, Enter, Esc all work). */
export function StudentSearch({
  value,
  onChange,
  error,
}: {
  value: StudentLookup | null;
  onChange: (student: StudentLookup | null) => void;
  error?: string;
}) {
  const listId = useId();
  const containerRef = useRef<HTMLDivElement>(null);
  const [text, setText] = useState("");
  const [open, setOpen] = useState(false);
  const [active, setActive] = useState(0);

  const term = useDebouncedValue(text.trim());
  const search = useStudentSearch(term);
  const items = list(search.data);
  const ready = term.length >= 2;
  const activeIndex = Math.min(active, Math.max(items.length - 1, 0));
  const showList = open && text.trim().length >= 2;

  useEffect(() => {
    if (!open) return;
    const onPointerDown = (e: PointerEvent) => {
      if (!containerRef.current?.contains(e.target as Node)) setOpen(false);
    };
    document.addEventListener("pointerdown", onPointerDown);
    return () => document.removeEventListener("pointerdown", onPointerDown);
  }, [open]);

  function choose(student: StudentLookup) {
    onChange(student);
    setText("");
    setOpen(false);
  }

  if (value) {
    return (
      <div className="space-y-1.5">
        <p className="text-sm font-medium text-navy-900">Student</p>
        <div className="flex items-center justify-between gap-3 rounded-lg border border-navy-200 bg-white px-3 py-2.5">
          <div className="flex min-w-0 items-center gap-3">
            <span className="grid size-9 shrink-0 place-items-center rounded-full bg-navy-100 text-navy-700">
              <UserRound className="size-4" aria-hidden />
            </span>
            <div className="min-w-0">
              <p className="truncate text-sm font-medium text-navy-900">{value.studentName || "-"}</p>
              <p className="truncate text-xs text-muted">
                {value.studentNumber}
                {value.email ? ` · ${value.email}` : ""}
              </p>
            </div>
          </div>
          <Button variant="ghost" size="sm" onClick={() => onChange(null)}>
            Change
          </Button>
        </div>
      </div>
    );
  }

  return (
    <div ref={containerRef} className="relative">
      <Input
        label="Student"
        placeholder="Search by name or student number"
        hint="Type at least 2 characters."
        autoComplete="off"
        role="combobox"
        aria-expanded={showList}
        aria-controls={listId}
        aria-autocomplete="list"
        aria-activedescendant={showList && items.length > 0 ? `${listId}-${activeIndex}` : undefined}
        value={text}
        error={error}
        onFocus={() => setOpen(true)}
        onChange={(e) => {
          setText(e.target.value);
          setOpen(true);
          setActive(0);
        }}
        onKeyDown={(e) => {
          if (e.key === "ArrowDown" && items.length > 0) {
            e.preventDefault();
            setOpen(true);
            setActive(Math.min(activeIndex + 1, items.length - 1));
          } else if (e.key === "ArrowUp" && items.length > 0) {
            e.preventDefault();
            setActive(Math.max(activeIndex - 1, 0));
          } else if (
            e.key === "Enter" &&
            showList &&
            term === text.trim() &&
            items[activeIndex]
          ) {
            e.preventDefault(); // don't submit the form
            choose(items[activeIndex]);
          } else if (e.key === "Escape") {
            setOpen(false);
          }
        }}
        trailing={
          search.isFetching && ready ? (
            <Loader2 className="mr-2 size-4 animate-spin text-muted" aria-hidden />
          ) : undefined
        }
      />

      {showList && (
        <div className="absolute inset-x-0 top-full z-20 mt-1 max-h-64 overflow-auto rounded-xl border border-line bg-white shadow-pop">
          {search.isError ? (
            <p className="px-4 py-3 text-sm text-danger-700">{search.error.message}</p>
          ) : !ready || search.isPending || term !== text.trim() ? (
            <p className="px-4 py-3 text-sm text-muted">Searching&hellip;</p>
          ) : items.length === 0 ? (
            <p className="px-4 py-3 text-sm text-muted">No student found.</p>
          ) : (
            <ul id={listId} role="listbox" aria-label="Students">
              {items.map((student, i) => (
                <li
                  key={student.studentNumber}
                  id={`${listId}-${i}`}
                  role="option"
                  aria-selected={i === activeIndex}
                  onMouseDown={(e) => e.preventDefault()}
                  onClick={() => choose(student)}
                  onMouseEnter={() => setActive(i)}
                  className={cn(
                    "cursor-pointer px-4 py-2.5",
                    i === activeIndex && "bg-navy-50",
                  )}
                >
                  <p className="text-sm font-medium text-navy-900">{student.studentName || "-"}</p>
                  <p className="text-xs text-muted">
                    {student.studentNumber}
                    {student.email ? ` · ${student.email}` : ""}
                  </p>
                </li>
              ))}
            </ul>
          )}
        </div>
      )}
    </div>
  );
}
