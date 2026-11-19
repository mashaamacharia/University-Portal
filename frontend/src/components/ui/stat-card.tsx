import type { LucideIcon } from "lucide-react";
import { cn } from "@/lib/cn";

type Tone = "navy" | "green" | "amber" | "red";

const iconTone: Record<Tone, string> = {
  navy: "bg-navy-100 text-navy-700",
  green: "bg-forest-100 text-forest-700",
  amber: "bg-warn-100 text-warn-700",
  red: "bg-danger-100 text-danger-700",
};

type StatCardProps = {
  label: string;
  value: string;
  hint?: string;
  icon?: LucideIcon;
  tone?: Tone;
  className?: string;
};

export function StatCard({
  label,
  value,
  hint,
  icon: Icon,
  tone = "navy",
  className,
}: StatCardProps) {
  return (
    <div
      className={cn(
        "flex items-start justify-between gap-3 rounded-xl border border-line bg-white p-5 shadow-card",
        className,
      )}
    >
      <div className="min-w-0">
        <p className="text-sm text-muted">{label}</p>
        <p className="money mt-1 truncate font-serif text-3xl font-semibold text-navy-900">
          {value}
        </p>
        {hint && <p className="mt-1 text-sm text-muted">{hint}</p>}
      </div>
      {Icon && (
        <span
          className={cn(
            "grid size-10 shrink-0 place-items-center rounded-lg",
            iconTone[tone],
          )}
        >
          <Icon className="size-5" aria-hidden />
        </span>
      )}
    </div>
  );
}
