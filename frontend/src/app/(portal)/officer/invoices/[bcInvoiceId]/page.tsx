import { OfficerInvoiceDetailView } from "@/components/officer/officer-invoice-detail";

export const metadata = { title: "Invoice" };

export default async function OfficerInvoicePage({
  params,
}: {
  params: Promise<{ bcInvoiceId: string }>;
}) {
  const { bcInvoiceId } = await params;
  return <OfficerInvoiceDetailView bcInvoiceId={bcInvoiceId} />;
}
