import Link from "next/link";
import { FileText } from "lucide-react";
import { EmptyState } from "@/components/ui/empty-state";
import { InvoiceStatusBadge, PaymentStatusBadge } from "@/components/ui/status-badge";
import { Table, TBody, Td, Th, THead, Tr } from "@/components/ui/table";
import { cn } from "@/lib/cn";
import { formatDate, formatMoney } from "@/lib/format";
import type { OfficerInvoice } from "@/types";

export function OfficerInvoiceTable({
  invoices,
  emptyText = "Create a draft invoice to get started.",
}: {
  invoices: OfficerInvoice[];
  emptyText?: string;
}) {
  if (invoices.length === 0) {
    return <EmptyState icon={FileText} title="No invoices found" description={emptyText} />;
  }

  return (
    <Table>
      <THead>
        <tr>
          <Th>Invoice</Th>
          <Th>Student</Th>
          <Th>Date</Th>
          <Th>Due</Th>
          <Th align="right">Total</Th>
          <Th align="right">Remaining</Th>
          <Th>Status</Th>
        </tr>
      </THead>
      <TBody>
        {invoices.map((invoice, i) => {
          const label = invoice.invoiceNumber || "Draft";
          const overdue = invoice.paymentStatus === "Overdue";
          return (
            <Tr key={`${invoice.bcInvoiceId ?? invoice.invoiceNumber}-${i}`}>
              <Td>
                {invoice.bcInvoiceId ? (
                  <Link
                    href={`/officer/invoices/${invoice.bcInvoiceId}`}
                    className="font-medium text-navy-700 underline-offset-4 hover:underline"
                  >
                    {label}
                  </Link>
                ) : (
                  <span className="font-medium text-navy-900">{label}</span>
                )}
              </Td>
              <Td>
                <div className="text-navy-900">{invoice.studentName || "-"}</div>
                <div className="text-xs text-muted">{invoice.studentNumber}</div>
              </Td>
              <Td>{formatDate(invoice.invoiceDate)}</Td>
              <Td className={cn(overdue && "font-medium text-danger-700")}>
                {formatDate(invoice.dueDate)}
              </Td>
              <Td align="right">{formatMoney(invoice.totalAmount, invoice.currencyCode)}</Td>
              <Td align="right">{formatMoney(invoice.amountRemaining, invoice.currencyCode)}</Td>
              <Td>
                <div className="flex flex-wrap items-center gap-1.5">
                  <InvoiceStatusBadge status={invoice.status} />
                  {invoice.status === "Open" && (
                    <PaymentStatusBadge status={invoice.paymentStatus} />
                  )}
                </div>
              </Td>
            </Tr>
          );
        })}
      </TBody>
    </Table>
  );
}
