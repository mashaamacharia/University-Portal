import { apiFetch } from "../client";
import type {
  CreateUserRequest,
  UserPage,
  UsersQuery,
  UserSummary,
} from "@/types";

const base = "/api/admin/users";

export const adminApi = {
  users: (query?: UsersQuery) => apiFetch<UserPage>(base, { query }),

  user: (id: string) => apiFetch<UserSummary>(`${base}/${id}`),

  createUser: (body: CreateUserRequest) =>
    apiFetch<UserSummary>(base, { method: "POST", body }),

  setActive: (id: string, isActive: boolean) =>
    apiFetch<UserSummary>(`${base}/${id}/active`, {
      method: "PUT",
      body: { isActive },
    }),

  resetPassword: (id: string, newPassword: string) =>
    apiFetch<void>(`${base}/${id}/reset-password`, {
      method: "POST",
      body: { newPassword },
    }),
};
