"use client";

import { useState } from "react";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Modal } from "@/components/ui/modal";
import { ApiError } from "@/lib/api";
import { fieldError } from "@/lib/api/errors";
import { useResetPassword } from "@/lib/api/hooks/admin";
import type { UserSummary } from "@/types";
import { PasswordField } from "./password-field";

export function ResetPasswordDialog({
  user,
  onClose,
  onDone,
}: {
  user: UserSummary;
  onClose: () => void;
  onDone: (user: UserSummary) => void;
}) {
  const reset = useResetPassword();
  const [password, setPassword] = useState("");
  const [submitted, setSubmitted] = useState(false);

  const apiError = reset.error instanceof ApiError ? reset.error : null;
  const showGeneralError = reset.error && (!apiError || Object.keys(apiError.fieldErrors).length === 0);
  const clientError = submitted && !password ? "Enter or generate a password." : undefined;

  return (
    <Modal
      open
      onClose={onClose}
      title="Reset password"
      description={`Set a new password for ${user.fullName || user.email}. Their old one stops working.`}
      footer={
        <>
          <Button variant="ghost" onClick={onClose} disabled={reset.isPending}>
            Cancel
          </Button>
          <Button type="submit" form="reset-password-form" loading={reset.isPending}>
            Reset password
          </Button>
        </>
      }
    >
      <form
        id="reset-password-form"
        noValidate
        onSubmit={(e) => {
          e.preventDefault();
          setSubmitted(true);
          if (!password) return;
          reset.mutate({ id: user.id, newPassword: password }, { onSuccess: () => onDone(user) });
        }}
        className="space-y-4"
      >
        {showGeneralError && reset.error && <Alert tone="error">{reset.error.message}</Alert>}
        <PasswordField
          label="New password"
          value={password}
          onChange={setPassword}
          error={clientError ?? fieldError(reset.error, "newPassword")}
        />
      </form>
    </Modal>
  );
}
