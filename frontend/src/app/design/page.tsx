"use client";

/**
 * Temporary design-system preview at /design.
 * Delete this folder once F4 (app shell) is in place.
 */
import { useState } from "react";
import { AlertTriangle, FileText, Users, Wallet } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader } from "@/components/ui/card";
import { StatCard } from "@/components/ui/stat-card";
import { Input } from "@/components/ui/input";
import { Modal } from "@/components/ui/modal";
import { EmptyState } from "@/components/ui/empty-state";
import { Skeleton } from "@/components/ui/skeleton";
import { Table, THead, TBody, Tr, Th, Td } from "@/components/ui/table";
import {
  InvoiceStatusBadge,
  PaymentStatusBadge,
} from "@/components/ui/status-badge";

const swatches = [
  ["bg-navy-900", "navy-900"],
  ["bg-navy-700", "navy-700"],
  ["bg-navy-100", "navy-100"],
  ["bg-forest-600", "forest-600"],
  ["bg-forest-100", "forest-100"],
  ["bg-warn-100", "warn-100"],
  ["bg-danger-600", "danger-600"],
  ["bg-surface border border-line", "surface"],
];

export default function DesignPreview() {
  const [open, setOpen] = useState(false);

  return (
    <main className="mx-auto max-w-5xl space-y-8 px-4 py-10">
      <header>
        <h1 className="font-serif text-4xl font-semibold text-navy-900">
          Design system
        </h1>
        <p className="mt-1 text-muted">
          Every building block the portal will use, in one place.
        </p>
      </header>

      <Card>
        <CardHeader title="Colors" />
        <CardContent className="grid grid-cols-2 gap-4 sm:grid-cols-4">
          {swatches.map(([cls, name]) => (
            <div key={name}>
              <div className={`h-14 rounded-lg ${cls}`} />
              <p className="mt-1.5 text-sm text-muted">{name}</p>
            </div>
          ))}
        </CardContent>
      </Card>

      <Card>
        <CardHeader title="Buttons" />
        <CardContent className="flex flex-wrap items-center gap-3">
          <Button>Save changes</Button>
          <Button variant="accent">Post invoice</Button>
          <Button variant="secondary">Export statement</Button>
          <Button variant="ghost">Cancel</Button>
          <Button variant="danger">Discard draft</Button>
          <Button loading>Posting</Button>
          <Button size="sm">Small</Button>
          <Button size="lg">Large</Button>
        </CardContent>
      </Card>

      <div className="grid gap-4 sm:grid-cols-2 lg:grid-cols-4">
        <StatCard label="Total outstanding" value="KES 1,284,500" icon={Wallet} />
        <StatCard
          label="Students with balance"
          value="214"
          icon={Users}
          tone="green"
        />
        <StatCard
          label="Overdue"
          value="KES 312,000"
          hint="38 students"
          icon={AlertTriangle}
          tone="red"
        />
        <StatCard
          label="Drafts to post"
          value="12"
          icon={FileText}
          tone="amber"
        />
      </div>

      <Card>
        <CardHeader
          title="Invoices"
          description="Status badges and a money column."
          action={<Button size="sm">New invoice</Button>}
        />
        <Table>
          <THead>
            <tr>
              <Th>Invoice</Th>
              <Th>Student</Th>
              <Th>Status</Th>
              <Th>Payment</Th>
              <Th align="right">Amount</Th>
            </tr>
          </THead>
          <TBody>
            <Tr>
              <Td>INV-1042</Td>
              <Td>Amina Wanjiru</Td>
              <Td><InvoiceStatusBadge status="Open" /></Td>
              <Td><PaymentStatusBadge status="PartiallyPaid" /></Td>
              <Td align="right">45,000.00</Td>
            </Tr>
            <Tr>
              <Td>INV-1043</Td>
              <Td>Brian Otieno</Td>
              <Td><InvoiceStatusBadge status="Paid" /></Td>
              <Td><PaymentStatusBadge status="Paid" /></Td>
              <Td align="right">62,500.00</Td>
            </Tr>
            <Tr>
              <Td>INV-1044</Td>
              <Td>Cynthia Mutua</Td>
              <Td><InvoiceStatusBadge status="Open" /></Td>
              <Td><PaymentStatusBadge status="Overdue" /></Td>
              <Td align="right">18,200.00</Td>
            </Tr>
            <Tr>
              <Td>Draft</Td>
              <Td>David Kamau</Td>
              <Td><InvoiceStatusBadge status="Draft" /></Td>
              <Td>-</Td>
              <Td align="right">30,000.00</Td>
            </Tr>
          </TBody>
        </Table>
      </Card>

      <div className="grid gap-6 md:grid-cols-2">
        <Card>
          <CardHeader title="Form fields" />
          <CardContent className="space-y-4">
            <Input label="Email" type="email" placeholder="you@university.ac.ke" />
            <Input
              label="Student number"
              hint="Found on your admission letter."
              placeholder="e.g. SC211/0001/2022"
            />
            <Input
              label="Password"
              type="password"
              defaultValue="short"
              error="Use at least 8 characters."
            />
          </CardContent>
        </Card>

        <Card>
          <CardHeader title="Loading, empty and dialog" />
          <CardContent className="space-y-5">
            <div className="space-y-2">
              <Skeleton className="h-4 w-1/3" />
              <Skeleton className="h-8 w-2/3" />
            </div>
            <EmptyState
              icon={FileText}
              title="No invoices yet"
              description="Invoices appear here once the finance office posts them."
            />
            <Button variant="secondary" onClick={() => setOpen(true)}>
              Open dialog
            </Button>
          </CardContent>
        </Card>
      </div>

      <Modal
        open={open}
        onClose={() => setOpen(false)}
        title="Discard this draft?"
        description="The draft invoice for David Kamau will be deleted. This can't be undone."
        footer={
          <>
            <Button variant="ghost" onClick={() => setOpen(false)}>
              Keep draft
            </Button>
            <Button variant="danger" onClick={() => setOpen(false)}>
              Discard draft
            </Button>
          </>
        }
      />
    </main>
  );
}
