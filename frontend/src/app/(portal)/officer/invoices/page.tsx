import { OfficerInvoicesView } from "@/components/officer/invoices-view";
import { isInvoiceStatus } from "@/lib/invoice-status";

export const metadata = { title: "Invoices" };

export default async function OfficerInvoicesPage({
  searchParams,
}: {
  searchParams: Promise<{ status?: string }>;
}) {
  const { status } = await searchParams;
  const initialStatus = isInvoiceStatus(status) ? status : undefined;
  return <OfficerInvoicesView initialStatus={initialStatus} />;
}
