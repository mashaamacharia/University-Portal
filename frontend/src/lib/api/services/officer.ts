import { apiFetch } from "../client";
import type {
  CreateInvoiceRequest,
  OfficerDashboard,
  OfficerInvoiceDetail,
  OfficerInvoicePage,
  OfficerInvoicesQuery,
  OfficerPaymentPage,
  OfficerPaymentsQuery,
  OutstandingBalancePage,
  OutstandingBalancesQuery,
  StudentLookup,
} from "@/types";

const base = "/api/officer/finance";

export const officerApi = {
  dashboard: () => apiFetch<OfficerDashboard>(`${base}/dashboard`),

  outstandingBalances: (query?: OutstandingBalancesQuery) =>
    apiFetch<OutstandingBalancePage>(`${base}/outstanding-balances`, { query }),

  payments: (query?: OfficerPaymentsQuery) =>
    apiFetch<OfficerPaymentPage>(`${base}/payments`, { query }),

  invoices: (query?: OfficerInvoicesQuery) =>
    apiFetch<OfficerInvoicePage>(`${base}/invoices`, { query }),

  /** Student search shared by invoice creation and finance list filters. */
  searchStudents: (search: string) =>
    apiFetch<StudentLookup[]>(`${base}/students`, { query: { Search: search } }),

  invoice: (bcInvoiceId: string) =>
    apiFetch<OfficerInvoiceDetail>(
      `${base}/invoices/${encodeURIComponent(bcInvoiceId)}`,
    ),

  /**
   * Generate one idempotencyKey per form submission (crypto.randomUUID()) and
   * reuse it if the user retries the same submission.
   */
  createInvoice: (body: CreateInvoiceRequest, idempotencyKey: string) =>
    apiFetch<OfficerInvoiceDetail>(`${base}/invoices`, {
      method: "POST",
      body,
      headers: { "Idempotency-Key": idempotencyKey },
    }),

  postInvoice: (bcInvoiceId: string) =>
    apiFetch<OfficerInvoiceDetail>(`${base}/invoices/${bcInvoiceId}/post`, {
      method: "POST",
    }),

  discardInvoice: (bcInvoiceId: string) =>
    apiFetch<void>(`${base}/invoices/${bcInvoiceId}`, { method: "DELETE" }),
};
