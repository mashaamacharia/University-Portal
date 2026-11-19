import {
  keepPreviousData,
  useMutation,
  useQuery,
  useQueryClient,
} from "@tanstack/react-query";
import { officerApi } from "@/lib/api";
import type {
  CreateInvoiceRequest,
  OfficerInvoiceDetail,
  OfficerInvoicesQuery,
  OfficerPaymentsQuery,
  OutstandingBalancesQuery,
} from "@/types";

export const officerKeys = {
  all: ["officer"] as const,
  dashboard: () => ["officer", "dashboard"] as const,
  outstanding: (query?: OutstandingBalancesQuery) =>
    ["officer", "outstanding", query] as const,
  payments: (query?: OfficerPaymentsQuery) =>
    ["officer", "payments", query] as const,
  invoices: (query?: OfficerInvoicesQuery) =>
    ["officer", "invoices", query] as const,
  invoice: (id: string) => ["officer", "invoice", id] as const,
  students: (search: string) => ["officer", "students", search] as const,
};

export function useOfficerDashboard() {
  return useQuery({
    queryKey: officerKeys.dashboard(),
    queryFn: () => officerApi.dashboard(),
  });
}

export function useOutstandingBalances(query: OutstandingBalancesQuery) {
  return useQuery({
    queryKey: officerKeys.outstanding(query),
    queryFn: () => officerApi.outstandingBalances(query),
    placeholderData: keepPreviousData,
  });
}

export function useOfficerPayments(query: OfficerPaymentsQuery) {
  return useQuery({
    queryKey: officerKeys.payments(query),
    queryFn: () => officerApi.payments(query),
    placeholderData: keepPreviousData,
  });
}

export function useOfficerInvoices(query: OfficerInvoicesQuery) {
  return useQuery({
    queryKey: officerKeys.invoices(query),
    queryFn: () => officerApi.invoices(query),
    placeholderData: keepPreviousData,
  });
}

export function useOfficerInvoice(id: string) {
  return useQuery({
    queryKey: officerKeys.invoice(id),
    queryFn: () => officerApi.invoice(id),
  });
}

/** Student picker: only searches once there are at least 2 characters. */
export function useStudentSearch(search: string) {
  return useQuery({
    queryKey: officerKeys.students(search),
    queryFn: () => officerApi.searchStudents(search),
    enabled: search.length >= 2,
    staleTime: 60_000,
  });
}

export function useCreateInvoice() {
  const queryClient = useQueryClient();
  return useMutation<
    OfficerInvoiceDetail,
    Error,
    { body: CreateInvoiceRequest; idempotencyKey: string }
  >({
    mutationFn: ({ body, idempotencyKey }) =>
      officerApi.createInvoice(body, idempotencyKey),
    onSuccess: (invoice) => {
      queryClient.setQueryData(officerKeys.invoice(invoice.bcInvoiceId), invoice);
      queryClient.invalidateQueries({ queryKey: officerKeys.all });
    },
  });
}

export function usePostInvoice() {
  const queryClient = useQueryClient();
  return useMutation<OfficerInvoiceDetail, Error, string>({
    mutationFn: (id) => officerApi.postInvoice(id),
    onSuccess: (invoice, id) => {
      queryClient.setQueryData(officerKeys.invoice(id), invoice);
      queryClient.invalidateQueries({ queryKey: officerKeys.all });
    },
  });
}

export function useDiscardInvoice() {
  const queryClient = useQueryClient();
  return useMutation<void, Error, string>({
    mutationFn: (id) => officerApi.discardInvoice(id),
    onSuccess: (_data, id) => {
      queryClient.removeQueries({ queryKey: officerKeys.invoice(id) });
      queryClient.invalidateQueries({ queryKey: officerKeys.all });
    },
  });
}
