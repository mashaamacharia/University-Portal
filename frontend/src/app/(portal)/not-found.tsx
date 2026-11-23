import Link from "next/link";
import { Compass } from "lucide-react";
import { buttonVariants } from "@/components/ui/button";
import { EmptyState } from "@/components/ui/empty-state";

export const metadata = { title: "Page not found" };

/** Shown inside the app shell, so the menu stays available. */
export default function PortalNotFound() {
  return (
    <div className="mx-auto max-w-xl py-10">
      <EmptyState
        icon={Compass}
        title="We can't find that page"
        description="The link may be wrong or the page may have moved."
        action={
          <Link href="/" className={buttonVariants()}>
            Back to my dashboard
          </Link>
        }
      />
    </div>
  );
}
