import { InvoiceDetailView } from "@/components/student/invoice-detail";

export const metadata = { title: "Invoice" };

export default async function InvoicePage({
  params,
}: {
  params: Promise<{ invoiceNumber: string }>;
}) {
  const { invoiceNumber } = await params;

  let number = invoiceNumber;
  try {
    number = decodeURIComponent(invoiceNumber);
  } catch {
    // keep the raw value if it isn't valid percent-encoding
  }

  return <InvoiceDetailView invoiceNumber={number} />;
}
