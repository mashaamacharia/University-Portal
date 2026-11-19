import { ChangePasswordForm } from "@/components/auth/change-password-form";
import { Card, CardContent, CardHeader } from "@/components/ui/card";

export const metadata = { title: "Change password" };

export default function ChangePasswordPage() {
  return (
    <div className="mx-auto max-w-md">
      <Card>
        <CardHeader
          title="Change password"
          description="Use something you don't use anywhere else."
        />
        <CardContent>
          <ChangePasswordForm />
        </CardContent>
      </Card>
    </div>
  );
}
