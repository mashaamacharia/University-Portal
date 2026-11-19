"use client";

import { useState } from "react";
import { KeyRound, Plus, UserCheck, UserX, Users } from "lucide-react";
import { useAuth } from "@/components/auth/auth-provider";
import { PageHeader } from "@/components/page-header";
import { Alert } from "@/components/ui/alert";
import { Badge, type BadgeTone } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card, CardContent } from "@/components/ui/card";
import { EmptyState } from "@/components/ui/empty-state";
import { ErrorState } from "@/components/ui/error-state";
import { FilterTabs } from "@/components/ui/filter-tabs";
import { Input } from "@/components/ui/input";
import { Pagination } from "@/components/ui/pagination";
import { Skeleton } from "@/components/ui/skeleton";
import { Table, TBody, Td, Th, THead, Tr } from "@/components/ui/table";
import { useUsers } from "@/lib/api/hooks/admin";
import { roleLabel } from "@/lib/auth/roles";
import { formatDate } from "@/lib/format";
import { useDebouncedValue } from "@/lib/hooks/use-debounced-value";
import { list } from "@/lib/utils";
import type { PortalRole, UserSummary } from "@/types";
import { CreateUserDialog } from "./create-user-dialog";
import { ResetPasswordDialog } from "./reset-password-dialog";
import { ToggleActiveDialog } from "./toggle-active-dialog";

const PAGE_SIZE = 20;
type RoleFilter = PortalRole | "all";
type Dialog =
  | { type: "create" }
  | { type: "reset"; user: UserSummary }
  | { type: "toggle"; user: UserSummary }
  | null;

const ROLE_TONE: Record<string, BadgeTone> = {
  Administrator: "navy",
  FinanceOfficer: "green",
  Student: "neutral",
};

export function AdminUsersView() {
  const { user: me } = useAuth();
  const [role, setRole] = useState<RoleFilter>("all");
  const [search, setSearch] = useState("");
  const [page, setPage] = useState(1);
  const [dialog, setDialog] = useState<Dialog>(null);
  const [notice, setNotice] = useState<string | null>(null);

  const debouncedSearch = useDebouncedValue(search).trim();
  const query = useUsers({
    Role: role === "all" ? undefined : role,
    Search: debouncedSearch || undefined,
    Page: page,
    PageSize: PAGE_SIZE,
  });

  const filtered = role !== "all" || search !== "";
  const open = (next: Dialog) => {
    setNotice(null);
    setDialog(next);
  };

  return (
    <>
      <PageHeader
        title="Users"
        description="Create accounts and control who can sign in."
        actions={
          <Button variant="accent" onClick={() => open({ type: "create" })}>
            <Plus className="size-4" aria-hidden />
            New user
          </Button>
        }
      />

      {notice && (
        <Alert tone="success" className="mb-4">
          {notice}
        </Alert>
      )}

      <Card>
        <CardContent className="flex flex-wrap items-end justify-between gap-4 border-b border-line">
          <FilterTabs
            label="Role"
            value={role}
            onChange={(value) => {
              setRole(value);
              setPage(1);
            }}
            options={[
              { value: "all", label: "Everyone" },
              { value: "Student", label: "Students" },
              { value: "FinanceOfficer", label: "Finance officers" },
              { value: "Administrator", label: "Administrators" },
            ]}
          />
          <div className="w-64">
            <Input
              label="Search"
              type="search"
              placeholder="Name, email or student number"
              value={search}
              onChange={(e) => {
                setSearch(e.target.value);
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
            icon={Users}
            title="No users found"
            description={filtered ? "Nobody matches those filters." : "Create the first account to get started."}
          />
        ) : (
          <>
            <Table>
              <THead>
                <tr>
                  <Th>User</Th>
                  <Th>Role</Th>
                  <Th>Student number</Th>
                  <Th>Status</Th>
                  <Th>Created</Th>
                  <Th align="right">Actions</Th>
                </tr>
              </THead>
              <TBody>
                {list(query.data.items).map((u) => {
                  const isSelf = u.id === me.userId;
                  return (
                    <Tr key={u.id}>
                      <Td>
                        <div className="font-medium text-navy-900">
                          {u.fullName || "-"}
                          {isSelf && <span className="ml-2 text-xs font-normal text-muted">(you)</span>}
                        </div>
                        <div className="text-xs text-muted">{u.email}</div>
                      </Td>
                      <Td>
                        <Badge tone={ROLE_TONE[u.role ?? ""] ?? "neutral"}>{roleLabel(u.role ?? "")}</Badge>
                      </Td>
                      <Td>{u.studentNumber || "-"}</Td>
                      <Td>
                        <Badge tone={u.isActive ? "green" : "neutral"}>
                          {u.isActive ? "Active" : "Inactive"}
                        </Badge>
                      </Td>
                      <Td>{formatDate(u.createdAtUtc)}</Td>
                      <Td align="right">
                        <div className="flex justify-end gap-1">
                          <Button variant="ghost" size="sm" onClick={() => open({ type: "reset", user: u })}>
                            <KeyRound className="size-4" aria-hidden />
                            Reset password
                          </Button>
                          <Button
                            variant="ghost"
                            size="sm"
                            disabled={isSelf && u.isActive}
                            title={isSelf && u.isActive ? "You can't deactivate your own account." : undefined}
                            onClick={() => open({ type: "toggle", user: u })}
                          >
                            {u.isActive ? (
                              <UserX className="size-4" aria-hidden />
                            ) : (
                              <UserCheck className="size-4" aria-hidden />
                            )}
                            {u.isActive ? "Deactivate" : "Reactivate"}
                          </Button>
                        </div>
                      </Td>
                    </Tr>
                  );
                })}
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

      {dialog?.type === "create" && (
        <CreateUserDialog
          onClose={() => setDialog(null)}
          onCreated={(created) => {
            setDialog(null);
            setNotice(
              `Created ${created.fullName || created.email}. Give them their temporary password securely.`,
            );
          }}
        />
      )}
      {dialog?.type === "reset" && (
        <ResetPasswordDialog
          user={dialog.user}
          onClose={() => setDialog(null)}
          onDone={(target) => {
            setDialog(null);
            setNotice(`Password reset for ${target.fullName || target.email}. Give them the new one securely.`);
          }}
        />
      )}
      {dialog?.type === "toggle" && (
        <ToggleActiveDialog
          user={dialog.user}
          onClose={() => setDialog(null)}
          onDone={(target, isActive) => {
            setDialog(null);
            setNotice(`${target.fullName || target.email} is now ${isActive ? "active" : "inactive"}.`);
          }}
        />
      )}
    </>
  );
}
