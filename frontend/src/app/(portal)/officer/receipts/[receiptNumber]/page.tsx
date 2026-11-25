import { ReceiptView } from "@/components/finance/receipt-view";

export const metadata = { title: "Receipt" };

export default async function OfficerReceiptPage({
  params,
  searchParams,
}: {
  params: Promise<{ receiptNumber: string }>;
  searchParams: Promise<{ studentNumber?: string }>;
}) {
  const [{ receiptNumber }, { studentNumber }] = await Promise.all([params, searchParams]);
  return (
    <ReceiptView
      audience="officer"
      receiptNumber={receiptNumber}
      studentNumber={studentNumber ?? ""}
    />
  );
}
