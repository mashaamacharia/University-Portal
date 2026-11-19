import { cn } from "@/lib/cn";

type Option<T extends string> = { value: T; label: string; count?: number };

export function FilterTabs<T extends string>({
  options,
  value,
  onChange,
  label,
}: {
  options: Option<T>[];
  value: T;
  onChange: (value: T) => void;
  label: string;
}) {
  return (
    <div role="group" aria-label={label} className="flex flex-wrap gap-2">
      {options.map((option) => {
        const selected = option.value === value;
        return (
          <button
            key={option.value}
            type="button"
            aria-pressed={selected}
            onClick={() => onChange(option.value)}
            className={cn(
              "rounded-full border px-3.5 py-1.5 text-sm font-medium transition-colors",
              selected
                ? "border-navy-900 bg-navy-900 text-white"
                : "border-line bg-white text-navy-800 hover:bg-navy-50",
            )}
          >
            {option.label}
            {option.count !== undefined && (
              <span className={cn("money ml-1.5", selected ? "text-navy-200" : "text-muted")}>
                {option.count}
              </span>
            )}
          </button>
        );
      })}
    </div>
  );
}
