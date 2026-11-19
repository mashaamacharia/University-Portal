"use client";

import { FileQuestion } from "lucide-react";
import { PageHeader } from "@/components/page-header";
import { Card, CardContent, CardHeader } from "@/components/ui/card";
import { EmptyState } from "@/components/ui/empty-state";
import { ErrorState } from "@/components/ui/error-state";
import { Skeleton } from "@/components/ui/skeleton";
import { PaymentStatusBadge } from "@/components/ui/status-badge";
import { Table, TBody, Td, Th, THead, Tr } from "@/components/ui/table";
import { ApiError } from "@/lib/api";
import { useStudentInvoice } from "@/lib/api/hooks/student";
import { cn } from "@/lib/cn";
import { formatDate, formatMoney } from "@/lib/format";
import { list } from "@/lib/utils";

function Field({
  label,
  children,
  className,
}: {
  label: string;
  children: React.ReactNode;
  className?: string;
}) {
  return (
    <div className={className}>
      <dt className="text-sm text-muted">{label}</dt>
      <dd className="money mt-0.5 font-medium text-navy-900">{children}</dd>
    </div>
  );
}

export function InvoiceDetailView({ invoiceNumber }: { invoiceNumber: string }) {
  const query = useStudentInvoice(invoiceNumber);
  const header = <PageHeader title={`Invoice ${invoiceNumber}`} />;

  if (query.isPending) {
    return (
      <>
        {header}
        <Skeleton className="h-96 rounded-xl" />
      </>
    );
  }

  if (query.isError) {
    const notFound = query.error instanceof ApiError && query.error.isNotFound;
    return (
      <>
        {header}
        <Card>
          {notFound ? (
            <EmptyState
              icon={FileQuestion}
              title="Invoice not found"
              description="That invoice doesn't exist, or it isn't on your account."
            />
          ) : (
            <ErrorState error={query.error} onRetry={() => query.refetch()} />
          )}
        </Card>
      </>
    );
  }

  const { invoice } = query.data;
  const lines = list(query.data.lines);
  const cur = invoice.currencyCode;
  const overdue = invoice.paymentStatus === "Overdue";

  return (
    <>
      <PageHeader
        title={`Invoice ${invoice.invoiceNumber ?? invoiceNumber}`}
        actions={<PaymentStatusBadge status={invoice.paymentStatus} />}
      />

      <Card>
        <CardHeader title="Summary" />
        <CardContent>
          <dl className="grid grid-cols-2 gap-x-6 gap-y-5 sm:grid-cols-4">
            <Field label="Invoice date">{formatDate(invoice.invoiceDate)}</Field>
            <Field label="Due date">
              <span className={cn(overdue && "text-danger-700")}>
                {formatDate(invoice.dueDate)}
              </span>
            </Field>
            <Field label="Total">{formatMoney(invoice.totalAmount, cur)}</Field>
            <Field label="Paid">{formatMoney(invoice.amountPaid, cur)}</Field>
            <Field label="Before tax">{formatMoney(invoice.totalExcludingTax, cur)}</Field>
            <Field label="Tax">{formatMoney(invoice.taxAmount, cur)}</Field>
            <Field label="Remaining" className="col-span-2">
              <span
                className={cn(
                  "font-serif text-2xl",
                  invoice.amountRemaining > 0 ? "text-navy-900" : "text-forest-700",
                )}
              >
                {formatMoney(invoice.amountRemaining, cur)}
              </span>
            </Field>
          </dl>
        </CardContent>
      </Card>

      <Card className="mt-6">
        <CardHeader title="What this invoice covers" />
        {lines.length === 0 ? (
          <EmptyState title="No line items" description="This invoice has no itemised lines." />
        ) : (
          <Table>
            <THead>
              <tr>
                <Th>Description</Th>
                <Th align="right">Qty</Th>
                <Th align="right">Unit price</Th>
                <Th align="right">Discount</Th>
                <Th align="right">Before tax</Th>
                <Th align="right">Tax</Th>
                <Th align="right">Total</Th>
              </tr>
            </THead>
            <TBody>
              {lines.map((line, i) => (
                <Tr key={i}>
                  <Td>{line.description || "-"}</Td>
                  <Td align="right">{line.quantity}</Td>
                  <Td align="right">{formatMoney(line.unitPrice, cur)}</Td>
                  <Td align="right">{formatMoney(line.discountAmount, cur)}</Td>
                  <Td align="right">{formatMoney(line.amountExcludingTax, cur)}</Td>
                  <Td align="right">{formatMoney(line.taxAmount, cur)}</Td>
                  <Td align="right" className="font-medium">
                    {formatMoney(line.amountIncludingTax, cur)}
                  </Td>
                </Tr>
              ))}
            </TBody>
          </Table>
        )}
      </Card>
    </>
  );
}
