"use client";

import { useState } from "react";
import { PageHeader } from "@/components/page-header";
import { Card, CardContent } from "@/components/ui/card";
import { ErrorState } from "@/components/ui/error-state";
import { FilterTabs } from "@/components/ui/filter-tabs";
import { Skeleton } from "@/components/ui/skeleton";
import { useStudentInvoices } from "@/lib/api/hooks/student";
import { list } from "@/lib/utils";
import { InvoiceTable } from "./invoice-table";

type Filter = "all" | "outstanding" | "overdue" | "paid";

export function InvoicesView() {
  const query = useStudentInvoices(100);
  const [filter, setFilter] = useState<Filter>("all");

  const header = (
    <PageHeader title="Invoices" description="Everything that has been billed to you." />
  );

  if (query.isPending) {
    return (
      <>
        {header}
        <Skeleton className="h-80 rounded-xl" />
      </>
    );
  }
  if (query.isError) {
    return (
      <>
        {header}
        <Card>
          <ErrorState error={query.error} onRetry={() => query.refetch()} />
        </Card>
      </>
    );
  }

  const invoices = list(query.data);
  const matches = {
    all: invoices,
    outstanding: invoices.filter((i) => i.amountRemaining > 0),
    overdue: invoices.filter((i) => i.paymentStatus === "Overdue"),
    paid: invoices.filter((i) => i.paymentStatus === "Paid"),
  };

  return (
    <>
      {header}
      <Card>
        <CardContent className="border-b border-line">
          <FilterTabs
            label="Filter invoices"
            value={filter}
            onChange={setFilter}
            options={[
              { value: "all", label: "All", count: matches.all.length },
              { value: "outstanding", label: "Outstanding", count: matches.outstanding.length },
              { value: "overdue", label: "Overdue", count: matches.overdue.length },
              { value: "paid", label: "Paid", count: matches.paid.length },
            ]}
          />
        </CardContent>
        <InvoiceTable invoices={matches[filter]} />
      </Card>
    </>
  );
}
