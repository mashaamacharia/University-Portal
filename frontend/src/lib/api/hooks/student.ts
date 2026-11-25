import { keepPreviousData, useMutation, useQuery } from "@tanstack/react-query";
import { studentApi } from "@/lib/api";
import type { Receipt, StudentPaymentsQuery } from "@/types";

export const studentKeys = {
  statement: (top?: number) => ["student", "statement", top] as const,
  invoices: (top?: number) => ["student", "invoices", top] as const,
  invoice: (number: string) => ["student", "invoice", number] as const,
  payments: (query?: StudentPaymentsQuery) =>
    ["student", "payments", query] as const,
  receipt: (receiptNumber: string) =>
    ["student", "receipt", receiptNumber] as const,
};

export function useStatement(top?: number) {
  return useQuery({
    queryKey: studentKeys.statement(top),
    queryFn: () => studentApi.statement(top),
  });
}

export function useStudentInvoices(top?: number) {
  return useQuery({
    queryKey: studentKeys.invoices(top),
    queryFn: () => studentApi.invoices(top),
  });
}

export function useStudentInvoice(invoiceNumber: string) {
  return useQuery({
    queryKey: studentKeys.invoice(invoiceNumber),
    queryFn: () => studentApi.invoice(invoiceNumber),
  });
}

export function useStudentPayments(query: StudentPaymentsQuery) {
  return useQuery({
    queryKey: studentKeys.payments(query),
    queryFn: () => studentApi.payments(query),
    placeholderData: keepPreviousData, // keep the old page on screen while the next loads
  });
}

export function useStudentReceipt(receiptNumber: string, enabled = true) {
  return useQuery({
    queryKey: studentKeys.receipt(receiptNumber),
    queryFn: () => studentApi.receipt(receiptNumber),
    enabled: enabled && Boolean(receiptNumber),
  });
}

export function usePrintStudentReceipt() {
  return useMutation<Receipt, Error, string>({
    mutationFn: (receiptNumber) => studentApi.printReceipt(receiptNumber),
  });
}
