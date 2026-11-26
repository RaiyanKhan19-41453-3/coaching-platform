import { useEffect, useState } from 'react';
import { Stack, Title, Table, Paper, Text, Alert, Group, Badge } from '@mantine/core';
import { api, asArray } from '../api';

export function Parent() {
  const [children, setChildren] = useState<any[]>([]);
  const [summary, setSummary] = useState<any>(null);
  const [err, setErr] = useState('');
  useEffect(()=>{ api('/api/parent/children').then(r=>{const list=asArray(r); setChildren(list); if(list[0]) api(`/api/parent/children/${list[0].studentId}/summary`).then(setSummary).catch(e=>setErr(e.message));}).catch(e=>setErr(e.message)); },[]);
  return <Stack gap="md">
    <Title order={2}>Parent portal</Title>
    {err && <Alert color="red">{err}</Alert>}
    <Paper withBorder><Table><Table.Thead><Table.Tr><Table.Th>Child</Table.Th><Table.Th>Relationship</Table.Th><Table.Th>ID</Table.Th></Table.Tr></Table.Thead>
      <Table.Tbody>{children.map(c=><Table.Tr key={c.studentId}><Table.Td>{c.firstName} {c.lastName}</Table.Td><Table.Td>{c.relationship}</Table.Td><Table.Td>{c.studentId.slice(0,8)}…</Table.Td></Table.Tr>)}</Table.Tbody></Table></Paper>
    {summary && <Paper withBorder p="md"><Group justify="space-between"><Title order={4}>{summary.name}</Title><Badge color="teal">Guardian view</Badge></Group>
      <Text mt="sm">Attendance: {summary.attendancePercent}% · Collected: {summary.collected} · Outstanding: {summary.outstanding} · Average grade: {summary.averageGrade}% · Active enrolments: {summary.activeEnrollments}</Text>
      <Text size="xs" c="dimmed" mt="sm">Figures respect the guardian's visibility flags.</Text></Paper>}
  </Stack>;
}
