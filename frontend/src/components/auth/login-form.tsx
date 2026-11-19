"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { useMutation } from "@tanstack/react-query";
import { ApiError, authApi } from "@/lib/api";
import { homeForRoles, safeNext } from "@/lib/auth/roles";
import type { LoginResult } from "@/types";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { PasswordInput } from "@/components/ui/password-input";

export function LoginForm({
  next,
  expired,
}: {
  next?: string;
  expired?: boolean;
}) {
  const router = useRouter();
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");

  const login = useMutation<LoginResult, Error>({
    mutationFn: () => authApi.login({ email: email.trim(), password }),
    onSuccess: ({ user }) => {
      router.replace(safeNext(next) ?? homeForRoles(user.roles));
      router.refresh();
    },
  });

  const errorMessage = login.error
    ? login.error instanceof ApiError && login.error.status === 401
      ? "Incorrect email or password."
      : login.error.message
    : null;

  return (
    <form
      onSubmit={(e) => {
        e.preventDefault();
        login.mutate();
      }}
      className="space-y-5"
    >
      {expired && !errorMessage && (
        <Alert tone="info">Your session ended. Sign in again to continue.</Alert>
      )}
      {errorMessage && <Alert tone="error">{errorMessage}</Alert>}

      <Input
        label="Email"
        type="email"
        name="email"
        autoComplete="username"
        autoFocus
        required
        placeholder="you@university.ac.ke"
        value={email}
        onChange={(e) => setEmail(e.target.value)}
      />
      <PasswordInput
        label="Password"
        name="password"
        autoComplete="current-password"
        required
        value={password}
        onChange={(e) => setPassword(e.target.value)}
      />

      <Button
        type="submit"
        size="lg"
        className="w-full"
        loading={login.isPending || login.isSuccess}
      >
        Sign in
      </Button>

      <p className="text-center text-sm text-muted">
        Forgot your password? Ask an administrator to reset it.
      </p>
    </form>
  );
}
