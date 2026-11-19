import { useId } from "react";
import { ChevronDown } from "lucide-react";
import { cn } from "@/lib/cn";

type SelectProps = React.ComponentProps<"select"> & {
  label: string;
  hint?: string;
  error?: string;
};

export function Select({
  label,
  hint,
  error,
  id,
  className,
  children,
  ...props
}: SelectProps) {
  const autoId = useId();
  const selectId = id ?? autoId;

  return (
    <div className="space-y-1.5">
      <label htmlFor={selectId} className="block text-sm font-medium text-navy-900">
        {label}
      </label>
      <div className="relative">
        <select
          id={selectId}
          aria-invalid={error ? true : undefined}
          className={cn(
            "h-10 w-full appearance-none rounded-lg border bg-white pl-3 pr-9 text-sm text-ink",
            error ? "border-danger-600" : "border-navy-200 hover:border-navy-300",
            className,
          )}
          {...props}
        >
          {children}
        </select>
        <ChevronDown
          className="pointer-events-none absolute right-3 top-1/2 size-4 -translate-y-1/2 text-muted"
          aria-hidden
        />
      </div>
      {error ? (
        <p className="text-sm text-danger-700">{error}</p>
      ) : hint ? (
        <p className="text-sm text-muted">{hint}</p>
      ) : null}
    </div>
  );
}
