import { apiFetch } from "../client";
import type {
  BalanceSummary,
  FeeStatement,
  InvoiceDetail,
  InvoiceSummary,
  PaymentHistoryPage,
  Receipt,
  StudentPaymentsQuery,
} from "@/types";

const base = "/api/student/finance";

export const studentApi = {
  statement: (top?: number) =>
    apiFetch<FeeStatement>(`${base}/statement`, { query: { top } }),

  balance: () => apiFetch<BalanceSummary>(`${base}/balance`),

  invoices: (top?: number) =>
    apiFetch<InvoiceSummary[]>(`${base}/invoices`, { query: { top } }),

  invoice: (invoiceNumber: string) =>
    apiFetch<InvoiceDetail>(
      `${base}/invoices/${encodeURIComponent(invoiceNumber)}`,
    ),

  payments: (query?: StudentPaymentsQuery) =>
    apiFetch<PaymentHistoryPage>(`${base}/payments`, { query }),

  receipt: (receiptNumber: string) =>
    apiFetch<Receipt>(`${base}/receipts/${encodeURIComponent(receiptNumber)}`),

  printReceipt: (receiptNumber: string) =>
    apiFetch<Receipt>(`${base}/receipts/${encodeURIComponent(receiptNumber)}/print`, {
      method: "POST",
    }),
};
