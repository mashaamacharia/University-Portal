import Link from "next/link";
import { Compass } from "lucide-react";
import { Brand } from "@/components/brand";
import { buttonVariants } from "@/components/ui/button";
import { EmptyState } from "@/components/ui/empty-state";

export const metadata = { title: "Page not found" };

export default function NotFound() {
  return (
    <main className="mx-auto flex min-h-screen max-w-xl flex-col justify-center px-4 py-16">
      <Brand className="mb-10 self-center" />
      <EmptyState
        icon={Compass}
        title="We can't find that page"
        description="The link may be wrong or the page may have moved."
        action={
          <Link href="/" className={buttonVariants()}>
            Go to the portal
          </Link>
        }
      />
    </main>
  );
}
