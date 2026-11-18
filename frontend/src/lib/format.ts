/**
 * Used when the API returns an empty currency code (Business Central's local
 * currency). Change this once the company currency is settled.
 */
export const DEFAULT_CURRENCY = "KES";

const LOCALE = "en-KE";

export function formatMoney(
  amount: number | null | undefined,
  currencyCode?: string | null,
): string {
  return new Intl.NumberFormat(LOCALE, {
    style: "currency",
    currency: currencyCode || DEFAULT_CURRENCY,
  }).format(amount ?? 0);
}

export function formatNumber(value: number | null | undefined): string {
  return new Intl.NumberFormat(LOCALE).format(value ?? 0);
}

/** For `YYYY-MM-DD` values (API "date" fields). Avoids the time-zone off-by-one of new Date("2026-11-18"). */
export function formatDate(value: string | null | undefined): string {
  if (!value) return "-";
  const [y, m, d] = value.slice(0, 10).split("-").map(Number);
  if (!y || !m || !d) return "-";
  return new Intl.DateTimeFormat("en-GB", { dateStyle: "medium" }).format(
    new Date(y, m - 1, d),
  );
}

/** For ISO timestamps (API "date-time" fields, always UTC). Shown in the viewer's time zone. */
export function formatDateTime(value: string | null | undefined): string {
  if (!value) return "-";
  const date = new Date(value);
  if (Number.isNaN(date.getTime())) return "-";
  return new Intl.DateTimeFormat("en-GB", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(date);
}
