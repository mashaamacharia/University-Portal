import { ReceiptView } from "@/components/finance/receipt-view";

export const metadata = { title: "Receipt" };

export default async function StudentReceiptPage({
  params,
}: {
  params: Promise<{ receiptNumber: string }>;
}) {
  const { receiptNumber } = await params;
  return <ReceiptView audience="student" receiptNumber={receiptNumber} />;
}
