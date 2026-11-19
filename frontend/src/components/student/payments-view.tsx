"use client";

import { useState } from "react";
import { PageHeader } from "@/components/page-header";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { ErrorState } from "@/components/ui/error-state";
import { Input } from "@/components/ui/input";
import { Pagination } from "@/components/ui/pagination";
import { Skeleton } from "@/components/ui/skeleton";
import { useStudentPayments } from "@/lib/api/hooks/student";
import { list } from "@/lib/utils";
import { PaymentTable } from "./payment-table";

const PAGE_SIZE = 15;

export function PaymentsView() {
  const [from, setFrom] = useState("");
  const [to, setTo] = useState("");
  const [page, setPage] = useState(1);

  const query = useStudentPayments({
    From: from || undefined,
    To: to || undefined,
    Page: page,
    PageSize: PAGE_SIZE,
  });

  const filtered = from !== "" || to !== "";

  const header = (
    <PageHeader title="Payments" description="Every payment recorded on your account." />
  );

  return (
    <>
      {header}
      <Card>
        <CardContent className="flex flex-wrap items-end gap-4 border-b border-line">
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
                setFrom("");
                setTo("");
                setPage(1);
              }}
            >
              Clear dates
            </Button>
          )}
        </CardContent>

        {query.isPending ? (
          <div className="space-y-3 p-5">
            {[0, 1, 2, 3].map((i) => (
              <Skeleton key={i} className="h-10" />
            ))}
          </div>
        ) : query.isError ? (
          <ErrorState error={query.error} onRetry={() => query.refetch()} />
        ) : (
          <>
            <PaymentTable
              payments={list(query.data.items)}
              emptyText={
                filtered
                  ? "No payments in that date range."
                  : "Payments appear here once they're recorded."
              }
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
