"use client";

import { AlertTriangle } from "lucide-react";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/ui/empty-state";

export default function PortalError({
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  return (
    <div className="mx-auto max-w-xl py-10">
      <EmptyState
        icon={AlertTriangle}
        title="Something went wrong"
        description="The portal couldn't load this page. If it keeps happening, check that the API is running."
        action={<Button onClick={reset}>Try again</Button>}
      />
    </div>
  );
}
