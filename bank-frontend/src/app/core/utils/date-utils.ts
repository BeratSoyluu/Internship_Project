// src/app/core/utils/date-utils.ts
export function toVbDate(date: Date): string {
  const local = new Date(date.getTime() - (date.getTimezoneOffset() * 60000))
    .toISOString()
    .slice(0, 19);
  return `${local}+03:00`;
}
