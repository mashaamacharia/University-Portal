import { AlertCircle, CheckCircle2, Info } from "lucide-react";
import { cn } from "@/lib/cn";

type Tone = "error" | "success" | "info";

const styles: Record<Tone, string> = {
  error: "border-danger-100 bg-danger-50 text-danger-700",
  success: "border-forest-200 bg-forest-50 text-forest-800",
  info: "border-navy-200 bg-navy-50 text-navy-800",
};

const icons = { error: AlertCircle, success: CheckCircle2, info: Info };

export function Alert({
  tone = "info",
  className,
  children,
}: {
  tone?: Tone;
  className?: string;
  children: React.ReactNode;
}) {
  const Icon = icons[tone];
  return (
    <div
      role={tone === "error" ? "alert" : "status"}
      className={cn(
        "flex items-start gap-2.5 rounded-lg border px-3.5 py-3 text-sm",
        styles[tone],
        className,
      )}
    >
      <Icon className="mt-0.5 size-4 shrink-0" aria-hidden />
      <div>{children}</div>
    </div>
  );
}
