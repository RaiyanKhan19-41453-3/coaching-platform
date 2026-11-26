export const TOKEN_KEY = 'coaching-token';

export function getToken() { return localStorage.getItem(TOKEN_KEY) ?? ''; }
export function setToken(t: string) { localStorage.setItem(TOKEN_KEY, t); }
export function clearToken() { localStorage.removeItem(TOKEN_KEY); }

export async function api<T = any>(path: string, options: { method?: string; body?: unknown; headers?: Record<string, string> } = {}): Promise<T> {
  const r = await fetch(path, {
    method: options.method ?? (options.body !== undefined ? 'POST' : 'GET'),
    headers: {
      'Content-Type': 'application/json',
      Authorization: `Bearer ${getToken()}`,
      ...(options.headers ?? {}),
    },
    body: options.body !== undefined ? JSON.stringify(options.body) : undefined,
  });
  const text = await r.text();
  if (!r.ok) {
    let message = `${r.status} ${r.statusText}`;
    try {
      const j = JSON.parse(text);
      message = j.detail?.error ?? j.detail ?? j.message ?? j.title ?? message;
    } catch { if (text) message = text.slice(0, 200); }
    throw new Error(message);
  }
  if (!text) return null as T;
  try { return JSON.parse(text) as T; } catch { return text as unknown as T; }
}

export function asArray<T = any>(v: unknown): T[] {
  if (Array.isArray(v)) return v as T[];
  if (v && typeof v === 'object' && 'items' in v) return (v as any).items as T[];
  return [];
}

export async function apiBlob(path: string): Promise<{ blob: Blob; fileName: string }> {
  const r = await fetch(path, { headers: { Authorization: `Bearer ${getToken()}` } });
  if (!r.ok) throw new Error(`${r.status} ${r.statusText}`);
  const match = r.headers.get('content-disposition')?.match(/filename="?([^";]+)"?/i);
  return { blob: await r.blob(), fileName: match?.[1] ?? 'download' };
}

export function saveBlob(blob: Blob, fileName: string) {
  const url = URL.createObjectURL(blob);
  const a = document.createElement('a');
  a.href = url;
  a.download = fileName;
  a.click();
  URL.revokeObjectURL(url);
}
