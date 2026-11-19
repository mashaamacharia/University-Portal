"use client";

import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { generatePassword } from "@/lib/password";

/**
 * A visible temporary-password field with a Generate button.
 * It is shown in plain text on purpose: the admin has to hand it to the user.
 */
export function PasswordField({
  label,
  value,
  onChange,
  error,
}: {
  label: string;
  value: string;
  onChange: (value: string) => void;
  error?: string;
}) {
  return (
    <Input
      label={label}
      type="text"
      autoComplete="off"
      spellCheck={false}
      hint="Share it with the user securely. They can change it after signing in."
      value={value}
      error={error}
      className="pr-24 font-mono"
      onChange={(e) => onChange(e.target.value)}
      trailing={
        <Button type="button" variant="ghost" size="sm" onClick={() => onChange(generatePassword())}>
          Generate
        </Button>
      }
    />
  );
}
