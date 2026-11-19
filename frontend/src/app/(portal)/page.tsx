import { redirect } from "next/navigation";
import { homeForRoles } from "@/lib/auth/roles";
import { requireUser } from "@/lib/server/guards";

/** "/" sends each person to their own area. */
export default async function Home() {
  const user = await requireUser();
  redirect(homeForRoles(user.roles));
}
