import { AuthProvider } from "@/components/auth/auth-provider";
import { AppShell } from "@/components/shell/app-shell";
import { requireUser } from "@/lib/server/guards";

/** Everything signed-in lives under this layout: session gate + app shell. */
export default async function PortalLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const user = await requireUser();
  return (
    <AuthProvider user={user}>
      <AppShell>{children}</AppShell>
    </AuthProvider>
  );
}
