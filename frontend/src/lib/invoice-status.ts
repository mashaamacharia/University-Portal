import type { InvoiceStatus } from "@/types";

export const INVOICE_STATUSES = [
  "Draft",
  "InReview",
  "Open",
  "Paid",
  "Corrective",
  "Canceled",
] satisfies readonly InvoiceStatus[];

export function isInvoiceStatus(value?: string): value is InvoiceStatus {
  return INVOICE_STATUSES.some((status) => status === value);
}
