import Link from "next/link";
import { FileText } from "lucide-react";
import { EmptyState } from "@/components/ui/empty-state";
import { PaymentStatusBadge } from "@/components/ui/status-badge";
import { Table, TBody, Td, Th, THead, Tr } from "@/components/ui/table";
import { cn } from "@/lib/cn";
import { formatDate, formatMoney } from "@/lib/format";
import type { InvoiceSummary } from "@/types";

export function InvoiceTable({
  invoices,
  compact = false,
}: {
  invoices: InvoiceSummary[];
  /** Fewer columns, for the dashboard. */
  compact?: boolean;
}) {
  if (invoices.length === 0) {
    return (
      <EmptyState
        icon={FileText}
        title="No invoices yet"
        description="Invoices appear here once they're issued to you."
      />
    );
  }

  return (
    <Table>
      <THead>
        <tr>
          <Th>Invoice</Th>
          {!compact && <Th>Date</Th>}
          <Th>Due</Th>
          {!compact && <Th align="right">Total</Th>}
          <Th align="right">Remaining</Th>
          <Th>Status</Th>
        </tr>
      </THead>
      <TBody>
        {invoices.map((invoice) => {
          const number = invoice.invoiceNumber ?? "";
          const overdue = invoice.paymentStatus === "Overdue";
          return (
            <Tr key={number}>
              <Td>
                <Link
                  href={`/student/invoices/${encodeURIComponent(number)}`}
                  className="font-medium text-navy-700 underline-offset-4 hover:underline"
                >
                  {number || "-"}
                </Link>
              </Td>
              {!compact && <Td>{formatDate(invoice.invoiceDate)}</Td>}
              <Td className={cn(overdue && "font-medium text-danger-700")}>
                {formatDate(invoice.dueDate)}
              </Td>
              {!compact && (
                <Td align="right">
                  {formatMoney(invoice.totalAmount, invoice.currencyCode)}
                </Td>
              )}
              <Td align="right">
                {formatMoney(invoice.amountRemaining, invoice.currencyCode)}
              </Td>
              <Td>
                <PaymentStatusBadge status={invoice.paymentStatus} />
              </Td>
            </Tr>
          );
        })}
      </TBody>
    </Table>
  );
}
