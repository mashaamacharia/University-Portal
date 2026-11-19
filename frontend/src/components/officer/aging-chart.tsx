import { CircleCheck } from "lucide-react";
import { EmptyState } from "@/components/ui/empty-state";
import { formatMoney } from "@/lib/format";
import type { AgingBucket } from "@/types";

// Buckets are ordered oldest-last, so colour escalates from calm to alarming.
const COLORS = [
  "var(--color-forest-500)",
  "var(--color-navy-400)",
  "#d9962b",
  "#d9692b",
  "var(--color-danger-600)",
];

/** Horizontal bars in plain CSS: stays sharp and readable on phones. */
export function AgingChart({ buckets }: { buckets: AgingBucket[] }) {
  const total = buckets.reduce((sum, b) => sum + Math.max(b.amount, 0), 0);
  const max = Math.max(0, ...buckets.map((b) => b.amount));

  if (max <= 0) {
    return (
      <EmptyState
        icon={CircleCheck}
        title="Nothing outstanding"
        description="There are no unpaid balances right now."
      />
    );
  }

  return (
    <ul className="space-y-5">
      {buckets.map((bucket, i) => {
        const share = total > 0 ? Math.round((Math.max(bucket.amount, 0) / total) * 100) : 0;
        const width = bucket.amount > 0 ? Math.max((bucket.amount / max) * 100, 2) : 0;
        return (
          <li key={`${bucket.label}-${i}`}>
            <div className="mb-1.5 flex items-baseline justify-between gap-3 text-sm">
              <span className="font-medium text-navy-900">{bucket.label}</span>
              <span className="money text-ink">
                {formatMoney(bucket.amount)}
                <span className="text-muted"> &middot; {share}%</span>
              </span>
            </div>
            <div className="h-2.5 overflow-hidden rounded-full bg-navy-100">
              <div
                className="h-full rounded-full"
                style={{
                  width: `${width}%`,
                  backgroundColor: COLORS[Math.min(i, COLORS.length - 1)],
                }}
              />
            </div>
          </li>
        );
      })}
    </ul>
  );
}
