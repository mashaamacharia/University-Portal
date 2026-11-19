import { AuthProvider } from "@/components/auth/auth-provider";
import { requireUser } from "@/lib/server/guards";

/**
 * Everything signed-in lives under this layout. F4 adds the sidebar and top bar here.
 */
export default async function PortalLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  const user = await requireUser();
  return <AuthProvider user={user}>{children}</AuthProvider>;
}
