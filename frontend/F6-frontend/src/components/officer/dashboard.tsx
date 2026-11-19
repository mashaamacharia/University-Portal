"use client";

import Link from "next/link";
import { useIsFetching, useQueryClient } from "@tanstack/react-query";
import { AlertTriangle, FilePenLine, RefreshCw, Users, Wallet } from "lucide-react";
import { PageHeader } from "@/components/page-header";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader } from "@/components/ui/card";
import { EmptyState } from "@/components/ui/empty-state";
import { ErrorState } from "@/components/ui/error-state";
import { Skeleton } from "@/components/ui/skeleton";
import { StatCard } from "@/components/ui/stat-card";
import {
  officerKeys,
  useOfficerDashboard,
  useOfficerPayments,
  useOutstandingBalances,
} from "@/lib/api/hooks/officer";
import { formatDate, formatDateTime, formatMoney, formatNumber } from "@/lib/format";
import { list } from "@/lib/utils";
import { AgingChart } from "./aging-chart";
import { OfficerPaymentTable } from "./officer-payment-table";

function viewAll(href: string) {
  return (
    <Link
      href={href}
      className="text-sm font-medium text-navy-700 underline-offset-4 hover:underline"
    >
      View all
    </Link>
  );
}

function CardSkeleton({ className }: { className?: string }) {
  return <Skeleton className={className ?? "h-72 rounded-xl"} />;
}

export function OfficerDashboard() {
  const queryClient = useQueryClient();
  const refreshing = useIsFetching({ queryKey: officerKeys.all }) > 0;

  const dashboard = useOfficerDashboard();
  const overdue = useOutstandingBalances({ OverdueOnly: true, Page: 1, PageSize: 5 });
  const payments = useOfficerPayments({ Page: 1, PageSize: 5 });

  const data = dashboard.data;

  return (
    <>
      <PageHeader
        title="Finance overview"
        description={
          data
            ? `As of ${formatDate(data.asOfDate)} · updated ${formatDateTime(data.generatedAtUtc)}`
            : "How fees stand across the university."
        }
        actions={
          <Button
            variant="secondary"
            loading={refreshing}
            onClick={() => queryClient.invalidateQueries({ queryKey: officerKeys.all })}
          >
            {!refreshing && <RefreshCw className="size-4" aria-hidden />}
            Refresh
          </Button>
        }
      />

      {/* KPI cards */}
      {dashboard.isPending ? (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          {[0, 1, 2, 3].map((i) => (
            <CardSkeleton key={i} className="h-28 rounded-xl" />
          ))}
        </div>
      ) : dashboard.isError ? (
        <Card>
          <ErrorState error={dashboard.error} onRetry={() => dashboard.refetch()} />
        </Card>
      ) : (
        <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
          <StatCard
            label="Total outstanding"
            value={formatMoney(data!.totalOutstanding)}
            hint="Unpaid across all students"
            icon={Wallet}
          />
          <StatCard
            label="Students with a balance"
            value={formatNumber(data!.studentsWithBalance)}
            icon={Users}
            tone="navy"
          />
          <StatCard
            label="Students overdue"
            value={formatNumber(data!.studentsWithOverdue)}
            hint={data!.studentsWithOverdue > 0 ? "Past their due date" : "Nobody is overdue"}
            icon={AlertTriangle}
            tone={data!.studentsWithOverdue > 0 ? "red" : "green"}
          />
          <StatCard
            label="Invoices awaiting action"
            value={formatNumber(data!.draftInvoices + data!.invoicesInReview)}
            hint={`${formatNumber(data!.draftInvoices)} drafts · ${formatNumber(data!.invoicesInReview)} in review`}
            icon={FilePenLine}
            tone="amber"
          />
        </div>
      )}

      <div className="mt-6 grid gap-6 lg:grid-cols-2">
        {/* Aging */}
        <Card>
          <CardHeader
            title="How old is the debt"
            description="Outstanding balances grouped by age."
          />
          <CardContent>
            {dashboard.isPending ? (
              <CardSkeleton className="h-56" />
            ) : dashboard.isError ? (
              <ErrorState error={dashboard.error} onRetry={() => dashboard.refetch()} />
            ) : (
              <AgingChart buckets={list(data!.aging)} />
            )}
          </CardContent>
        </Card>

        {/* Overdue students */}
        <Card>
          <CardHeader
            title="Students to follow up"
            description="Overdue balances."
            action={viewAll("/officer/outstanding?overdue=1")}
          />
          {overdue.isPending ? (
            <div className="space-y-3 p-5">
              {[0, 1, 2, 3].map((i) => (
                <Skeleton key={i} className="h-10" />
              ))}
            </div>
          ) : overdue.isError ? (
            <ErrorState error={overdue.error} onRetry={() => overdue.refetch()} />
          ) : list(overdue.data.items).length === 0 ? (
            <EmptyState
              title="Nobody is overdue"
              description="Students with overdue balances will appear here."
            />
          ) : (
            <ul className="divide-y divide-line">
              {list(overdue.data.items).map((student) => (
                <li
                  key={student.studentNumber}
                  className="flex items-center justify-between gap-4 px-5 py-3"
                >
                  <div className="min-w-0">
                    <p className="truncate text-sm font-medium text-navy-900">
                      {student.studentName || "-"}
                    </p>
                    <p className="text-xs text-muted">{student.studentNumber}</p>
                  </div>
                  <p className="money shrink-0 text-sm font-medium text-danger-700">
                    {formatMoney(student.overdueAmount)}
                  </p>
                </li>
              ))}
            </ul>
          )}
        </Card>
      </div>

      {/* Latest payments */}
      <Card className="mt-6">
        <CardHeader title="Latest payments" action={viewAll("/officer/payments")} />
        {payments.isPending ? (
          <div className="space-y-3 p-5">
            {[0, 1, 2].map((i) => (
              <Skeleton key={i} className="h-10" />
            ))}
          </div>
        ) : payments.isError ? (
          <ErrorState error={payments.error} onRetry={() => payments.refetch()} />
        ) : (
          <OfficerPaymentTable payments={list(payments.data.items)} />
        )}
      </Card>
    </>
  );
}
