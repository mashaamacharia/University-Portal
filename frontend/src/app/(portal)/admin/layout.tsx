import { requireRole } from "@/lib/server/guards";

export default async function AdminLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  await requireRole("Administrator");
  return <>{children}</>;
}
