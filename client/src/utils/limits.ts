// Must match server/Helpers/Limits.cs
export const TIME_LIMIT_MIN = 5;
export const TIME_LIMIT_MAX = 300;
export const POINTS_MIN = 1;
export const POINTS_MAX = 1000;

/** Keeps digits only and caps the value at `max`. Returns "" when nothing is left. */
export function parseWholeNumber(raw: string, max: number): number | "" {
  const digits = raw.replace(/\D/g, "").slice(0, 10);
  return digits === "" ? "" : Math.min(Number(digits), max);
}
