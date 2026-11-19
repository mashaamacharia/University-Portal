"use client";

import { useState } from "react";
import { UserCheck } from "lucide-react";
import { PageHeader } from "@/components/page-header";
import { Card, CardContent } from "@/components/ui/card";
import { EmptyState } from "@/components/ui/empty-state";
import { ErrorState } from "@/components/ui/error-state";
import { FilterTabs } from "@/components/ui/filter-tabs";
import { Input } from "@/components/ui/input";
import { Pagination } from "@/components/ui/pagination";
import { Skeleton } from "@/components/ui/skeleton";
import { Table, TBody, Td, Th, THead, Tr } from "@/components/ui/table";
import { useOutstandingBalances } from "@/lib/api/hooks/officer";
import { cn } from "@/lib/cn";
import { formatMoney } from "@/lib/format";
import { useDebouncedValue } from "@/lib/hooks/use-debounced-value";
import { list } from "@/lib/utils";

const PAGE_SIZE = 20;
type Scope = "all" | "overdue";

export function OutstandingView({ initialOverdueOnly = false }: { initialOverdueOnly?: boolean }) {
  const [scope, setScope] = useState<Scope>(initialOverdueOnly ? "overdue" : "all");
  const [minText, setMinText] = useState("");
  const [page, setPage] = useState(1);

  const debouncedMin = useDebouncedValue(minText);
  const minBalance = debouncedMin.trim() === "" ? undefined : Number(debouncedMin);

  const query = useOutstandingBalances({
    OverdueOnly: scope === "overdue" ? true : undefined,
    MinBalance: minBalance !== undefined && !Number.isNaN(minBalance) ? minBalance : undefined,
    Page: page,
    PageSize: PAGE_SIZE,
  });

  return (
    <>
      <PageHeader
        title="Outstanding balances"
        description="Students who still owe fees. Contact details are here so you can follow up."
      />
      <Card>
        <CardContent className="flex flex-wrap items-end justify-between gap-4 border-b border-line">
          <FilterTabs
            label="Which students"
            value={scope}
            onChange={(value) => {
              setScope(value);
              setPage(1);
            }}
            options={[
              { value: "all", label: "All balances" },
              { value: "overdue", label: "Overdue only" },
            ]}
          />
          <div className="w-44">
            <Input
              label="Minimum balance"
              type="number"
              inputMode="decimal"
              min={0}
              placeholder="0"
              value={minText}
              onChange={(e) => {
                setMinText(e.target.value);
                setPage(1);
              }}
            />
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
        ) : list(query.data.items).length === 0 ? (
          <EmptyState
            icon={UserCheck}
            title="No students match"
            description="Try clearing the filters. If there are none, nobody owes anything."
          />
        ) : (
          <>
            <Table>
              <THead>
                <tr>
                  <Th>Student</Th>
                  <Th>Contact</Th>
                  <Th align="right">Balance</Th>
                  <Th align="right">Overdue</Th>
                </tr>
              </THead>
              <TBody>
                {list(query.data.items).map((student) => (
                  <Tr key={student.studentNumber}>
                    <Td>
                      <div className="font-medium text-navy-900">{student.studentName || "-"}</div>
                      <div className="text-xs text-muted">{student.studentNumber}</div>
                    </Td>
                    <Td>
                      <div className="space-y-0.5 text-sm">
                        {student.email ? (
                          <a
                            href={`mailto:${student.email}`}
                            className="block text-navy-700 underline-offset-4 hover:underline"
                          >
                            {student.email}
                          </a>
                        ) : (
                          <span className="block text-muted">No email</span>
                        )}
                        {student.phoneNumber && (
                          <a
                            href={`tel:${student.phoneNumber}`}
                            className="block text-muted underline-offset-4 hover:underline"
                          >
                            {student.phoneNumber}
                          </a>
                        )}
                      </div>
                    </Td>
                    <Td align="right">{formatMoney(student.balance)}</Td>
                    <Td
                      align="right"
                      className={cn(student.overdueAmount > 0 && "font-medium text-danger-700")}
                    >
                      {formatMoney(student.overdueAmount)}
                    </Td>
                  </Tr>
                ))}
              </TBody>
            </Table>
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
