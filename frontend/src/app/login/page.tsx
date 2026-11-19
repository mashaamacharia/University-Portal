import type { Metadata } from "next";
import { redirect } from "next/navigation";
import { ShieldCheck, Smartphone, Users } from "lucide-react";
import { Brand } from "@/components/brand";
import { LoginForm } from "@/components/auth/login-form";
import { homeForRoles, safeNext } from "@/lib/auth/roles";
import { getCurrentUser } from "@/lib/server/session";

export const metadata: Metadata = {
  title: "Sign in",
  description: "Sign in to the university portal.",
};

const points = [
  {
    icon: Users,
    text: "Student services and staff tools together in one portal.",
  },
  {
    icon: ShieldCheck,
    text: "Secure sign-in with access tailored to your role.",
  },
  {
    icon: Smartphone,
    text: "Works on your phone, tablet or computer.",
  },
];

export default async function LoginPage({
  searchParams,
}: {
  searchParams: Promise<{ next?: string; reason?: string }>;
}) {
  const { next, reason } = await searchParams;

  // Already signed in? Skip the form. (If the API is down, just show the form.)
  const user = await getCurrentUser().catch(() => null);
  if (user) redirect(safeNext(next) ?? homeForRoles(user.roles));

  return (
    <main className="grid min-h-screen lg:grid-cols-[5fr_4fr]">
      <aside className="relative hidden flex-col justify-between overflow-hidden bg-navy-900 p-12 text-white lg:flex">
        <div
          aria-hidden
          className="absolute -bottom-32 -left-24 size-[28rem] rounded-full bg-forest-600/25 blur-3xl"
        />
        <Brand tone="light" className="relative" />
        <div className="relative max-w-md">
          <h1 className="font-serif text-5xl font-semibold leading-tight">
            Your university, all in one place.
          </h1>
          <ul className="mt-10 space-y-4">
            {points.map(({ icon: Icon, text }) => (
              <li key={text} className="flex items-start gap-3 text-navy-100">
                <Icon className="mt-0.5 size-5 shrink-0 text-forest-300" aria-hidden />
                <span>{text}</span>
              </li>
            ))}
          </ul>
        </div>
        <p className="relative text-sm text-navy-300">University Portal</p>
      </aside>

      <section className="flex items-center justify-center px-6 py-12">
        <div className="w-full max-w-sm">
          <Brand className="mb-10 lg:hidden" />
          <h2 className="font-serif text-3xl font-semibold text-navy-900">Welcome back</h2>
          <p className="mb-8 mt-1 text-muted">Sign in to your portal account.</p>
          <LoginForm next={next} expired={reason === "expired"} />
        </div>
      </section>
    </main>
  );
}
