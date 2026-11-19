import { requireRole } from "@/lib/server/guards";

export default async function OfficerLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  await requireRole("FinanceOfficer");
  return <>{children}</>;
}
