"use client";

import { createContext, useCallback, useContext, useMemo } from "react";
import { useRouter } from "next/navigation";
import { useQueryClient } from "@tanstack/react-query";
import { authApi } from "@/lib/api";
import { hasRole } from "@/lib/auth/roles";
import { list } from "@/lib/utils";
import type { CurrentUser, PortalRole } from "@/types";

type AuthValue = {
  user: CurrentUser;
  roles: string[];
  hasRole: (role: PortalRole) => boolean;
  signOut: () => Promise<void>;
};

const AuthContext = createContext<AuthValue | null>(null);

/** Receives the user from a server layout, so client components can read it without refetching. */
export function AuthProvider({
  user,
  children,
}: {
  user: CurrentUser;
  children: React.ReactNode;
}) {
  const router = useRouter();
  const queryClient = useQueryClient();

  const signOut = useCallback(async () => {
    try {
      await authApi.logout();
    } finally {
      queryClient.clear(); // never leave one user's data cached for the next
      router.replace("/login");
      router.refresh();
    }
  }, [queryClient, router]);

  const value = useMemo<AuthValue>(
    () => ({
      user,
      roles: list(user.roles),
      hasRole: (role) => hasRole(user.roles, role),
      signOut,
    }),
    [user, signOut],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

export function useAuth(): AuthValue {
  const value = useContext(AuthContext);
  if (!value) throw new Error("useAuth must be used inside <AuthProvider>.");
  return value;
}
