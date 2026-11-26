import { useEffect, useState } from 'react';
import { SimpleGrid, Card, Text, Title, Group, Badge, Button, Stack, Alert } from '@mantine/core';
import { Link } from 'react-router-dom';
import { api, asArray } from '../api';

export function Dashboard() {
  const [d, setD] = useState<any>(null);
  const [err, setErr] = useState('');
  useEffect(()=>{ api('/api/reports/dashboard').then(setD).catch(e=>setErr(e.message)); },[]);
  const cards = [
    { label:'Students', value:d?.students, to:'/academics', tone:'blue' },
    { label:'Active batches', value:d?.batches, to:'/academics', tone:'teal' },
    { label:'Collected', value:d?.collected, to:'/finance', tone:'green' },
    { label:'Outstanding', value:d?.outstanding, to:'/finance', tone:'orange' },
    { label:'Avg attendance', value:d?.attendancePercent ? `${d.attendancePercent}%` : 0, to:'/reports', tone:'violet' },
    { label:'Low attendance alerts', value:d?.lowAttendanceCount, to:'/reports', tone:'red' },
  ];
  return <Stack gap="md">
    <Group justify="space-between"><div><Title order={2}>Overview</Title><Text c="dimmed">Live snapshot of your center</Text></div><Badge color="teal" variant="light">Real-time projections</Badge></Group>
    {err && <Alert color="red">{err}</Alert>}
    <SimpleGrid cols={{ base:1, sm:2, lg:3 }}>
      {cards.map(c=><Card key={c.label} withBorder radius="md" p="lg" component={Link} to={c.to} style={{textDecoration:'none',color:'inherit'}}>
        <Text size="sm" c="dimmed">{c.label}</Text>
        <Title order={2} mt="xs">{c.value ?? '—'}</Title>
        <Text size="xs" c={c.tone as any} mt="xs" tt="uppercase" fw={700}>Open →</Text>
      </Card>)}
    </SimpleGrid>
    <Card withBorder><Title order={4} mb="sm">Get started</Title>
      <Group>
        <Button component={Link} to="/academics" variant="light">Manage courses & batches</Button>
        <Button component={Link} to="/finance" variant="light">Create an invoice</Button>
        <Button component={Link} to="/assessment" variant="light">Build an assessment</Button>
        <Button component={Link} to="/communication" variant="light">Post an announcement</Button>
      </Group>
    </Card>
  </Stack>;
}
