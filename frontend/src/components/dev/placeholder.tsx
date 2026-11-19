import Link from "next/link";
import { Card, CardContent } from "@/components/ui/card";
import { SignOutButton } from "@/components/auth/sign-out-button";
import { getCurrentUser } from "@/lib/server/session";

/** Temporary landing content until F5, F6 and F8 replace it. */
export async function Placeholder({ title, next }: { title: string; next: string }) {
  const user = await getCurrentUser();

  return (
    <main className="mx-auto max-w-2xl space-y-6 px-4 py-12">
      <div>
        <h1 className="font-serif text-4xl font-semibold text-navy-900">{title}</h1>
        <p className="mt-1 text-muted">
          Signed in as {user?.fullName ?? user?.email}
          {user?.email && user.fullName ? ` (${user.email})` : ""}.
        </p>
      </div>
      <Card>
        <CardContent className="space-y-4">
          <p className="text-sm text-ink">
            Sign-in works. This screen is replaced in <strong>{next}</strong>.
          </p>
          <div className="flex flex-wrap items-center gap-3">
            <Link
              href="/account/password"
              className="text-sm font-medium text-navy-700 underline underline-offset-4 hover:text-navy-900"
            >
              Change password
            </Link>
            <SignOutButton />
          </div>
        </CardContent>
      </Card>
    </main>
  );
}
