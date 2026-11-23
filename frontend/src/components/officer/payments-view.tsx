"use client";

import { useState } from "react";
import { PageHeader } from "@/components/page-header";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { ErrorState } from "@/components/ui/error-state";
import { Input } from "@/components/ui/input";
import { Pagination } from "@/components/ui/pagination";
import { Skeleton } from "@/components/ui/skeleton";
import { useOfficerPayments } from "@/lib/api/hooks/officer";
import { list } from "@/lib/utils";
import type { StudentLookup } from "@/types";
import { OfficerPaymentTable } from "./officer-payment-table";
import { StudentSearch } from "./student-search";

const PAGE_SIZE = 20;

export function OfficerPaymentsView() {
  const [student, setStudent] = useState<StudentLookup | null>(null);
  const [from, setFrom] = useState("");
  const [to, setTo] = useState("");
  const [page, setPage] = useState(1);

  const query = useOfficerPayments({
    CustomerNumber: student?.studentNumber ?? undefined,
    From: from || undefined,
    To: to || undefined,
    Page: page,
    PageSize: PAGE_SIZE,
  });

  const filtered = student !== null || from !== "" || to !== "";

  return (
    <>
      <PageHeader title="Payments" description="Every payment recorded across all students." />
      <Card>
        <CardContent className="flex flex-wrap items-end gap-4 border-b border-line">
          <div className="w-72">
            <StudentSearch
              value={student}
              onChange={(selected) => {
                setStudent(selected);
                setPage(1);
              }}
            />
          </div>
          <div className="w-44">
            <Input
              label="From"
              type="date"
              value={from}
              max={to || undefined}
              onChange={(e) => {
                setFrom(e.target.value);
                setPage(1);
              }}
            />
          </div>
          <div className="w-44">
            <Input
              label="To"
              type="date"
              value={to}
              min={from || undefined}
              onChange={(e) => {
                setTo(e.target.value);
                setPage(1);
              }}
            />
          </div>
          {filtered && (
            <Button
              variant="ghost"
              onClick={() => {
                setStudent(null);
                setFrom("");
                setTo("");
                setPage(1);
              }}
            >
              Clear filters
            </Button>
          )}
        </CardContent>

        {query.isPending ? (
          <div className="space-y-3 p-5">
            {[0, 1, 2, 3, 4].map((i) => (
              <Skeleton key={i} className="h-12" />
            ))}
          </div>
        ) : query.isError ? (
          <ErrorState error={query.error} onRetry={() => query.refetch()} />
        ) : (
          <>
            <OfficerPaymentTable
              payments={list(query.data.items)}
              emptyText={filtered ? "No payments match those filters." : undefined}
            />
            <Pagination
              page={query.data.page}
              pageSize={query.data.pageSize}
              totalCount={query.data.totalCount}
              totalPages={query.data.totalPages}
              onPageChange={setPage}
            />
          </>
        )}
      </Card>
    </>
  );
}
