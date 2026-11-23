"use client";

import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Modal } from "@/components/ui/modal";
import { useSetUserActive } from "@/lib/api/hooks/admin";
import type { UserSummary } from "@/types";

export function ToggleActiveDialog({
  user,
  onClose,
  onDone,
}: {
  user: UserSummary;
  onClose: () => void;
  onDone: (user: UserSummary, isActive: boolean) => void;
}) {
  const toggle = useSetUserActive();
  const activating = !user.isActive;
  const name = user.fullName || user.email || "this user";

  return (
    <Modal
      open
      onClose={onClose}
      title={activating ? `Reactivate ${name}?` : `Deactivate ${name}?`}
      description={
        activating
          ? "They will be able to sign in again."
          : "They won't be able to sign in. Their records are kept, and you can reactivate them any time."
      }
      footer={
        <>
          <Button variant="ghost" onClick={onClose} disabled={toggle.isPending}>
            Cancel
          </Button>
          <Button
            variant={activating ? "accent" : "danger"}
            loading={toggle.isPending}
            onClick={() =>
              toggle.mutate(
                { id: user.id, isActive: activating },
                { onSuccess: () => onDone(user, activating) },
              )
            }
          >
            {activating ? "Reactivate" : "Deactivate"}
          </Button>
        </>
      }
    >
      {toggle.error && <Alert tone="error">{toggle.error.message}</Alert>}
    </Modal>
  );
}
