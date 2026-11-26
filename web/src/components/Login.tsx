import { useState } from 'react';
import { Center, Paper, Title, TextInput, PasswordInput, Button, Stack, Text, Alert } from '@mantine/core';
import { api, setToken } from '../api';

export function Login({ onSuccess }: { onSuccess:()=>void }) {
  const [email, setEmail] = useState('admin@star.local');
  const [password, setPassword] = useState('Password123!');
  const [slug, setSlug] = useState('star-coaching');
  const [err, setErr] = useState(''); const [busy,setBusy] = useState(false);
  const submit = async () => {
    setBusy(true); setErr('');
    try {
      const r = await fetch('/api/auth/login',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({email,password,tenantSlug:slug})});
      const j = await r.json();
      if(!r.ok) throw new Error(j.detail?.error ?? j.title ?? 'Login failed');
      setToken(j.accessToken); onSuccess();
    } catch(e:any){ setErr(e.message); } finally { setBusy(false); }
  };
  return <Center mih="80vh">
    <Paper withBorder shadow="md" p="xl" w={420}>
      <Stack gap="md">
        <div><Title order={2}>Coaching Platform</Title><Text c="dimmed" size="sm">Sign in to your center</Text></div>
        {err && <Alert color="red">{err}</Alert>}
        <TextInput label="Email" value={email} onChange={e=>setEmail(e.currentTarget.value)}/>
        <PasswordInput label="Password" value={password} onChange={e=>setPassword(e.currentTarget.value)}/>
        <TextInput label="Tenant slug" value={slug} onChange={e=>setSlug(e.currentTarget.value)}/>
        <Button loading={busy} onClick={submit} fullWidth>Sign in</Button>
      </Stack>
    </Paper>
  </Center>;
}
