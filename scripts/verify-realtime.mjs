import { HubConnectionBuilder } from '@microsoft/signalr';

const base = 'http://localhost:18081';
const loginResponse = await fetch(`${base}/api/auth/login`, {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    email: 'admin@star.local',
    password: 'Password123!',
    tenantSlug: 'star-coaching',
  }),
});
if (!loginResponse.ok) throw new Error(`login failed: ${loginResponse.status}`);
const auth = await loginResponse.json();
const headers = { 'Content-Type': 'application/json', Authorization: `Bearer ${auth.accessToken}` };

const connection = new HubConnectionBuilder()
  .withUrl(`${base}/hubs/notifications`, { accessTokenFactory: () => auth.accessToken })
  .build();

let received = null;
connection.on('notification', (notice) => {
  received = notice;
  console.log('PUSHED:', JSON.stringify(notice));
});

await connection.start();
console.log('SignalR connected');

const invoice = await fetch(`${base}/api/invoices`, {
  method: 'POST', headers,
  body: JSON.stringify({
    studentId: auth.userId,
    lines: [{ description: 'Realtime test', quantity: 1, unitPrice: 1234, currency: 'BDT' }],
    discount: 0, taxPercent: 0,
  }),
});
console.log('invoice HTTP', invoice.status);
await fetch(`${base}/api/jobs/outbox/dispatch`, { method: 'POST', headers });

await new Promise(resolve => setTimeout(resolve, 4000));
await connection.stop();

if (!received) {
  console.error('FAILED: no notification pushed');
  process.exit(1);
}
console.log('SUCCESS: outbox event delivered over SignalR');
