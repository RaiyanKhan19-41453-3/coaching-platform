import { useState } from 'react';
import { Stack, Title, TextInput, Paper, Text, Badge, Group, Alert } from '@mantine/core';
import { api } from '../api';

export function Search() {
  const [term, setTerm] = useState('');
  const [result, setResult] = useState<any>(null);
  const [err, setErr] = useState('');
  const run = (value:string) => { setTerm(value); if(value.trim().length<2){setResult(null);return;} api(`/api/reports/search?term=${encodeURIComponent(value)}`).then(setResult).catch(e=>setErr(e.message)); };
  return <Stack gap="md">
    <Title order={2}>Search</Title>
    <TextInput placeholder="Search students, courses, batches, files…" value={term} onChange={e=>run(e.currentTarget.value)} size="md"/>
    {err && <Alert color="red">{err}</Alert>}
    {result && Object.entries(result).map(([k,v]:any)=><Paper key={k} withBorder p="sm">
      <Group justify="space-between"><Text fw={600} tt="capitalize">{k}</Text><Badge>{v.length}</Badge></Group>
      {v.length===0 ? <Text size="sm" c="dimmed">No matches</Text> : v.map((x:any)=><Text key={x.id} size="sm">• {x.name} <Text span size="xs" c="dimmed">({x.id.slice(0,8)})</Text></Text>)}
    </Paper>)}
  </Stack>;
}
