import { cn } from "@/lib/cn";

/** Label + value pair for <dl> summaries. */
export function DetailField({
  label,
  children,
  className,
}: {
  label: string;
  children: React.ReactNode;
  className?: string;
}) {
  return (
    <div className={className}>
      <dt className="text-sm text-muted">{label}</dt>
      <dd className={cn("money mt-0.5 font-medium text-navy-900")}>{children}</dd>
    </div>
  );
}
