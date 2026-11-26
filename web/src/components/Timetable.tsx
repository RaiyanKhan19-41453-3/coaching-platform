import { useState } from 'react';
import { Stack, Title, Text, Paper, Tabs, TextInput, Button, Group, Table, Textarea, Alert, Badge, SimpleGrid, Card } from '@mantine/core';
import { api, asArray } from '../api';

/** Scheduling operations: timetable lookup, session generation, attendance and room availability. */
export function Timetable() {
  const [msg, setMsg] = useState(''), [err, setErr] = useState('');
  const [rows, setRows] = useState<any[]>([]);

  const [batchId, setBatchId] = useState(''), [teacherId, setTeacherId] = useState(''), [studentId, setStudentId] = useState('');
  const [from, setFrom] = useState(''), [to, setTo] = useState('');
  const [ruleId, setRuleId] = useState('');
  const [sessionId, setSessionId] = useState(''), [attendanceJson, setAttendanceJson] = useState('[]');
  const [roomId, setRoomId] = useState(''), [startUtc, setStartUtc] = useState(''), [endUtc, setEndUtc] = useState('');

  const run = async (fn: () => Promise<any>, success = '') => {
    setErr(''); setMsg('');
    try { const r = await fn(); if (success) setMsg(success); return r; }
    catch (e: any) { setErr(e.message); return null; }
  };
  const qs = (parts: Record<string, string>) => Object.entries(parts).filter(([, v]) => v).map(([k, v]) => `${k}=${encodeURIComponent(v)}`).join('&');

  const loadTimetable = () => run(async () => {
    const r = await api(`/api/timetable?${qs({ batchId, teacherId, studentId, from, to })}`);
    setRows(asArray(r)); return r;
  });

  const loadAttendance = () => run(async () => {
    const r = await api(`/api/sessions/${sessionId}/attendance`);
    setRows(asArray(r)); return r;
  });

  return (
    <Stack gap="md">
      <div><Title order={2}>Timetable & attendance</Title><Text c="dimmed">Generate sessions, check room availability and record attendance</Text></div>
      {err && <Alert color="red">{err}</Alert>}
      {msg && <Alert color="green">{msg}</Alert>}

      <Tabs defaultValue="timetable">
        <Tabs.List>
          <Tabs.Tab value="timetable">Timetable</Tabs.Tab>
          <Tabs.Tab value="sessions">Generate sessions</Tabs.Tab>
          <Tabs.Tab value="attendance">Attendance</Tabs.Tab>
          <Tabs.Tab value="rooms">Room availability</Tabs.Tab>
        </Tabs.List>

        <Tabs.Panel value="timetable" pt="md">
          <Paper withBorder p="md" mb="md">
            <Group align="flex-end" wrap="wrap">
              <TextInput label="Batch ID" value={batchId} onChange={e => setBatchId(e.currentTarget.value)} />
              <TextInput label="Teacher ID" value={teacherId} onChange={e => setTeacherId(e.currentTarget.value)} />
              <TextInput label="Student ID" value={studentId} onChange={e => setStudentId(e.currentTarget.value)} />
              <TextInput label="From" type="datetime-local" value={from} onChange={e => setFrom(e.currentTarget.value)} />
              <TextInput label="To" type="datetime-local" value={to} onChange={e => setTo(e.currentTarget.value)} />
              <Button onClick={loadTimetable}>Search</Button>
            </Group>
          </Paper>
          <Paper withBorder>
            <Table.ScrollContainer minWidth={700}>
              <Table striped>
                <Table.Thead><Table.Tr><Table.Th>Session</Table.Th><Table.Th>Batch</Table.Th><Table.Th>Starts</Table.Th><Table.Th>Ends</Table.Th><Table.Th>Room</Table.Th><Table.Th>Teacher</Table.Th></Table.Tr></Table.Thead>
                <Table.Tbody>
                  {rows.length === 0 && <Table.Tr><Table.Td colSpan={6}><Text c="dimmed" ta="center" py="lg">No sessions in this range</Text></Table.Td></Table.Tr>}
                  {rows.map((r: any, i: number) => <Table.Tr key={r.id ?? i}>
                    <Table.Td>{String(r.id ?? '').slice(0, 8)}</Table.Td><Table.Td>{r.batchName ?? r.batchId ?? '—'}</Table.Td>
                    <Table.Td>{r.startUtc ? new Date(r.startUtc).toLocaleString() : r.startTime ?? '—'}</Table.Td>
                    <Table.Td>{r.endUtc ? new Date(r.endUtc).toLocaleString() : r.endTime ?? '—'}</Table.Td>
                    <Table.Td>{r.roomName ?? r.roomId ?? '—'}</Table.Td><Table.Td>{r.teacherName ?? r.teacherId ?? '—'}</Table.Td>
                  </Table.Tr>)}
                </Table.Tbody>
              </Table>
            </Table.ScrollContainer>
          </Paper>
        </Tabs.Panel>

        <Tabs.Panel value="sessions" pt="md">
          <Paper withBorder p="md" maw={560}>
            <Title order={4} mb="xs">Materialise recurring sessions</Title>
            <Text size="sm" c="dimmed" mb="md">Creates concrete session rows from a schedule rule over a date range.</Text>
            <Stack gap="sm">
              <TextInput label="Schedule rule ID" value={ruleId} onChange={e => setRuleId(e.currentTarget.value)} />
              <Group grow><TextInput label="From (YYYY-MM-DD)" type="date" value={from} onChange={e => setFrom(e.currentTarget.value)} />
                <TextInput label="To (YYYY-MM-DD)" type="date" value={to} onChange={e => setTo(e.currentTarget.value)} /></Group>
              <Button onClick={() => run(() => api(`/api/schedule-rules/${ruleId}/sessions`, { method: 'POST', body: { from, to } }), 'Sessions generated')}>Generate</Button>
            </Stack>
          </Paper>
        </Tabs.Panel>

        <Tabs.Panel value="attendance" pt="md">
          <Paper withBorder p="md" mb="md">
            <Group align="flex-end" wrap="wrap">
              <TextInput label="Session ID" value={sessionId} onChange={e => setSessionId(e.currentTarget.value)} />
              <Button onClick={loadAttendance}>Load attendance</Button>
            </Group>
          </Paper>
          <Paper withBorder p="md">
            <Title order={4} mb="xs">Mark attendance</Title>
            <Text size="sm" c="dimmed" mb="sm">JSON array of {"{ studentId, status, minutesLate, note }"}; status: Present, Absent, Late or Excused.</Text>
            <Textarea value={attendanceJson} onChange={e => setAttendanceJson(e.currentTarget.value)} autosize minRows={4} mb="sm" />
            <Button onClick={() => run(async () => {
              const records = JSON.parse(attendanceJson);
              await api(`/api/sessions/${sessionId}/attendance`, { method: 'POST', body: { records } });
              return loadAttendance();
            }, 'Attendance saved')}>Save attendance</Button>
          </Paper>
          <Paper withBorder mt="md">
            <Table striped>
              <Table.Thead><Table.Tr><Table.Th>Student</Table.Th><Table.Th>Status</Table.Th><Table.Th>Minutes late</Table.Th><Table.Th>Method</Table.Th><Table.Th>Note</Table.Th></Table.Tr></Table.Thead>
              <Table.Tbody>{rows.map((r: any, i: number) => <Table.Tr key={r.studentId ?? i}>
                <Table.Td>{String(r.studentId ?? '').slice(0, 8)}</Table.Td>
                <Table.Td><Badge color={r.status === 'Present' ? 'green' : r.status === 'Absent' ? 'red' : 'yellow'}>{r.status}</Badge></Table.Td>
                <Table.Td>{r.minutesLate ?? 0}</Table.Td><Table.Td>{r.method ?? '—'}</Table.Td><Table.Td>{r.note ?? '—'}</Table.Td>
              </Table.Tr>)}</Table.Tbody>
            </Table>
          </Paper>
        </Tabs.Panel>

        <Tabs.Panel value="rooms" pt="md">
          <Paper withBorder p="md" maw={620}>
            <Title order={4} mb="xs">Check room availability</Title>
            <Stack gap="sm">
              <TextInput label="Room ID" value={roomId} onChange={e => setRoomId(e.currentTarget.value)} />
              <Group grow>
                <TextInput label="Start (ISO 8601)" value={startUtc} onChange={e => setStartUtc(e.currentTarget.value)} placeholder="2026-12-01T18:00:00Z" />
                <TextInput label="End (ISO 8601)" value={endUtc} onChange={e => setEndUtc(e.currentTarget.value)} placeholder="2026-12-01T20:00:00Z" />
              </Group>
              <Button onClick={() => run(async () => {
                const r: any = await api(`/api/rooms/${roomId}/availability`, { method: 'POST', body: { startUtc, endUtc } });
                setMsg(r.available ? 'Room is available' : 'Room is not available');
                return r;
              })}>Check</Button>
            </Stack>
          </Paper>
        </Tabs.Panel>
      </Tabs>
    </Stack>
  );
}
