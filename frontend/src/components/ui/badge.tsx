import { cn } from "@/lib/cn";

export type BadgeTone = "neutral" | "navy" | "green" | "amber" | "red";

const tones: Record<BadgeTone, string> = {
  neutral: "bg-navy-50 text-navy-700 ring-navy-200",
  navy: "bg-navy-100 text-navy-800 ring-navy-300",
  green: "bg-forest-50 text-forest-700 ring-forest-200",
  amber: "bg-warn-50 text-warn-700 ring-warn-100",
  red: "bg-danger-50 text-danger-700 ring-danger-100",
};

export function Badge({
  tone = "neutral",
  className,
  ...props
}: React.ComponentProps<"span"> & { tone?: BadgeTone }) {
  return (
    <span
      className={cn(
        "inline-flex items-center whitespace-nowrap rounded-full px-2.5 py-0.5 text-xs font-medium ring-1 ring-inset",
        tones[tone],
        className,
      )}
      {...props}
    />
  );
}
