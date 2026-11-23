"use client";

import { AlertTriangle } from "lucide-react";
import { Brand } from "@/components/brand";
import { Button } from "@/components/ui/button";
import { EmptyState } from "@/components/ui/empty-state";

/** Catches errors outside the signed-in area (e.g. the sign-in page). */
export default function RootError({ reset }: { error: Error & { digest?: string }; reset: () => void }) {
  return (
    <main className="mx-auto flex min-h-screen max-w-xl flex-col justify-center px-4 py-16">
      <Brand className="mb-10 self-center" />
      <EmptyState
        icon={AlertTriangle}
        title="Something went wrong"
        description="Please try again. If it keeps happening, check that the API is running."
        action={<Button onClick={reset}>Try again</Button>}
      />
    </main>
  );
}
