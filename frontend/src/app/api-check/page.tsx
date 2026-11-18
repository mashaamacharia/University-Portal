"use client";

/**
 * Temporary page at /api-check to prove the cookie + proxy flow end to end.
 * Delete it once F3 (login) is in place.
 */
import { useState } from "react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { ApiError, authApi, officerApi, studentApi } from "@/lib/api";

export default function ApiCheck() {
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [output, setOutput] = useState<string>("Nothing yet.");
  const [busy, setBusy] = useState(false);

  async function run(label: string, call: () => Promise<unknown>) {
    setBusy(true);
    try {
      const data = await call();
      setOutput(`${label}: OK\n\n${JSON.stringify(data ?? null, null, 2)}`);
    } catch (e) {
      const err = e as ApiError;
      setOutput(
        `${label}: FAILED (${err.status ?? "?"})\n${err.message}\n\n${JSON.stringify(err.fieldErrors ?? {}, null, 2)}`,
      );
    } finally {
      setBusy(false);
    }
  }

  return (
    <main className="mx-auto max-w-3xl space-y-6 px-4 py-10">
      <h1 className="font-serif text-3xl font-semibold text-navy-900">
        API check
      </h1>

      <Card>
        <CardHeader title="Sign in" description="Uses a user you created through the admin API." />
        <CardContent className="space-y-4">
          <Input label="Email" type="email" value={email} onChange={(e) => setEmail(e.target.value)} />
          <Input label="Password" type="password" value={password} onChange={(e) => setPassword(e.target.value)} />
          <div className="flex flex-wrap gap-3">
            <Button loading={busy} onClick={() => run("login", () => authApi.login({ email, password }))}>
              Sign in
            </Button>
            <Button variant="secondary" disabled={busy} onClick={() => run("me", authApi.me)}>
              Who am I
            </Button>
            <Button variant="secondary" disabled={busy} onClick={() => run("student statement", () => studentApi.statement(5))}>
              Student statement
            </Button>
            <Button variant="secondary" disabled={busy} onClick={() => run("officer dashboard", officerApi.dashboard)}>
              Officer dashboard
            </Button>
            <Button variant="ghost" disabled={busy} onClick={() => run("logout", authApi.logout)}>
              Sign out
            </Button>
          </div>
        </CardContent>
      </Card>

      <pre className="overflow-x-auto rounded-xl bg-navy-950 p-5 text-sm text-navy-100">
        {output}
      </pre>
    </main>
  );
}
