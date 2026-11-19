"use client";

import { useState } from "react";
import { useMutation } from "@tanstack/react-query";
import { ApiError, authApi } from "@/lib/api";
import { fieldError } from "@/lib/api/errors";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { PasswordInput } from "@/components/ui/password-input";

export function ChangePasswordForm() {
  const [current, setCurrent] = useState("");
  const [next, setNext] = useState("");
  const [confirm, setConfirm] = useState("");
  const [mismatch, setMismatch] = useState(false);

  const change = useMutation<void, Error>({
    mutationFn: () =>
      authApi.changePassword({ currentPassword: current, newPassword: next }),
    onSuccess: () => {
      setCurrent("");
      setNext("");
      setConfirm("");
    },
  });

  const hasFieldErrors =
    change.error instanceof ApiError &&
    Object.keys(change.error.fieldErrors).length > 0;

  return (
    <form
      onSubmit={(e) => {
        e.preventDefault();
        if (next !== confirm) {
          setMismatch(true);
          return;
        }
        setMismatch(false);
        change.mutate();
      }}
      className="space-y-5"
    >
      {change.isSuccess && <Alert tone="success">Your password has been changed.</Alert>}
      {change.error && !hasFieldErrors && (
        <Alert tone="error">{change.error.message}</Alert>
      )}

      <PasswordInput
        label="Current password"
        autoComplete="current-password"
        required
        value={current}
        onChange={(e) => setCurrent(e.target.value)}
        error={fieldError(change.error, "currentPassword")}
      />
      <PasswordInput
        label="New password"
        autoComplete="new-password"
        required
        value={next}
        onChange={(e) => setNext(e.target.value)}
        error={fieldError(change.error, "newPassword")}
      />
      <PasswordInput
        label="Confirm new password"
        autoComplete="new-password"
        required
        value={confirm}
        onChange={(e) => {
          setConfirm(e.target.value);
          setMismatch(false);
        }}
        error={mismatch ? "The two passwords don't match." : undefined}
      />

      <Button type="submit" loading={change.isPending}>
        Update password
      </Button>
    </form>
  );
}
