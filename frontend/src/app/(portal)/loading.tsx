import { Skeleton } from "@/components/ui/skeleton";

export default function PortalLoading() {
  return (
    <div role="status" aria-live="polite">
      <span className="sr-only">Loading&hellip;</span>
      <Skeleton className="mb-6 h-9 w-64" />
      <div className="grid gap-4 sm:grid-cols-3">
        {[0, 1, 2].map((i) => (
          <Skeleton key={i} className="h-28 rounded-xl" />
        ))}
      </div>
      <Skeleton className="mt-6 h-72 rounded-xl" />
    </div>
  );
}
