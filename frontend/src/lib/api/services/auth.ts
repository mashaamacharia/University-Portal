import { apiFetch } from "../client";
import type {
  ChangePasswordRequest,
  CurrentUser,
  LoginRequest,
  LoginResult,
} from "@/types";

export const authApi = {
  login: (body: LoginRequest) =>
    apiFetch<LoginResult>("/api/auth/login", {
      method: "POST",
      body,
      redirectOn401: false,
    }),

  logout: () => apiFetch<void>("/api/auth/logout", { method: "POST" }),

  me: () => apiFetch<CurrentUser>("/api/auth/me"),

  changePassword: (body: ChangePasswordRequest) =>
    apiFetch<void>("/api/auth/change-password", { method: "POST", body }),
};
