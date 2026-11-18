import type { components } from "@/types/api";
import { Badge, type BadgeTone } from "./badge";

type InvoiceStatus = components["schemas"]["InvoiceStatus"];
type PaymentStatus = components["schemas"]["PaymentStatus"];

const invoiceStatus: Record<InvoiceStatus, { label: string; tone: BadgeTone }> =
  {
    Draft: { label: "Draft", tone: "neutral" },
    InReview: { label: "In review", tone: "amber" },
    Open: { label: "Open", tone: "navy" },
    Paid: { label: "Paid", tone: "green" },
    Corrective: { label: "Corrective", tone: "amber" },
    Canceled: { label: "Canceled", tone: "red" },
  };

const paymentStatus: Record<PaymentStatus, { label: string; tone: BadgeTone }> =
  {
    Unpaid: { label: "Unpaid", tone: "navy" },
    PartiallyPaid: { label: "Partly paid", tone: "amber" },
    Paid: { label: "Paid", tone: "green" },
    Overdue: { label: "Overdue", tone: "red" },
  };

export function InvoiceStatusBadge({ status }: { status?: InvoiceStatus }) {
  if (!status) return null;
  const { label, tone } = invoiceStatus[status];
  return <Badge tone={tone}>{label}</Badge>;
}

export function PaymentStatusBadge({ status }: { status?: PaymentStatus }) {
  if (!status) return null;
  const { label, tone } = paymentStatus[status];
  return <Badge tone={tone}>{label}</Badge>;
}
