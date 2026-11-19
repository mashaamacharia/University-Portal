import { Card, CardContent } from "@/components/ui/card";
import { PageHeader } from "@/components/page-header";
import { getCurrentUser } from "@/lib/server/session";

/** Temporary page body until F5, F6 and F8 replace it. */
export async function Placeholder({ title, next }: { title: string; next: string }) {
  const user = await getCurrentUser();
  const name = user?.fullName || user?.email || "there";

  return (
    <>
      <PageHeader title={title} description={`Welcome, ${name}.`} />
      <Card>
        <CardContent>
          <p className="text-sm text-ink">
            Sign-in and the app shell are working. This screen is built in{" "}
            <strong>{next}</strong>.
          </p>
        </CardContent>
      </Card>
    </>
  );
}
