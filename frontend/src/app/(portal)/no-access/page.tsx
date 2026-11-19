import { ShieldAlert } from "lucide-react";
import { EmptyState } from "@/components/ui/empty-state";
import { SignOutButton } from "@/components/auth/sign-out-button";

export const metadata = { title: "No access" };

export default function NoAccess() {
  return (
    <main className="mx-auto max-w-xl px-4 py-16">
      <EmptyState
        icon={ShieldAlert}
        title="Your account has no portal access yet"
        description="You're signed in, but no role is assigned to your account. Ask an administrator to set it up."
        action={<SignOutButton />}
      />
    </main>
  );
}
