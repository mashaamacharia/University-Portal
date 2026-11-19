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
import { useDebouncedValue } from "@/lib/hooks/use-debounced-value";
import { list } from "@/lib/utils";
import { OfficerPaymentTable } from "./officer-payment-table";

const PAGE_SIZE = 20;

export function OfficerPaymentsView() {
  const [student, setStudent] = useState("");
  const [from, setFrom] = useState("");
  const [to, setTo] = useState("");
  const [page, setPage] = useState(1);

  const debouncedStudent = useDebouncedValue(student).trim();

  const query = useOfficerPayments({
    CustomerNumber: debouncedStudent || undefined,
    From: from || undefined,
    To: to || undefined,
    Page: page,
    PageSize: PAGE_SIZE,
  });

  const filtered = student !== "" || from !== "" || to !== "";

  return (
    <>
      <PageHeader title="Payments" description="Every payment recorded across all students." />
      <Card>
        <CardContent className="flex flex-wrap items-end gap-4 border-b border-line">
          <div className="w-56">
            <Input
              label="Student number"
              placeholder="e.g. SC211/0001/2022"
              value={student}
              onChange={(e) => {
                setStudent(e.target.value);
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
                setStudent("");
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
