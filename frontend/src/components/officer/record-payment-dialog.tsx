"use client";

import { useMemo, useRef, useState } from "react";
import Link from "next/link";
import { Alert } from "@/components/ui/alert";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Modal } from "@/components/ui/modal";
import { Select } from "@/components/ui/select";
import {
  useCheckPaymentRequest,
  useRecordPayment,
  useReconcilePayment,
  useStudentOpenInvoices,
} from "@/lib/api/hooks/officer";
import { formatMoney } from "@/lib/format";
import { list } from "@/lib/utils";
import type {
  OfficerInvoice,
  PaymentRequestResult,
  StudentLookup,
} from "@/types";
import { StudentSearch } from "./student-search";

type Props = {
  open: boolean;
  onClose: () => void;
};

function localToday() {
  const now = new Date();
  const year = now.getFullYear();
  const month = String(now.getMonth() + 1).padStart(2, "0");
  const day = String(now.getDate()).padStart(2, "0");
  return `${year}-${month}-${day}`;
}

function newRequestId() {
  if (typeof crypto.randomUUID === "function") return crypto.randomUUID();

  const bytes = crypto.getRandomValues(new Uint8Array(16));
  bytes[6] = (bytes[6] & 0x0f) | 0x40;
  bytes[8] = (bytes[8] & 0x3f) | 0x80;
  const hex = Array.from(bytes, (byte) => byte.toString(16).padStart(2, "0")).join("");
  return `${hex.slice(0, 8)}-${hex.slice(8, 12)}-${hex.slice(12, 16)}-${hex.slice(16, 20)}-${hex.slice(20)}`;
}

function invoiceLabel(invoice: OfficerInvoice) {
  const number = invoice.invoiceNumber || "Invoice";
  const due = invoice.dueDate ? ` · due ${invoice.dueDate}` : "";
  return `${number} · balance ${formatMoney(invoice.amountRemaining, invoice.currencyCode)}${due}`;
}

export function RecordPaymentDialog({ open, onClose }: Props) {
  const record = useRecordPayment();
  const reconcile = useReconcilePayment();
  const checkStatus = useCheckPaymentRequest();
  const [student, setStudent] = useState<StudentLookup | null>(null);
  const [invoiceNumber, setInvoiceNumber] = useState("");
  const [amount, setAmount] = useState("");
  const [paymentDate, setPaymentDate] = useState(localToday);
  const [method, setMethod] = useState("Cash");
  const [reference, setReference] = useState("");
  const [submitted, setSubmitted] = useState(false);
  const [result, setResult] = useState<PaymentRequestResult | null>(null);
  const attempt = useRef<{ fingerprint: string; requestId: string } | null>(null);

  const invoicesQuery = useStudentOpenInvoices(student?.studentNumber ?? undefined);
  const openInvoices = useMemo(
    () => list(invoicesQuery.data?.items).filter(
      (invoice) => (invoice.amountRemaining ?? 0) > 0 && Boolean(invoice.invoiceNumber),
    ),
    [invoicesQuery.data],
  );
  const selectedInvoice = openInvoices.find(
    (invoice) => invoice.invoiceNumber === invoiceNumber,
  );
  const amountValue = Number(amount);
  const amountHasTooManyDecimals = (amount.split(".")[1]?.length ?? 0) > 2;
  const hasValidAmount =
    amount.trim() !== "" &&
    Number.isFinite(amountValue) &&
    amountValue > 0 &&
    amountValue <= 100_000_000 &&
    !amountHasTooManyDecimals &&
    (!selectedInvoice || amountValue <= (selectedInvoice.amountRemaining ?? 0));
  const hasValidDate = Boolean(paymentDate) && paymentDate <= localToday();
  const studentError = submitted && !student ? "Choose a student." : undefined;
  const amountError =
    submitted && !amount.trim()
      ? "Enter an amount greater than zero."
      : submitted && Number.isFinite(amountValue) && amountValue > 100_000_000
        ? "Amount cannot exceed 100,000,000."
        : submitted && amount.trim() && Number.isFinite(amountValue) && amountValue <= 0
          ? "Enter an amount greater than zero."
          : submitted && amount.trim() && !Number.isFinite(amountValue)
            ? "Enter a valid amount."
            : submitted && amountHasTooManyDecimals
              ? "Amount can have at most 2 decimal places."
              : submitted && selectedInvoice && amountValue > (selectedInvoice.amountRemaining ?? 0)
          ? "Amount cannot exceed the selected invoice's remaining balance."
          : undefined;
  const dateError = submitted && !hasValidDate
    ? "Choose today or an earlier date."
    : undefined;
  const methodError = submitted && !method.trim() ? "Choose a payment method." : undefined;
  const busy = record.isPending || reconcile.isPending || checkStatus.isPending;
  const resultIsFinal = result?.status === "Posted" || result?.status === "Failed";

  function reset() {
    setStudent(null);
    setInvoiceNumber("");
    setAmount("");
    setPaymentDate(localToday());
    setMethod("Cash");
    setReference("");
    setSubmitted(false);
    setResult(null);
    record.reset();
    reconcile.reset();
    checkStatus.reset();
    attempt.current = null;
  }

  function close() {
    if (busy) return;
    onClose();
  }

  function submit(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setSubmitted(true);
    if (!student || !hasValidAmount || !hasValidDate || !method.trim()) return;

    const body = {
      studentNumber: student.studentNumber ?? "",
      invoiceNumber: invoiceNumber || null,
      amount: amountValue,
      paymentDate,
      method: method.trim(),
      reference: reference.trim() || null,
    };
    const fingerprint = JSON.stringify(body);
    if (!attempt.current || attempt.current.fingerprint !== fingerprint) {
      attempt.current = { fingerprint, requestId: newRequestId() };
    }

    record.mutate(
      { requestId: attempt.current.requestId, ...body },
      {
        onSuccess: (payment) => {
          setResult(payment);
          reconcile.reset();
        },
      },
    );
  }

  function reconcilePayment() {
    if (!result) return;
    if (result.status === "Pending")
      checkStatus.mutate(result.requestId, { onSuccess: (payment) => setResult(payment) });
    else reconcile.mutate(result.requestId, { onSuccess: (payment) => setResult(payment) });
  }

  function finish() {
    reset();
    onClose();
  }

  return (
    <Modal
      open={open}
      onClose={close}
      title={result ? "Payment status" : "Record a payment"}
      description={
        result
          ? "The payment request has been saved. Check its current Business Central status below."
          : "Record money received from a student. The payment will be posted to Business Central."
      }
      footer={
        result ? (
          <>
            {!resultIsFinal && (
              <Button
                variant="secondary"
                loading={reconcile.isPending || checkStatus.isPending}
                onClick={reconcilePayment}
              >
                {result.status === "Pending" ? "Refresh status" : "Reconcile payment"}
              </Button>
            )}
            <Button variant="accent" onClick={finish} disabled={busy}>
              {result.status === "Posted" ? "Done" : "Close"}
            </Button>
          </>
        ) : (
          <>
            <Button variant="ghost" onClick={close} disabled={busy}>
              Cancel
            </Button>
            <Button
              variant="accent"
              type="submit"
              form="record-payment-form"
              loading={record.isPending}
              disabled={Boolean(studentError || amountError || dateError || methodError)}
            >
              Record payment
            </Button>
          </>
        )
      }
    >
      {result ? (
        <div className="space-y-4">
          <Alert tone={result.status === "Posted" ? "success" : result.status === "Failed" ? "error" : "info"}>
            <p className="font-semibold">
              {result.status === "Posted"
                ? "Payment posted"
                : result.status === "Failed"
                  ? "Payment was not posted"
                  : result.status === "Pending"
                    ? "Payment is still processing"
                    : "Payment outcome is not confirmed"}
            </p>
            <p className="mt-1">{result.message || "Business Central confirmed the payment."}</p>
          </Alert>
          <dl className="grid grid-cols-2 gap-3 text-sm">
            <div>
              <dt className="text-muted">Student number</dt>
              <dd className="font-medium text-navy-900">{result.studentNumber}</dd>
            </div>
            <div>
              <dt className="text-muted">Amount</dt>
              <dd className="font-medium text-navy-900">{formatMoney(result.amount)}</dd>
            </div>
            {result.invoiceNumber && (
              <div>
                <dt className="text-muted">Invoice</dt>
                <dd className="font-medium text-navy-900">{result.invoiceNumber}</dd>
              </div>
            )}
            {result.receiptNumber && (
              <div>
                <dt className="text-muted">Receipt</dt>
                <dd className="font-medium text-navy-900">
                  {result.status === "Posted" ? (
                    <Link
                      className="text-forest-700 underline-offset-2 hover:underline"
                      href={`/officer/receipts/${encodeURIComponent(result.receiptNumber)}?studentNumber=${encodeURIComponent(result.studentNumber)}`}
                    >
                      {result.receiptNumber}
                    </Link>
                  ) : (
                    result.receiptNumber
                  )}
                </dd>
              </div>
            )}
          </dl>
          {reconcile.error && <Alert tone="error">{reconcile.error.message}</Alert>}
          {checkStatus.error && <Alert tone="error">{checkStatus.error.message}</Alert>}
        </div>
      ) : (
        <form id="record-payment-form" onSubmit={submit} className="space-y-4">
          {record.error && <Alert tone="error">{record.error.message}</Alert>}

          <StudentSearch
            value={student}
            error={studentError}
            onChange={(selected) => {
              setStudent(selected);
              setInvoiceNumber("");
            }}
          />

          <Select
            label="Apply to invoice (optional)"
            value={invoiceNumber}
            disabled={!student || invoicesQuery.isPending || invoicesQuery.isError}
            hint={
              !student
                ? "Choose a student first, or leave this blank to record an on-account payment."
                : invoicesQuery.isPending
                  ? "Loading open invoices…"
                  : "Leave blank to record an on-account payment."
            }
            onChange={(event) => setInvoiceNumber(event.target.value)}
          >
            <option value="">On-account payment</option>
            {openInvoices.map((invoice) => (
              <option key={invoice.bcInvoiceId} value={invoice.invoiceNumber ?? ""}>
                {invoiceLabel(invoice)}
              </option>
            ))}
          </Select>
          {invoicesQuery.isError && (
            <Alert tone="error">
              Could not load this student&apos;s open invoices: {invoicesQuery.error.message}
            </Alert>
          )}
          {student && !invoicesQuery.isPending && !invoicesQuery.isError && openInvoices.length === 0 && (
            <p className="-mt-2 text-sm text-muted">
              No open invoices found. You can still record an on-account payment.
            </p>
          )}

          <div className="grid gap-4 sm:grid-cols-2">
            <Input
              label="Amount received"
              type="number"
              required
              min="0.01"
              max={selectedInvoice?.amountRemaining ?? 100_000_000}
              step="0.01"
              inputMode="decimal"
              value={amount}
              error={amountError}
              onChange={(event) => setAmount(event.target.value)}
            />
            <Input
              label="Payment date"
              type="date"
              required
              max={localToday()}
              value={paymentDate}
              error={dateError}
              onChange={(event) => setPaymentDate(event.target.value)}
            />
          </div>

          <div className="grid gap-4 sm:grid-cols-2">
            <Select
              label="Payment method"
              value={method}
              error={methodError}
              onChange={(event) => setMethod(event.target.value)}
            >
              <option value="Cash">Cash</option>
              <option value="Bank">Bank</option>
              <option value="Cheque">Cheque</option>
            </Select>
            <Input
              label="Reference (optional)"
              maxLength={60}
              placeholder="Bank slip, cheque number…"
              value={reference}
              onChange={(event) => setReference(event.target.value)}
            />
          </div>

          {selectedInvoice && amount && Number.isFinite(amountValue) && (
            <p className="text-sm text-muted">
              Remaining after payment:{" "}
              <span className="font-medium text-navy-900">
                {formatMoney(
                  Math.max(0, (selectedInvoice.amountRemaining ?? 0) - amountValue),
                  selectedInvoice.currencyCode,
                )}
              </span>
            </p>
          )}
        </form>
      )}
    </Modal>
  );
}
