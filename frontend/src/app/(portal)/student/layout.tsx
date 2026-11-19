import { requireRole } from "@/lib/server/guards";

export default async function StudentLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  await requireRole("Student");
  return <>{children}</>;
}
