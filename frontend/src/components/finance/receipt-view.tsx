"use client";

import { useEffect, useState } from "react";
import Link from "next/link";
import Image from "next/image";
import { Alert } from "@/components/ui/alert";
import { Badge } from "@/components/ui/badge";
import { Button, buttonVariants } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { EmptyState } from "@/components/ui/empty-state";
import { ErrorState } from "@/components/ui/error-state";
import { Skeleton } from "@/components/ui/skeleton";
import { usePrintOfficerReceipt, useOfficerReceipt } from "@/lib/api/hooks/officer";
import { usePrintStudentReceipt, useStudentReceipt } from "@/lib/api/hooks/student";
import { ApiError } from "@/lib/api";
import { formatDate, formatMoney } from "@/lib/format";
import type { Receipt } from "@/types";

type Props = {
  audience: "student" | "officer";
  receiptNumber: string;
  studentNumber?: string;
};

function Field({
  label,
  children,
}: {
  label: string;
  children: React.ReactNode;
}) {
  return (
    <div>
      <dt className="text-sm text-muted">{label}</dt>
      <dd className="mt-0.5 font-medium text-navy-900">{children}</dd>
    </div>
  );
}

export function ReceiptView({ audience, receiptNumber, studentNumber = "" }: Props) {
  const [printedReceipt, setPrintedReceipt] = useState<Receipt | null>(null);
  const studentQuery = useStudentReceipt(receiptNumber, audience === "student");
  const officerQuery = useOfficerReceipt(
    studentNumber,
    receiptNumber,
    audience === "officer",
  );
  const studentPrint = usePrintStudentReceipt();
  const officerPrint = usePrintOfficerReceipt();
  const query = audience === "student" ? studentQuery : officerQuery;
  const receipt = printedReceipt ?? query.data;
  const isPrinting = studentPrint.isPending || officerPrint.isPending;
  const printError = audience === "student" ? studentPrint.error : officerPrint.error;

  useEffect(() => {
    if (printedReceipt) window.print();
  }, [printedReceipt]);

  function print() {
    if (audience === "student") {
      studentPrint.mutate(receiptNumber, { onSuccess: setPrintedReceipt });
    } else {
      officerPrint.mutate(
        { studentNumber, receiptNumber },
        { onSuccess: setPrintedReceipt },
      );
    }
  }

  const backHref = audience === "student" ? "/student/payments" : "/officer/payments";
  const title = `Receipt ${receiptNumber}`;

  if (audience === "officer" && !studentNumber) {
    return (
      <>
        <div className="no-print mb-6 flex items-center justify-between gap-4">
          <h1 className="font-serif text-3xl text-navy-900">{title}</h1>
          <Link className={buttonVariants({ variant: "ghost" })} href={backHref}>
            Back to payments
          </Link>
        </div>
        <Card>
          <EmptyState
            title="Student number required"
            description="Open this receipt from the finance payments list so the student account can be verified."
          />
        </Card>
      </>
    );
  }

  if (query.isPending) {
    return (
      <>
        <div className="no-print mb-6">
          <h1 className="font-serif text-3xl text-navy-900">{title}</h1>
        </div>
        <Skeleton className="h-96 rounded-xl" />
      </>
    );
  }

  if (query.isError) {
    const notFound = query.error instanceof ApiError && query.error.isNotFound;
    return (
      <>
        <div className="no-print mb-6 flex items-center justify-between gap-4">
          <h1 className="font-serif text-3xl text-navy-900">{title}</h1>
          <Link className={buttonVariants({ variant: "ghost" })} href={backHref}>
            Back to payments
          </Link>
        </div>
        <Card>
          {notFound ? (
            <EmptyState
              title="Receipt not found"
              description="That receipt doesn't exist, or it isn't available for this account."
            />
          ) : (
            <ErrorState error={query.error} onRetry={() => query.refetch()} />
          )}
        </Card>
      </>
    );
  }

  if (!receipt) return null;

  return (
    <>
      <div className="no-print mb-6 flex flex-wrap items-center justify-between gap-4">
        <div>
          <h1 className="font-serif text-3xl text-navy-900">{title}</h1>
          <p className="mt-1 text-sm text-muted">Payment receipt details and print copy.</p>
        </div>
        <div className="flex items-center gap-2">
          <Link className={buttonVariants({ variant: "ghost" })} href={backHref}>
            Back to payments
          </Link>
          <Button
            variant="accent"
            onClick={print}
            loading={isPrinting}
            disabled={receipt.status === "Reversed"}
          >
            Print receipt
          </Button>
        </div>
      </div>

      {printError && (
        <div className="no-print mb-4">
          <Alert tone="error">{printError.message}</Alert>
        </div>
      )}
      {receipt.status === "Reversed" && (
        <div className="no-print mb-4">
          <Alert tone="error">This payment was reversed. Its receipt cannot be printed.</Alert>
        </div>
      )}

      <article className="receipt-print mx-auto max-w-3xl rounded-xl border border-line bg-white p-6 shadow-card sm:p-10">
        <header className="border-b-2 border-navy-900 pb-5 text-center">
          <Image
            src="/chuka-university-logo.png"
            alt="Chuka University"
            width={180}
            height={110}
            priority
            className="mx-auto mb-3 h-20 w-36 object-contain"
          />
          <h2 className="font-serif text-3xl font-semibold text-navy-900">
            {receipt.issuer.name}
          </h2>
          {receipt.issuer.addressLines.map((line, index) => (
            <p key={`${index}-${line}`} className="text-sm text-muted">{line}</p>
          ))}
          {(receipt.issuer.phone || receipt.issuer.email) && (
            <p className="mt-1 text-sm text-muted">
              {[receipt.issuer.phone, receipt.issuer.email].filter(Boolean).join(" · ")}
            </p>
          )}
          <p className="mt-5 text-lg font-semibold uppercase tracking-widest text-navy-900">
            Payment receipt
          </p>
        </header>

        <div className="mt-6 flex flex-wrap items-start justify-between gap-4">
          <div>
            <p className="text-sm text-muted">Receipt number</p>
            <p className="money text-xl font-semibold text-navy-900">{receipt.receiptNumber}</p>
          </div>
          <div className="text-right">
            <p className="text-sm text-muted">Payment date</p>
            <p className="font-medium text-navy-900">{formatDate(receipt.paymentDate)}</p>
          </div>
        </div>

        {receipt.copyNumber !== null && (
          <div className="mt-4 text-center">
            <Badge tone={receipt.copyNumber === 0 ? "green" : "amber"}>
              {receipt.copyNumber === 0
                ? "Original"
                : `Duplicate copy ${receipt.copyNumber}`}
            </Badge>
          </div>
        )}

        <dl className="mt-7 grid gap-x-8 gap-y-5 border-y border-line py-6 sm:grid-cols-2">
          <Field label="Received from">
            {receipt.student.name} ({receipt.student.studentNumber})
          </Field>
          <Field label="Payment method">{receipt.method || "Entered in Business Central"}</Field>
          <Field label="Reference">{receipt.reference || "-"}</Field>
          <Field label="Received by">{receipt.receivedBy || "Entered in Business Central"}</Field>
          <Field label="Settled invoice numbers">
            {receipt.settledInvoices.length > 0
              ? receipt.settledInvoices.join(", ")
              : "On account or allocation not available"}
          </Field>
          {receipt.invoiceBalances.map((invoice) => (
            <Field key={invoice.invoiceNumber} label={`Current balance · ${invoice.invoiceNumber}`}>
              {formatMoney(invoice.remainingBalance, invoice.currencyCode)}
            </Field>
          ))}
          <Field label="Current total account balance">
            {receipt.currentAccountBalance === null
              ? "Unavailable"
              : formatMoney(
                  receipt.currentAccountBalance,
                  receipt.accountBalanceCurrencyCode,
                )}
          </Field>
        </dl>

        <div className="mt-7 flex items-end justify-between gap-4">
          <p className="text-lg font-semibold text-navy-900">Amount received</p>
          <p className="money font-serif text-3xl font-semibold text-forest-700">
            {formatMoney(receipt.amountReceived, receipt.currencyCode)}
          </p>
        </div>

        {receipt.issuer.footerNote && (
          <footer className="mt-10 border-t border-line pt-4 text-center text-sm text-muted">
            {receipt.issuer.footerNote}
          </footer>
        )}
      </article>
    </>
  );
}
