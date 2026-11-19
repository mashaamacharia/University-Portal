"use client";

import Link from "next/link";
import { AlertTriangle, CalendarClock, ScrollText, Wallet } from "lucide-react";
import { useAuth } from "@/components/auth/auth-provider";
import { PageHeader } from "@/components/page-header";
import { Alert } from "@/components/ui/alert";
import { buttonVariants } from "@/components/ui/button";
import { Card, CardHeader } from "@/components/ui/card";
import { ErrorState } from "@/components/ui/error-state";
import { Skeleton } from "@/components/ui/skeleton";
import { StatCard } from "@/components/ui/stat-card";
import { useStatement } from "@/lib/api/hooks/student";
import { formatDate, formatMoney } from "@/lib/format";
import { list } from "@/lib/utils";
import type { InvoiceSummary } from "@/types";
import { InvoiceTable } from "./invoice-table";
import { PaymentTable } from "./payment-table";

/** Earliest due date among invoices that still have something to pay. */
function nextDue(invoices: InvoiceSummary[]): InvoiceSummary | undefined {
  return invoices
    .filter((invoice) => invoice.amountRemaining > 0)
    .sort((a, b) => a.dueDate.localeCompare(b.dueDate))[0];
}

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

export function StudentDashboard() {
  const { user } = useAuth();
  const statement = useStatement(20);
  const firstName = user.fullName?.split(" ")[0] || "there";

  const header = (
    <PageHeader
      title={`Hello, ${firstName}`}
      description={
        user.studentNumber
          ? `Student no. ${user.studentNumber}`
          : "Here's where your fees stand."
      }
      actions={
        <Link href="/student/statement" className={buttonVariants({ variant: "secondary" })}>
          <ScrollText className="size-4" aria-hidden />
          Fee statement
        </Link>
      }
    />
  );

  if (statement.isPending) {
    return (
      <>
        {header}
        <div className="grid gap-4 sm:grid-cols-3">
          {[0, 1, 2].map((i) => (
            <Skeleton key={i} className="h-28 rounded-xl" />
          ))}
        </div>
        <div className="mt-6 grid gap-6 lg:grid-cols-5">
          <Skeleton className="h-72 rounded-xl lg:col-span-3" />
          <Skeleton className="h-72 rounded-xl lg:col-span-2" />
        </div>
      </>
    );
  }

  if (statement.isError) {
    return (
      <>
        {header}
        <Card>
          <ErrorState error={statement.error} onRetry={() => statement.refetch()} />
        </Card>
      </>
    );
  }

  const { balance } = statement.data;
  const invoices = list(statement.data.invoices);
  const payments = list(statement.data.recentPayments);
  const due = nextDue(invoices);
  const currency = balance.currencyCode;
  const hasOverdue = balance.overdueAmount > 0;

  return (
    <>
      {header}

      {hasOverdue && (
        <Alert tone="error" className="mb-4">
          You have {formatMoney(balance.overdueAmount, currency)} overdue. Please
          settle it with the finance office as soon as you can.
        </Alert>
      )}

      <div className="grid gap-4 sm:grid-cols-3">
        <StatCard
          label="Current balance"
          value={formatMoney(balance.balance, currency)}
          hint={
            balance.balance > 0
              ? "Amount you currently owe"
              : balance.balance < 0
                ? "Credit on your account"
                : "You're all paid up"
          }
          icon={Wallet}
          tone={balance.balance > 0 ? "navy" : "green"}
        />
        <StatCard
          label="Overdue"
          value={formatMoney(balance.overdueAmount, currency)}
          hint={hasOverdue ? "Past the due date" : "Nothing overdue"}
          icon={AlertTriangle}
          tone={hasOverdue ? "red" : "green"}
        />
        <StatCard
          label="Next payment due"
          value={due ? formatDate(due.dueDate) : "None"}
          hint={
            due
              ? `${formatMoney(due.amountRemaining, due.currencyCode)} on ${due.invoiceNumber}`
              : "No open invoices"
          }
          icon={CalendarClock}
          tone="amber"
        />
      </div>

      <div className="mt-6 grid gap-6 lg:grid-cols-5">
        <Card className="lg:col-span-3">
          <CardHeader
            title="Recent invoices"
            action={viewAll("/student/invoices")}
          />
          <InvoiceTable invoices={invoices.slice(0, 5)} compact />
        </Card>
        <Card className="lg:col-span-2">
          <CardHeader title="Recent payments" action={viewAll("/student/payments")} />
          <PaymentTable payments={payments.slice(0, 5)} />
        </Card>
      </div>
    </>
  );
}
