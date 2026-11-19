/** Turns a nullable list from the API into a plain array. */
export function list<T>(value: readonly T[] | null | undefined): T[] {
  return value ? [...value] : [];
}
