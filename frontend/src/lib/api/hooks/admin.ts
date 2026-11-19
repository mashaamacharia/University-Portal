import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import { adminApi } from "@/lib/api";
import type { CreateUserRequest, UsersQuery, UserSummary } from "@/types";

export const adminKeys = {
  all: ["admin"] as const,
  users: (query?: UsersQuery) => ["admin", "users", query] as const,
};

export function useUsers(query: UsersQuery) {
  return useQuery({
    queryKey: adminKeys.users(query),
    queryFn: () => adminApi.users(query),
    placeholderData: keepPreviousData,
  });
}

export function useCreateUser() {
  const queryClient = useQueryClient();
  return useMutation<UserSummary, Error, CreateUserRequest>({
    mutationFn: (body) => adminApi.createUser(body),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: adminKeys.all }),
  });
}

export function useSetUserActive() {
  const queryClient = useQueryClient();
  return useMutation<UserSummary, Error, { id: string; isActive: boolean }>({
    mutationFn: ({ id, isActive }) => adminApi.setActive(id, isActive),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: adminKeys.all }),
  });
}

export function useResetPassword() {
  return useMutation<void, Error, { id: string; newPassword: string }>({
    mutationFn: ({ id, newPassword }) => adminApi.resetPassword(id, newPassword),
  });
}
