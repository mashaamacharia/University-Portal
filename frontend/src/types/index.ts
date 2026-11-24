import type { components, paths } from "./api";

type S = components["schemas"];

/**
 * openapi-typescript marks every property optional (`x?: T`) because .NET does
 * not emit "required" for value types. Strict removes that noise so components
 * can read `invoice.totalAmount` without `?.`. `| null` is kept on purpose:
 * strings and lists can genuinely be null (use `list()` for lists).
 */
export type Strict<T> = T extends (infer U)[]
  ? Strict<U>[]
  : T extends object
    ? { [K in keyof T]-?: Strict<Exclude<T[K], undefined>> }
    : T;

/** Query-string parameters of a GET endpoint, taken straight from the API types. */
type QueryOf<P extends keyof paths> = paths[P] extends {
  get: { parameters: { query?: infer Q } };
}
  ? NonNullable<Q>
  : never;

// ---- Enums -----------------------------------------------------------------
export type PortalRole = S["PortalRole"];
export type InvoiceStatus = S["InvoiceStatus"];
export type PaymentStatus = S["PaymentStatus"];
export type InvoiceLineType = S["InvoiceLineType"];

// ---- Auth ------------------------------------------------------------------
export type CurrentUser = Strict<S["CurrentUserDto"]>;
export type LoginRequest = S["LoginRequest"];
export type ChangePasswordRequest = S["ChangePasswordRequest"];
/** What our own /api/auth/login route returns. The token never reaches the browser. */
export type LoginResult = { user: CurrentUser; expiresAtUtc: string };

// ---- Student finance -------------------------------------------------------
export type FeeStatement = Strict<S["FeeStatementDto"]>;
export type BalanceSummary = Strict<S["BalanceSummaryDto"]>;
export type InvoiceSummary = Strict<S["InvoiceSummaryDto"]>;
export type InvoiceDetail = Strict<S["InvoiceDetailDto"]>;
export type InvoiceLine = Strict<S["InvoiceLineDto"]>;
export type PaymentHistoryEntry = Strict<S["PaymentHistoryEntryDto"]>;
export type PaymentHistoryPage = Strict<S["PaymentHistoryEntryDtoPagedResult"]>;
export type StudentPaymentsQuery = QueryOf<"/api/student/finance/payments">;

// ---- Officer finance -------------------------------------------------------
export type OfficerDashboard = Strict<S["OfficerDashboardDto"]>;
export type AgingBucket = Strict<S["AgingBucketDto"]>;
/**
 * The list DTO has no bcInvoiceId yet, but the detail, post and delete endpoints
 * need it. Add it to OfficerInvoiceDto on the backend; this intersection keeps the
 * frontend compiling before and after that change.
 */
export type OfficerInvoice = Strict<S["OfficerInvoiceDto"]>;
export type OfficerInvoicePage = Strict<S["OfficerInvoiceDtoPagedResult"]>;
export type StudentLookup = Strict<S["StudentLookupDto"]>;
export type OfficerInvoiceDetail = Strict<S["OfficerInvoiceDetailDto"]>;
export type OfficerPayment = Strict<S["OfficerPaymentDto"]>;
export type OfficerPaymentPage = Strict<S["OfficerPaymentDtoPagedResult"]>;
export type PaymentRequestStatus = "Pending" | "Posted" | "Failed" | "Unknown";
export type RecordPaymentRequest = {
  requestId: string;
  studentNumber: string;
  invoiceNumber: string | null;
  amount: number;
  paymentDate: string;
  method: string;
  reference: string | null;
};
export type PaymentRequestResult = {
  requestId: string;
  status: PaymentRequestStatus;
  receiptNumber: string | null;
  studentNumber: string;
  invoiceNumber: string | null;
  amount: number;
  paymentDate: string;
  method: string;
  message: string | null;
};
export type OutstandingBalance = Strict<S["OutstandingBalanceDto"]>;
export type OutstandingBalancePage = Strict<
  S["OutstandingBalanceDtoPagedResult"]
>;
// Requests keep their generated shape: optional fields there are real.
export type CreateInvoiceRequest = S["CreateInvoiceRequest"];
export type CreateInvoiceLineRequest = S["CreateInvoiceLineRequest"];
export type OfficerInvoicesQuery = QueryOf<"/api/officer/finance/invoices">;
export type OfficerPaymentsQuery = QueryOf<"/api/officer/finance/payments">;
export type OutstandingBalancesQuery =
  QueryOf<"/api/officer/finance/outstanding-balances">;

// ---- Admin -----------------------------------------------------------------
export type UserSummary = Strict<S["UserSummaryDto"]>;
export type UserPage = Strict<S["UserSummaryDtoPagedResult"]>;
export type CreateUserRequest = S["CreateUserRequest"];
export type UsersQuery = QueryOf<"/api/admin/users">;
