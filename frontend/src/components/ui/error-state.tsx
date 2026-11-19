import { AlertTriangle } from "lucide-react";
import { Button } from "./button";
import { EmptyState } from "./empty-state";

export function ErrorState({
  error,
  onRetry,
}: {
  error: Error;
  onRetry?: () => void;
}) {
  return (
    <EmptyState
      icon={AlertTriangle}
      title="We couldn't load this"
      description={error.message}
      action={
        onRetry ? (
          <Button variant="secondary" onClick={onRetry}>
            Try again
          </Button>
        ) : undefined
      }
    />
  );
}
