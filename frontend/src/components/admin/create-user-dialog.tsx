"use client";

import { useState } from "react";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Modal } from "@/components/ui/modal";
import { Select } from "@/components/ui/select";
import { ApiError } from "@/lib/api";
import { fieldError } from "@/lib/api/errors";
import { useCreateUser } from "@/lib/api/hooks/admin";
import type { CreateUserRequest, PortalRole, UserSummary } from "@/types";
import { PasswordField } from "./password-field";

const EMAIL = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

/** Mount it only while it should be open: it starts with a blank form each time. */
export function CreateUserDialog({
  onClose,
  onCreated,
}: {
  onClose: () => void;
  onCreated: (user: UserSummary) => void;
}) {
  const create = useCreateUser();
  const [fullName, setFullName] = useState("");
  const [email, setEmail] = useState("");
  const [role, setRole] = useState<PortalRole>("Student");
  const [studentNumber, setStudentNumber] = useState("");
  const [password, setPassword] = useState("");
  const [submitted, setSubmitted] = useState(false);

  const client = {
    fullName: fullName.trim() ? undefined : "Enter the person's name.",
    email: EMAIL.test(email.trim()) ? undefined : "Enter a valid email address.",
    studentNumber:
      role === "Student" && !studentNumber.trim() ? "Students need a student number." : undefined,
    password: password ? undefined : "Enter or generate a password.",
  };
  const hasClientErrors = Object.values(client).some(Boolean);
  const show = <K extends keyof typeof client>(key: K) =>
    (submitted ? client[key] : undefined) ?? fieldError(create.error, key);

  const apiError = create.error instanceof ApiError ? create.error : null;
  const showGeneralError = create.error && (!apiError || Object.keys(apiError.fieldErrors).length === 0);

  function submit() {
    setSubmitted(true);
    if (hasClientErrors) return;

    const body: CreateUserRequest = {
      email: email.trim(),
      fullName: fullName.trim(),
      password,
      role,
      studentNumber: role === "Student" ? studentNumber.trim() : null,
    };
    create.mutate(body, { onSuccess: onCreated });
  }

  return (
    <Modal
      open
      onClose={onClose}
      title="New user"
      description="Create a portal account and choose what they can access."
      footer={
        <>
          <Button variant="ghost" onClick={onClose} disabled={create.isPending}>
            Cancel
          </Button>
          <Button type="submit" form="create-user-form" variant="accent" loading={create.isPending}>
            Create user
          </Button>
        </>
      }
    >
      <form
        id="create-user-form"
        noValidate
        onSubmit={(e) => {
          e.preventDefault();
          submit();
        }}
        className="space-y-4"
      >
        {showGeneralError && create.error && <Alert tone="error">{create.error.message}</Alert>}

        <Input
          label="Full name"
          autoComplete="off"
          value={fullName}
          error={show("fullName")}
          onChange={(e) => setFullName(e.target.value)}
        />
        <Input
          label="Email"
          type="email"
          autoComplete="off"
          value={email}
          error={show("email")}
          onChange={(e) => setEmail(e.target.value)}
        />
        <Select
          label="Role"
          value={role}
          onChange={(e) => setRole(e.target.value as PortalRole)}
        >
          <option value="Student">Student</option>
          <option value="FinanceOfficer">Finance officer</option>
          <option value="Administrator">Administrator</option>
        </Select>
        {role === "Student" && (
          <Input
            label="Student number"
            autoComplete="off"
            placeholder="e.g. SC211/0001/2022"
            value={studentNumber}
            error={show("studentNumber")}
            onChange={(e) => setStudentNumber(e.target.value)}
          />
        )}
        <PasswordField
          label="Temporary password"
          value={password}
          onChange={setPassword}
          error={show("password")}
        />
      </form>
    </Modal>
  );
}
