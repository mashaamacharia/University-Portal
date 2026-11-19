const LOWER = "abcdefghijkmnopqrstuvwxyz"; // no l (looks like 1)
const UPPER = "ABCDEFGHJKLMNPQRSTUVWXYZ"; // no I or O
const DIGITS = "23456789"; // no 0 or 1
const SYMBOLS = "!@#$%&*?";

function randomIndex(max: number): number {
  const buffer = new Uint32Array(1);
  crypto.getRandomValues(buffer);
  return buffer[0] % max;
}

const pick = (set: string) => set[randomIndex(set.length)];

/** A readable temporary password with at least one of each character type. */
export function generatePassword(length = 14): string {
  const all = LOWER + UPPER + DIGITS + SYMBOLS;
  const chars = [pick(LOWER), pick(UPPER), pick(DIGITS), pick(SYMBOLS)];
  while (chars.length < length) chars.push(pick(all));

  // Fisher-Yates shuffle so the guaranteed characters aren't always first.
  for (let i = chars.length - 1; i > 0; i--) {
    const j = randomIndex(i + 1);
    [chars[i], chars[j]] = [chars[j], chars[i]];
  }
  return chars.join("");
}
