"use client";

import { useState } from "react";
import { LogOut } from "lucide-react";
import { Button } from "@/components/ui/button";
import { useAuth } from "./auth-provider";

export function SignOutButton() {
  const { signOut } = useAuth();
  const [busy, setBusy] = useState(false);

  return (
    <Button
      variant="secondary"
      loading={busy}
      onClick={async () => {
        setBusy(true);
        await signOut();
      }}
    >
      <LogOut className="size-4" aria-hidden />
      Sign out
    </Button>
  );
}
