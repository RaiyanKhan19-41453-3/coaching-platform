import { HubConnectionBuilder, type HubConnection } from '@microsoft/signalr';
import { useEffect, useRef, useState } from 'react';

type Notice = {
  id: string;
  type: string;
  title: string;
  body: string;
  sentAt: string;
};

type Asset = {
  id: string;
  fileName: string;
  contentType: string;
  sizeBytes: number;
  scanStatus: number;
};

const TOKEN_KEY = 'coaching-token';

export default function App() {
  const [token, setToken] = useState(() => localStorage.getItem(TOKEN_KEY) ?? '');
  const [email, setEmail] = useState('admin@star.local');
  const [password, setPassword] = useState('Password123!');
  const [tenantSlug, setTenantSlug] = useState('star-coaching');
  const [error, setError] = useState('');
  const [connection, setConnection] = useState('disconnected');
  const [notices, setNotices] = useState<Notice[]>([]);
  const [assets, setAssets] = useState<Asset[]>([]);
  const [file, setFile] = useState<File | null>(null);
  const [uploading, setUploading] = useState(false);
  const hub = useRef<HubConnection | null>(null);

  async function refreshAssets() {
    try {
      const response = await fetch('/api/assets', {
        headers: { Authorization: `Bearer ${localStorage.getItem(TOKEN_KEY) ?? ''}` },
      });
      if (response.ok) setAssets(await response.json() as Asset[]);
    } catch { /* notifications are the primary view */ }
  }

  useEffect(() => {
    if (!token) return;
    const c = new HubConnectionBuilder()
      .withUrl('/hubs/notifications', { accessTokenFactory: () => token })
      .withAutomaticReconnect()
      .build();

    c.on('notification', (n: Notice) => setNotices(prev => [n, ...prev].slice(0, 20)));
    c.onreconnecting(() => setConnection('reconnecting…'));
    c.onreconnected(() => setConnection('connected'));
    c.onclose(() => setConnection('disconnected'));

    setConnection('connecting…');
    c.start()
      .then(() => { setConnection('connected'); void refreshAssets(); })
      .catch((e: unknown) => {
        setConnection('failed');
        setError(String(e));
      });
    hub.current = c;

    return () => {
      hub.current = null;
      void c.stop();
    };
  }, [token]);

  async function login(event: React.FormEvent) {
    event.preventDefault();
    setError('');
    try {
      const response = await fetch('/api/auth/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, password, tenantSlug }),
      });
      if (!response.ok) throw new Error(`Login failed: HTTP ${response.status}`);
      const data = (await response.json()) as { accessToken: string };
      localStorage.setItem(TOKEN_KEY, data.accessToken);
      setToken(data.accessToken);
    } catch (e) {
      setError(String(e));
    }
  }

  async function uploadFile(event: React.FormEvent) {
    event.preventDefault();
    if (!file) return;
    setUploading(true);
    setError('');
    try {
      const headers = { Authorization: `Bearer ${token}`, 'Content-Type': 'application/json' };
      const started = await fetch('/api/assets/upload-url', {
        method: 'POST', headers,
        body: JSON.stringify({
          fileName: file.name, contentType: file.type || 'application/octet-stream',
          sizeBytes: file.size, ownerType: 'Lesson', ownerId: null, isPublic: false,
        }),
      });
      if (!started.ok) throw new Error(`Upload URL failed: HTTP ${started.status}`);
      const { id, url } = await started.json() as { id: string; url: string };
      const put = await fetch(url, { method: 'PUT', headers: { 'Content-Type': file.type || 'application/octet-stream' }, body: file });
      if (!put.ok) throw new Error(`Storage upload failed: HTTP ${put.status}`);
      await fetch(`/api/assets/${id}/complete`, { method: 'POST', headers: { Authorization: `Bearer ${token}` } });
      await refreshAssets();
      setFile(null);
    } catch (e) {
      setError(String(e));
    } finally {
      setUploading(false);
    }
  }

  function logout() {
    localStorage.removeItem(TOKEN_KEY);
    setToken('');
    setNotices([]);
    setConnection('disconnected');
  }

  return (
    <main>
      <h1>Coaching Platform</h1>

      {!token ? (
        <form className="card" onSubmit={login}>
          <h2>Sign in</h2>
          <label>Email<input value={email} onChange={e => setEmail(e.target.value)} /></label>
          <label>Password<input type="password" value={password} onChange={e => setPassword(e.target.value)} /></label>
          <label>Tenant slug<input value={tenantSlug} onChange={e => setTenantSlug(e.target.value)} /></label>
          <button type="submit">Sign in</button>
        </form>
      ) : (
        <section className="card">
          <div className="row">
            <h2>Live notifications</h2>
            <span className={`badge ${connection}`}>{connection}</span>
            <button onClick={logout}>Sign out</button>
          </div>
          {notices.length === 0 ? (
            <p className="muted">Waiting for events. Trigger an invoice or attendance update to see one arrive.</p>
          ) : (
            <ul className="notices">
              {notices.map(n => (
                <li key={n.id}>
                  <strong>{n.title}</strong>
                  <span>{n.body}</span>
                  <small>{n.type}</small>
                </li>
              ))}
            </ul>
          )}
        </section>
      )}

      {token && (
        <section className="card">
          <h2>Materials</h2>
          <form className="row" onSubmit={uploadFile}>
            <input type="file" onChange={e => setFile(e.target.files?.[0] ?? null)} />
            <button type="submit" disabled={!file || uploading}>{uploading ? 'Uploading…' : 'Upload'}</button>
          </form>
          <ul className="notices">
            {assets.map(a => (
              <li key={a.id}>
                <strong>{a.fileName}</strong>
                <span>{a.sizeBytes} bytes · {a.scanStatus === 1 ? 'ready' : 'pending'}</span>
              </li>
            ))}
          </ul>
        </section>
      )}

      {error && <p className="error">{error}</p>}
    </main>
  );
}
