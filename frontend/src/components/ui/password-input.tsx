"use client";

import { useState } from "react";
import { Eye, EyeOff } from "lucide-react";
import { Input } from "./input";

export function PasswordInput(
  props: Omit<React.ComponentProps<typeof Input>, "type" | "trailing">,
) {
  const [visible, setVisible] = useState(false);
  return (
    <Input
      {...props}
      type={visible ? "text" : "password"}
      trailing={
        <button
          type="button"
          onClick={() => setVisible((v) => !v)}
          aria-label={visible ? "Hide password" : "Show password"}
          aria-pressed={visible}
          className="grid size-8 place-items-center rounded-md text-muted hover:bg-navy-100 hover:text-navy-900"
        >
          {visible ? (
            <EyeOff className="size-4" aria-hidden />
          ) : (
            <Eye className="size-4" aria-hidden />
          )}
        </button>
      }
    />
  );
}
