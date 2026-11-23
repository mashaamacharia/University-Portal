"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { CheckCircle2, FileQuestion, Trash2 } from "lucide-react";
import { PageHeader } from "@/components/page-header";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader } from "@/components/ui/card";
import { DetailField } from "@/components/ui/detail-field";
import { EmptyState } from "@/components/ui/empty-state";
import { ErrorState } from "@/components/ui/error-state";
import { Modal } from "@/components/ui/modal";
import { Skeleton } from "@/components/ui/skeleton";
import { InvoiceStatusBadge, PaymentStatusBadge } from "@/components/ui/status-badge";
import { Table, TBody, Td, Th, THead, Tr } from "@/components/ui/table";
import { ApiError } from "@/lib/api";
import {
  useDiscardInvoice,
  useOfficerInvoice,
  usePostInvoice,
} from "@/lib/api/hooks/officer";
import { cn } from "@/lib/cn";
import { formatDate, formatMoney } from "@/lib/format";
import { list } from "@/lib/utils";

type Dialog = "post" | "discard" | null;

export function OfficerInvoiceDetailView({ bcInvoiceId }: { bcInvoiceId: string }) {
  const router = useRouter();
  const query = useOfficerInvoice(bcInvoiceId);
  const post = usePostInvoice();
  const discard = useDiscardInvoice();
  const [dialog, setDialog] = useState<Dialog>(null);
  const [notice, setNotice] = useState<string | null>(null);

  function closeDialog() {
    setDialog(null);
    post.reset();
    discard.reset();
  }

  if (query.isPending) {
    return (
      <>
        <PageHeader title="Invoice" />
        <Skeleton className="h-96 rounded-xl" />
      </>
    );
  }

  if (query.isError) {
    const notFound = query.error instanceof ApiError && query.error.isNotFound;
    return (
      <>
        <PageHeader title="Invoice" />
        <Card>
          {notFound ? (
            <EmptyState
              icon={FileQuestion}
              title="Invoice not found"
              description="It may have been discarded, or the link is wrong."
            />
          ) : (
            <ErrorState error={query.error} onRetry={() => query.refetch()} />
          )}
        </Card>
      </>
    );
  }

  const invoice = query.data;
  const lines = list(invoice.lines);
  const cur = invoice.currencyCode;
  const isDraft = invoice.status === "Draft";
  const overdue = invoice.paymentStatus === "Overdue";
  const title = invoice.invoiceNumber ? `Invoice ${invoice.invoiceNumber}` : "Draft invoice";

  return (
    <>
      <PageHeader
        title={title}
        description={`${invoice.studentName ?? ""} · ${invoice.studentNumber ?? ""}`}
        actions={
          isDraft ? (
            <div className="flex flex-wrap gap-3">
              <Button variant="secondary" onClick={() => setDialog("discard")}>
                <Trash2 className="size-4" aria-hidden />
                Discard draft
              </Button>
              <Button variant="accent" onClick={() => setDialog("post")}>
                <CheckCircle2 className="size-4" aria-hidden />
                Post invoice
              </Button>
            </div>
          ) : undefined
        }
      />

      {notice && (
        <Alert tone="success" className="mb-4">
          {notice}
        </Alert>
      )}

      <Card>
        <CardHeader
          title="Summary"
          action={
            <div className="flex flex-wrap items-center gap-1.5">
              <InvoiceStatusBadge status={invoice.status} />
              {invoice.status === "Open" && <PaymentStatusBadge status={invoice.paymentStatus} />}
            </div>
          }
        />
        <CardContent>
          <dl className="grid grid-cols-2 gap-x-6 gap-y-5 sm:grid-cols-4">
            <DetailField label="Invoice date">{formatDate(invoice.invoiceDate)}</DetailField>
            <DetailField label="Due date">
              <span className={cn(overdue && "text-danger-700")}>{formatDate(invoice.dueDate)}</span>
            </DetailField>
            <DetailField label="Before tax">{formatMoney(invoice.totalExcludingTax, cur)}</DetailField>
            <DetailField label="Tax">{formatMoney(invoice.taxAmount, cur)}</DetailField>
            <DetailField label="Total">{formatMoney(invoice.totalAmount, cur)}</DetailField>
            <DetailField label="Remaining">{formatMoney(invoice.amountRemaining, cur)}</DetailField>
            {invoice.externalDocumentNumber && (
              <DetailField label="External document no." className="col-span-2">
                {invoice.externalDocumentNumber}
              </DetailField>
            )}
          </dl>
        </CardContent>
      </Card>

      <Card className="mt-6">
        <CardHeader title="Lines" />
        {lines.length === 0 ? (
          <EmptyState title="No lines" description="This invoice has no line items." />
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

      <Modal
        open={dialog === "post"}
        onClose={closeDialog}
        title="Post this invoice?"
        description={`${formatMoney(invoice.totalAmount, cur)} will be charged to ${invoice.studentName ?? "the student"}. Once posted, the invoice is final and can't be edited or discarded.`}
        footer={
          <>
            <Button variant="ghost" onClick={closeDialog} disabled={post.isPending}>
              Not yet
            </Button>
            <Button
              variant="accent"
              loading={post.isPending}
              onClick={() =>
                post.mutate(bcInvoiceId, {
                  onSuccess: () => {
                    setNotice("Invoice posted. The student can now see it.");
                    closeDialog();
                  },
                })
              }
            >
              Post invoice
            </Button>
          </>
        }
      >
        {post.error && <Alert tone="error">{post.error.message}</Alert>}
      </Modal>

      <Modal
        open={dialog === "discard"}
        onClose={closeDialog}
        title="Discard this draft?"
        description="The draft will be deleted. This can't be undone."
        footer={
          <>
            <Button variant="ghost" onClick={closeDialog} disabled={discard.isPending}>
              Keep draft
            </Button>
            <Button
              variant="danger"
              loading={discard.isPending}
              onClick={() =>
                discard.mutate(bcInvoiceId, {
                  onSuccess: () => router.replace("/officer/invoices"),
                })
              }
            >
              Discard draft
            </Button>
          </>
        }
      >
        {discard.error && <Alert tone="error">{discard.error.message}</Alert>}
      </Modal>
    </>
  );
}
