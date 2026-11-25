import Image from "next/image";
import { cn } from "@/lib/cn";

export function Brand({
  tone = "dark",
  className,
}: {
  tone?: "dark" | "light";
  className?: string;
}) {
  return (
    <span className={cn("inline-flex items-center gap-2.5", className)}>
      <Image
        src="/chuka-university-logo.png"
        alt=""
        width={52}
        height={42}
        priority
        className="h-10 w-12 shrink-0 object-contain"
      />
      <span
        className={cn(
          "font-serif text-base font-semibold leading-tight",
          tone === "light" ? "text-white" : "text-navy-900",
        )}
      >
        Chuka University Portal
      </span>
    </span>
  );
}
