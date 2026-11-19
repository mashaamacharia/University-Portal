"use client";

import { Printer } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader } from "@/components/ui/card";
import { ErrorState } from "@/components/ui/error-state";
import { Skeleton } from "@/components/ui/skeleton";
import { useStatement } from "@/lib/api/hooks/student";
import { formatDateTime, formatMoney } from "@/lib/format";
import { list } from "@/lib/utils";
import { InvoiceTable } from "./invoice-table";
import { PaymentTable } from "./payment-table";

export function StatementView() {
  const query = useStatement(100);

  if (query.isPending) return <Skeleton className="h-[32rem] rounded-xl" />;
  if (query.isError) {
    return (
      <Card>
        <ErrorState error={query.error} onRetry={() => query.refetch()} />
      </Card>
    );
  }

  const { student, balance, generatedAtUtc } = query.data;
  const invoices = list(query.data.invoices);
  const payments = list(query.data.recentPayments);

  return (
    <div className="space-y-6">
      <div className="flex flex-wrap items-start justify-between gap-4">
        <div>
          <h1 className="font-serif text-3xl font-semibold text-navy-900">Fee statement</h1>
          <p className="mt-1 text-muted">Generated {formatDateTime(generatedAtUtc)}</p>
        </div>
        <Button variant="secondary" onClick={() => window.print()} className="print:hidden">
          <Printer className="size-4" aria-hidden />
          Print
        </Button>
      </div>

      <Card>
        <CardContent>
          <dl className="grid gap-x-6 gap-y-4 sm:grid-cols-3">
            <div>
              <dt className="text-sm text-muted">Student</dt>
              <dd className="font-medium text-navy-900">{student.name || "-"}</dd>
              <dd className="text-sm text-muted">{student.studentNumber}</dd>
            </div>
            <div>
              <dt className="text-sm text-muted">Current balance</dt>
              <dd className="money font-serif text-2xl font-semibold text-navy-900">
                {formatMoney(balance.balance, balance.currencyCode)}
              </dd>
            </div>
            <div>
              <dt className="text-sm text-muted">Overdue</dt>
              <dd
                className={`money font-serif text-2xl font-semibold ${
                  balance.overdueAmount > 0 ? "text-danger-700" : "text-forest-700"
                }`}
              >
                {formatMoney(balance.overdueAmount, balance.currencyCode)}
              </dd>
            </div>
          </dl>
        </CardContent>
      </Card>

      <Card>
        <CardHeader title="Invoices" />
        <InvoiceTable invoices={invoices} />
      </Card>

      <Card className="break-inside-avoid">
        <CardHeader title="Payments" />
        <PaymentTable payments={payments} />
      </Card>
    </div>
  );
}
