import { keepPreviousData, useQuery } from "@tanstack/react-query";
import { officerApi } from "@/lib/api";
import type {
  OfficerPaymentsQuery,
  OutstandingBalancesQuery,
  StudentLookupQuery,
} from "@/types";

export const officerKeys = {
  all: ["officer"] as const,
  dashboard: () => ["officer", "dashboard"] as const,
  outstanding: (query?: OutstandingBalancesQuery) =>
    ["officer", "outstanding", query] as const,
  payments: (query?: OfficerPaymentsQuery) =>
    ["officer", "payments", query] as const,
  studentLookup: (query: StudentLookupQuery) =>
    ["officer", "students", query] as const,
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

export function useStudentLookup(search: string) {
  const query = { Search: search.trim() };
  return useQuery({
    queryKey: officerKeys.studentLookup(query),
    queryFn: ({ signal }) => officerApi.searchStudents(query, signal),
    enabled: query.Search.length >= 2,
    staleTime: 30_000,
  });
}
