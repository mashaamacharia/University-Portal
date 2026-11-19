import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { officerApi } from "@/lib/api";
import type { OfficerPaymentsQuery, OutstandingBalancesQuery } from "@/types";

export const officerKeys = {
  all: ["officer"] as const,
  dashboard: () => ["officer", "dashboard"] as const,
  outstanding: (query?: OutstandingBalancesQuery) =>
    ["officer", "outstanding", query] as const,
  payments: (query?: OfficerPaymentsQuery) =>
    ["officer", "payments", query] as const,
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
