import type { LucideIcon } from "lucide-react";

type EmptyStateProps = {
  icon?: LucideIcon;
  title: string;
  description?: string;
  action?: React.ReactNode;
};

export function EmptyState({
  icon: Icon,
  title,
  description,
  action,
}: EmptyStateProps) {
  return (
    <div className="flex flex-col items-center px-6 py-12 text-center">
      {Icon && (
        <span className="mb-4 grid size-12 place-items-center rounded-full bg-navy-100 text-navy-700">
          <Icon className="size-6" aria-hidden />
        </span>
      )}
      <h3 className="text-base font-semibold text-navy-900">{title}</h3>
      {description && (
        <p className="mt-1 max-w-sm text-sm text-muted">{description}</p>
      )}
      {action && <div className="mt-5">{action}</div>}
    </div>
  );
}
