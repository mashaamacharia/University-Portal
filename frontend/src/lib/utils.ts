/** Turns a nullable list from the API into a plain array. */
export function list<T>(value: readonly T[] | null | undefined): T[] {
  return value ? [...value] : [];
}

/** A unique id. Falls back when crypto.randomUUID is unavailable (plain-http pages). */
export function newId(): string {
  if (typeof crypto !== "undefined" && "randomUUID" in crypto) {
    return crypto.randomUUID();
  }
  return `${Date.now().toString(36)}-${Math.random().toString(36).slice(2, 10)}`;
}
