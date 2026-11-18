import { cn } from "@/lib/cn";

/** Scrolls sideways inside its own box so the page never does. */
export function Table({ className, ...props }: React.ComponentProps<"table">) {
  return (
    <div className="w-full overflow-x-auto">
      <table
        className={cn("w-full border-collapse text-left text-sm", className)}
        {...props}
      />
    </div>
  );
}

export function THead(props: React.ComponentProps<"thead">) {
  return <thead className="bg-navy-50" {...props} />;
}

export function TBody(props: React.ComponentProps<"tbody">) {
  return <tbody className="divide-y divide-line" {...props} />;
}

export function Tr({ className, ...props }: React.ComponentProps<"tr">) {
  return <tr className={cn("hover:bg-navy-50/60", className)} {...props} />;
}

export function Th({
  className,
  align = "left",
  ...props
}: React.ComponentProps<"th"> & { align?: "left" | "right" }) {
  return (
    <th
      scope="col"
      className={cn(
        "whitespace-nowrap px-5 py-3 font-medium text-muted",
        align === "right" && "text-right",
        className,
      )}
      {...props}
    />
  );
}

export function Td({
  className,
  align = "left",
  ...props
}: React.ComponentProps<"td"> & { align?: "left" | "right" }) {
  return (
    <td
      className={cn(
        "px-5 py-3.5 text-ink",
        align === "right" && "money text-right",
        className,
      )}
      {...props}
    />
  );
}
