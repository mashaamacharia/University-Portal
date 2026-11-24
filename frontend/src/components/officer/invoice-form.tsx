"use client";

import { useRef, useState } from "react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { Plus, Trash2 } from "lucide-react";
import { PageHeader } from "@/components/page-header";
import { Alert } from "@/components/ui/alert";
import { Button, buttonVariants } from "@/components/ui/button";
import { Card, CardContent, CardHeader } from "@/components/ui/card";
import { Input } from "@/components/ui/input";
import { Select } from "@/components/ui/select";
import { ApiError } from "@/lib/api";
import { useCreateInvoice } from "@/lib/api/hooks/officer";
import { formatMoney } from "@/lib/format";
import { newId } from "@/lib/utils";
import type {
  CreateInvoiceRequest,
  InvoiceLineType,
  StudentLookup,
} from "@/types";
import { StudentSearch } from "./student-search";

type LineDraft = {
  key: string;
  lineType: InvoiceLineType;
  number: string;
  description: string;
  quantity: string;
  unitPrice: string;
};

type LineErrors = { number?: string; quantity?: string; unitPrice?: string };

const emptyLine = (): LineDraft => ({
  key: newId(),
  lineType: "Item",
  number: "",
  description: "",
  quantity: "1",
  unitPrice: "",
});

function validateLine(line: LineDraft): LineErrors {
  const errors: LineErrors = {};
  if (!line.number.trim()) errors.number = "Enter the item or account number.";
  const qty = Number(line.quantity);
  if (line.quantity.trim() === "" || Number.isNaN(qty) || qty <= 0)
    errors.quantity = "Must be more than 0.";
  if (line.unitPrice.trim() !== "") {
    const price = Number(line.unitPrice);
    if (Number.isNaN(price) || price < 0) errors.unitPrice = "Can't be negative.";
  }
  return errors;
}

export function InvoiceForm() {
  const router = useRouter();
  const create = useCreateInvoice();

  const [student, setStudent] = useState<StudentLookup | null>(null);
  const [invoiceDate, setInvoiceDate] = useState("");
  const [lines, setLines] = useState<LineDraft[]>(() => [emptyLine()]);
  const [submitted, setSubmitted] = useState(false);

  // One idempotency key per distinct submission: a retry of the SAME payload reuses
  // it (so the API can't create the invoice twice); an edited payload gets a new key.
  const attempt = useRef<{ key: string; fingerprint: string } | null>(null);

  const lineErrors = lines.map(validateLine);
  const studentError = !student ? "Choose a student." : undefined;
  const hasErrors = !!studentError || lineErrors.some((e) => Object.keys(e).length > 0);

  function updateLine(key: string, patch: Partial<LineDraft>) {
    setLines((current) => current.map((l) => (l.key === key ? { ...l, ...patch } : l)));
  }

  const pricedTotal = lines.reduce((sum, l) => {
    const qty = Number(l.quantity);
    const price = Number(l.unitPrice);
    return l.unitPrice.trim() !== "" && !Number.isNaN(qty) && !Number.isNaN(price)
      ? sum + qty * price
      : sum;
  }, 0);
  const unpriced = lines.filter((l) => l.unitPrice.trim() === "").length;

  function submit() {
    setSubmitted(true);
    if (hasErrors || !student) return;

    const body: CreateInvoiceRequest = {
      studentNumber: student.studentNumber ?? "",
      invoiceDate: invoiceDate || null,
      lines: lines.map((l) => ({
        lineType: l.lineType,
        number: l.number.trim(),
        quantity: Number(l.quantity),
        unitPrice: l.unitPrice.trim() === "" ? null : Number(l.unitPrice),
        description: l.description.trim() || null,
      })),
    };

    const fingerprint = JSON.stringify(body);
    if (!attempt.current || attempt.current.fingerprint !== fingerprint) {
      attempt.current = { key: newId().replaceAll("-", ""), fingerprint };
    }

    create.mutate(
      { body, idempotencyKey: attempt.current.key },
      {
        onSuccess: (invoice) => {
          attempt.current = null;
          router.push(`/officer/invoices/${invoice.bcInvoiceId}`);
        },
      },
    );
  }

  const apiError = create.error instanceof ApiError ? create.error : null;
  const fieldMessages = apiError ? Object.values(apiError.fieldErrors).flat().slice(0, 6) : [];

  return (
    <>
      <PageHeader
        title="New invoice"
        description="This creates a draft. Nothing reaches the student until you post it."
      />

      <form
        noValidate
        onSubmit={(e) => {
          e.preventDefault();
          submit();
        }}
        className="space-y-6"
      >
        {create.error && (
          <Alert tone="error">
            <p>{create.error.message}</p>
            {fieldMessages.length > 1 && (
              <ul className="mt-1 list-disc pl-5">
                {fieldMessages.map((message, i) => (
                  <li key={i}>{message}</li>
                ))}
              </ul>
            )}
          </Alert>
        )}

        <Card>
          <CardHeader title="Who is it for?" />
          <CardContent className="grid gap-5 sm:grid-cols-2">
            <StudentSearch
              value={student}
              onChange={setStudent}
              error={submitted ? studentError : undefined}
            />
            <Input
              label="Invoice date"
              type="date"
              hint="Leave blank to use Business Central's default invoice date."
              value={invoiceDate}
              onChange={(e) => setInvoiceDate(e.target.value)}
            />
          </CardContent>
        </Card>

        <Card>
          <CardHeader
            title="What is being charged?"
            description="Each line is an item or a general-ledger account."
            action={
              <Button variant="secondary" size="sm" onClick={() => setLines((c) => [...c, emptyLine()])}>
                <Plus className="size-4" aria-hidden />
                Add line
              </Button>
            }
          />
          <CardContent className="space-y-4">
            {lines.map((line, index) => {
              const errors = submitted ? lineErrors[index] : {};
              return (
                <fieldset key={line.key} className="rounded-xl border border-line p-4">
                  <legend className="px-1 text-sm font-medium text-navy-900">Line {index + 1}</legend>
                  <div className="grid gap-4 sm:grid-cols-6">
                    <div className="sm:col-span-2">
                      <Select
                        label="Type"
                        value={line.lineType}
                        onChange={(e) =>
                          updateLine(line.key, { lineType: e.target.value as InvoiceLineType })
                        }
                      >
                        <option value="Item">Item</option>
                        <option value="Account">Account</option>
                      </Select>
                    </div>
                    <div className="sm:col-span-4">
                      <Input
                        label={line.lineType === "Item" ? "Item number" : "Account number"}
                        value={line.number}
                        error={errors.number}
                        onChange={(e) => updateLine(line.key, { number: e.target.value })}
                      />
                    </div>
                    <div className="sm:col-span-2">
                      <Input
                        label="Quantity"
                        type="number"
                        inputMode="decimal"
                        min={0}
                        step="any"
                        value={line.quantity}
                        error={errors.quantity}
                        onChange={(e) => updateLine(line.key, { quantity: e.target.value })}
                      />
                    </div>
                    <div className="sm:col-span-4">
                      <Input
                        label="Unit price"
                        type="number"
                        inputMode="decimal"
                        min={0}
                        step="any"
                        placeholder="Leave blank to use the standard price"
                        value={line.unitPrice}
                        error={errors.unitPrice}
                        onChange={(e) => updateLine(line.key, { unitPrice: e.target.value })}
                      />
                    </div>
                    <div className="sm:col-span-6">
                      <Input
                        label="Description (optional)"
                        value={line.description}
                        onChange={(e) => updateLine(line.key, { description: e.target.value })}
                      />
                    </div>
                  </div>
                  {lines.length > 1 && (
                    <div className="mt-3 flex justify-end">
                      <Button
                        variant="ghost"
                        size="sm"
                        onClick={() => setLines((c) => c.filter((l) => l.key !== line.key))}
                      >
                        <Trash2 className="size-4" aria-hidden />
                        Remove line
                      </Button>
                    </div>
                  )}
                </fieldset>
              );
            })}

            <p className="money text-right text-sm text-muted">
              Priced lines total{" "}
              <span className="font-medium text-navy-900">{formatMoney(pricedTotal)}</span>
              {unpriced > 0 && ` · ${unpriced} line${unpriced > 1 ? "s" : ""} priced by the system`}
            </p>
          </CardContent>
        </Card>

        <div className="flex flex-wrap justify-end gap-3">
          <Link href="/officer/invoices" className={buttonVariants({ variant: "ghost" })}>
            Cancel
          </Link>
          <Button type="submit" variant="accent" loading={create.isPending}>
            Create draft invoice
          </Button>
        </div>
      </form>
    </>
  );
}
