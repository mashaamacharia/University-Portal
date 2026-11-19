import { Banknote } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { EmptyState } from "@/components/ui/empty-state";
import { Table, TBody, Td, Th, THead, Tr } from "@/components/ui/table";
import { cn } from "@/lib/cn";
import { formatDate, formatMoney } from "@/lib/format";
import type { PaymentHistoryEntry } from "@/types";

export function PaymentTable({
  payments,
  emptyText = "Payments appear here once they're recorded.",
}: {
  payments: PaymentHistoryEntry[];
  emptyText?: string;
}) {
  if (payments.length === 0) {
    return <EmptyState icon={Banknote} title="No payments to show" description={emptyText} />;
  }

  return (
    <Table>
      <THead>
        <tr>
          <Th>Receipt</Th>
          <Th>Date</Th>
          <Th align="right">Amount</Th>
        </tr>
      </THead>
      <TBody>
        {payments.map((payment, i) => (
          <Tr key={`${payment.receiptNumber}-${i}`}>
            <Td className="font-medium">{payment.receiptNumber || "-"}</Td>
            <Td>{formatDate(payment.paymentDate)}</Td>
            <Td align="right">
              <span className={cn(payment.reversed && "text-muted line-through")}>
                {formatMoney(payment.amount, payment.currencyCode)}
              </span>
              {payment.reversed && (
                <Badge tone="red" className="ml-2 align-middle">
                  Reversed
                </Badge>
              )}
            </Td>
          </Tr>
        ))}
      </TBody>
    </Table>
  );
}
