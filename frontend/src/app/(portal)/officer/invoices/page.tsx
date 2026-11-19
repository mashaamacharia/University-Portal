import { OfficerInvoicesView, INVOICE_STATUSES } from "@/components/officer/invoices-view";
import type { InvoiceStatus } from "@/types";

export const metadata = { title: "Invoices" };

export default async function OfficerInvoicesPage({
  searchParams,
}: {
  searchParams: Promise<{ status?: string }>;
}) {
  const { status } = await searchParams;
  const initialStatus = INVOICE_STATUSES.find((s) => s === status) as InvoiceStatus | undefined;
  return <OfficerInvoicesView initialStatus={initialStatus} />;
}
