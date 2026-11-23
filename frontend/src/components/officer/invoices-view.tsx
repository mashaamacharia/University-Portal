"use client";

import { useState } from "react";
import Link from "next/link";
import { Plus } from "lucide-react";
import { PageHeader } from "@/components/page-header";
import { Button, buttonVariants } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { ErrorState } from "@/components/ui/error-state";
import { FilterTabs } from "@/components/ui/filter-tabs";
import { Input } from "@/components/ui/input";
import { Pagination } from "@/components/ui/pagination";
import { Skeleton } from "@/components/ui/skeleton";
import { useOfficerInvoices } from "@/lib/api/hooks/officer";
import { INVOICE_STATUSES } from "@/lib/invoice-status";
import { list } from "@/lib/utils";
import type { InvoiceStatus, StudentLookup } from "@/types";
import { OfficerInvoiceTable } from "./officer-invoice-table";
import { StudentSearch } from "./student-search";

const PAGE_SIZE = 20;
type StatusFilter = InvoiceStatus | "all";

const STATUS_LABELS: Record<InvoiceStatus, string> = {
  Draft: "Drafts",
  InReview: "In review",
  Open: "Open",
  Paid: "Paid",
  Corrective: "Corrective",
  Canceled: "Canceled",
};

export function OfficerInvoicesView({ initialStatus }: { initialStatus?: InvoiceStatus }) {
  const [status, setStatus] = useState<StatusFilter>(initialStatus ?? "all");
  const [student, setStudent] = useState<StudentLookup | null>(null);
  const [from, setFrom] = useState("");
  const [to, setTo] = useState("");
  const [page, setPage] = useState(1);

  const query = useOfficerInvoices({
    Status: status === "all" ? undefined : status,
    CustomerNumber: student?.studentNumber ?? undefined,
    From: from || undefined,
    To: to || undefined,
    Page: page,
    PageSize: PAGE_SIZE,
  });

  const filtered = status !== "all" || student !== null || from !== "" || to !== "";

  return (
    <>
      <PageHeader
        title="Invoices"
        description="Create drafts, review them, and post them to the student's account."
        actions={
          <Link href="/officer/invoices/new" className={buttonVariants({ variant: "accent" })}>
            <Plus className="size-4" aria-hidden />
            New invoice
          </Link>
        }
      />

      <Card>
        <CardContent className="space-y-4 border-b border-line">
          <FilterTabs
            label="Invoice status"
            value={status}
            onChange={(value) => {
              setStatus(value);
              setPage(1);
            }}
            options={[
              { value: "all", label: "All" },
              ...INVOICE_STATUSES.map((s) => ({ value: s, label: STATUS_LABELS[s] })),
            ]}
          />
          <div className="flex flex-wrap items-end gap-4">
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
                  setStatus("all");
                  setStudent(null);
                  setFrom("");
                  setTo("");
                  setPage(1);
                }}
              >
                Clear filters
              </Button>
            )}
          </div>
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
            <OfficerInvoiceTable
              invoices={list(query.data.items)}
              emptyText={filtered ? "No invoices match those filters." : undefined}
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
