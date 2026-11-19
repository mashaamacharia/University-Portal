import { OutstandingView } from "@/components/officer/outstanding-view";

export const metadata = { title: "Outstanding balances" };

export default async function OutstandingPage({
  searchParams,
}: {
  searchParams: Promise<{ overdue?: string }>;
}) {
  const { overdue } = await searchParams;
  return <OutstandingView initialOverdueOnly={overdue === "1"} />;
}
