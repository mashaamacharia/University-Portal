import { GraduationCap } from "lucide-react";
import { cn } from "@/lib/cn";

export function Brand({
  tone = "dark",
  className,
}: {
  tone?: "dark" | "light";
  className?: string;
}) {
  return (
    <span className={cn("inline-flex items-center gap-2.5", className)}>
      <span className="grid size-9 place-items-center rounded-lg bg-forest-600 text-white">
        <GraduationCap className="size-5" aria-hidden />
      </span>
      <span
        className={cn(
          "font-serif text-xl font-semibold",
          tone === "light" ? "text-white" : "text-navy-900",
        )}
      >
        University Portal
      </span>
    </span>
  );
}
