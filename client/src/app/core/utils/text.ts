export function normalizeText(value: string): string {
  return value
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
    .toLowerCase();
}

export interface TextSegment {
  text: string;
  match: boolean;
}

function baseChar(ch: string): string {
  const decomposed = ch.normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase();
  return decomposed.length > 0 ? decomposed : ch.toLowerCase();
}

export function splitMatch(text: string, query: string): TextSegment[] {
  const needle = normalizeText(query.trim());
  if (!needle) {
    return [{ text, match: false }];
  }

  const chars = Array.from(text);
  const starts: number[] = [];
  const normalized: string[] = [];
  let offset = 0;

  for (const ch of chars) {
    starts.push(offset);
    normalized.push(baseChar(ch));
    offset += ch.length;
  }

  starts.push(offset);
  const haystack = normalized.join('');
  const found = haystack.indexOf(needle);
  if (found === -1) {
    return [{ text, match: false }];
  }

  const from = starts[found];
  const to = starts[found + needle.length];
  const segments: TextSegment[] = [];

  if (from > 0) {
    segments.push({ text: text.slice(0, from), match: false });
  }
  segments.push({ text: text.slice(from, to), match: true });
  if (to < text.length) {
    segments.push({ text: text.slice(to), match: false });
  }

  return segments;
}
