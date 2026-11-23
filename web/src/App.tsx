import { useEffect, useState } from 'react';

type Health = { service: string; status: string; stack: string[] } | null;

export default function App() {
  const [health, setHealth] = useState<Health>(null);
  const [error, setError] = useState('');

  useEffect(() => {
    fetch('/api/')
      .then((r) => (r.ok ? r.json() : Promise.reject(new Error(`HTTP ${r.status}`))))
      .then(setHealth)
      .catch((e) => setError(String(e)));
  }, []);

  return (
    <main>
      <h1>Coaching Platform</h1>
      <p>Multi-tenant coaching center management — API + Web running entirely in Docker.</p>
      {error && <p className="error">API unreachable: {error}</p>}
      {health && (
        <pre>{JSON.stringify(health, null, 2)}</pre>
      )}
      {!health && !error && <p>Contacting API…</p>}
    </main>
  );
}
