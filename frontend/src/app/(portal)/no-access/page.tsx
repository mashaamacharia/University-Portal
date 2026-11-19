import { ShieldAlert } from "lucide-react";
import { EmptyState } from "@/components/ui/empty-state";

export const metadata = { title: "No access" };

export default function NoAccess() {
  return (
    <div className="mx-auto max-w-xl py-10">
      <EmptyState
        icon={ShieldAlert}
        title="Your account has no portal access yet"
        description="You're signed in, but no role is assigned to your account. Ask an administrator to set it up, then sign in again."
      />
    </div>
  );
}
