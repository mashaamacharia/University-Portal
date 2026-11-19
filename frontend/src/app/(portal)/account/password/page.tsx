import Link from "next/link";
import { ChangePasswordForm } from "@/components/auth/change-password-form";
import { Card, CardContent, CardHeader } from "@/components/ui/card";
import { homeForRoles } from "@/lib/auth/roles";
import { requireUser } from "@/lib/server/guards";

export const metadata = { title: "Change password" };

export default async function ChangePasswordPage() {
  const user = await requireUser();

  return (
    <main className="mx-auto max-w-md space-y-4 px-4 py-12">
      <Link
        href={homeForRoles(user.roles)}
        className="text-sm text-muted hover:text-navy-900"
      >
        &larr; Back
      </Link>
      <Card>
        <CardHeader
          title="Change password"
          description="Use something you don't use anywhere else."
        />
        <CardContent>
          <ChangePasswordForm />
        </CardContent>
      </Card>
    </main>
  );
}
