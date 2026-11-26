import { useEffect, useState } from 'react';
import { Stack, Title, Card, Group, Badge, Text, Button, Alert } from '@mantine/core';
import { HubConnectionBuilder } from '@microsoft/signalr';
import { api, getToken, asArray } from '../api';

export function Notifications() {
  const [items, setItems] = useState<any[]>([]);
  const [status, setStatus] = useState('connecting…');
  const [err, setErr] = useState('');
  const load = () => api('/api/communication/notifications').then(r=>setItems(asArray(r))).catch(e=>setErr(e.message));
  useEffect(()=>{
    void load();
    const c = new HubConnectionBuilder().withUrl('/hubs/notifications',{accessTokenFactory:()=>getToken()}).withAutomaticReconnect().build();
    c.on('notification', n => setItems(prev=>[n,...prev]));
    c.start().then(()=>setStatus('connected')).catch(()=>setStatus('offline'));
    return ()=>{ void c.stop(); };
  },[]);
  return <Stack gap="md">
    <Group justify="space-between"><Title order={2}>Notifications</Title><Badge color={status==='connected'?'green':'gray'}>{status}</Badge></Group>
    {err && <Alert color="red">{err}</Alert>}
    {items.length===0 && <Text c="dimmed">No notifications yet.</Text>}
    {items.map(n=><Card key={n.id} withBorder padding="sm">
      <Group justify="space-between"><Text fw={600}>{n.title}</Text><Group gap="xs"><Badge size="xs" variant="light">{n.type}</Badge>{!n.isRead && <Button size="xs" variant="subtle" onClick={()=>api(`/api/communication/notifications/${n.id}/read`,{method:'POST'}).then(load)}>Mark read</Button>}</Group></Group>
      <Text size="sm" c="dimmed">{n.body}</Text><Text size="xs" c="dimmed">{n.sentAt ? new Date(n.sentAt).toLocaleString() : ''}</Text>
    </Card>)}
  </Stack>;
}
