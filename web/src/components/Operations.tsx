import { useState } from 'react';
import { Stack, Title, Text, Tabs, Paper, Button, Group, TextInput, Select, Textarea, Badge, Alert, Table, NumberInput } from '@mantine/core';
import { api, apiBlob, saveBlob } from '../api';

const enrolmentStatuses = ['Applied','Waitlisted','Enrolled','Suspended','Transferred','Completed','Dropped']
  .map(v => ({ value: v, label: v }));

/** Administrative operations that do not fit the generic CRUD grid. */
export function Operations() {
  const [msg, setMsg] = useState('');
  const [err, setErr] = useState('');
  const [out, setOut] = useState<any>(null);

  const [key, setKey] = useState('');
  const [tenantId, setTenantId] = useState('');
  const [examId, setExamId] = useState('');
  const [candidateIds, setCandidateIds] = useState('');
  const [publishedCount, setPublishedCount] = useState(1);

  const [tName, setTName] = useState(''), [tSlug, setTSlug] = useState(''), [tEmail, setTEmail] = useState('');
  const [tPass, setTPass] = useState('Password123!'), [tFirst, setTFirst] = useState('Ada'), [tLast, setTLast] = useState('Lovelace');

  const [studentId, setStudentId] = useState(''), [batchId, setBatchId] = useState('');
  const [enrollmentId, setEnrollmentId] = useState(''), [enrollmentStatus, setEnrollmentStatus] = useState('Enrolled'), [reason, setReason] = useState('');

  const run = async (fn: () => Promise<any>, success: string) => {
    setErr(''); setMsg('');
    try { const r = await fn(); setOut(r); setMsg(success); }
    catch (e: any) { setErr(e.message); }
  };

  return (
    <Stack gap="md">
      <div><Title order={2}>Operations</Title><Text c="dimmed">Administrative jobs, integrations, provisioning and student records</Text></div>
      {err && <Alert color="red" title="Request failed">{err}</Alert>}
      {msg && <Alert color="green" title="Done">{msg}</Alert>}

      <Tabs defaultValue="jobs">
        <Tabs.List>
          <Tabs.Tab value="jobs">Background jobs</Tabs.Tab>
          <Tabs.Tab value="integration">Integration</Tabs.Tab>
          <Tabs.Tab value="tenants">Tenant provisioning</Tabs.Tab>
          <Tabs.Tab value="enrollments">Enrolment status</Tabs.Tab>
          <Tabs.Tab value="reportcards">Report cards</Tabs.Tab>
        </Tabs.List>

        <Tabs.Panel value="jobs" pt="md">
          <Paper withBorder p="md">
            <Title order={4} mb="xs">Outbox & projections</Title>
            <Text size="sm" c="dimmed" mb="md">Manually drain queued integration events or rebuild read-model projections.</Text>
            <Group>
              <Button onClick={() => run(() => api('/api/jobs/outbox/dispatch', { method: 'POST' }), 'Outbox dispatched')}>Dispatch outbox</Button>
              <Button variant="light" onClick={() => run(() => api('/api/jobs/projections/rebuild', { method: 'POST' }), 'Projections rebuilt')}>Rebuild projections</Button>
            </Group>
          </Paper>
        </Tabs.Panel>

        <Tabs.Panel value="integration" pt="md">
          <Paper withBorder p="md">
            <Title order={4} mb="xs">Push exam results to the platform</Title>
            <Text size="sm" c="dimmed" mb="md">The proctored exam service posts here with the configured integration key.</Text>
            <Stack gap="sm">
              <TextInput label="X-Integration-Key" value={key} onChange={e => setKey(e.currentTarget.value)} />
              <TextInput label="Tenant ID" value={tenantId} onChange={e => setTenantId(e.currentTarget.value)} />
              <TextInput label="Exam ID" value={examId} onChange={e => setExamId(e.currentTarget.value)} />
              <TextInput label="Candidate IDs (comma separated)" value={candidateIds} onChange={e => setCandidateIds(e.currentTarget.value)} />
              <NumberInput label="Published count" value={publishedCount} onChange={v => setPublishedCount(Number(v))} min={0} />
              <Group>
                <Button onClick={() => run(() => api('/api/integrations/exam-results', {
                  method: 'POST', headers: { 'X-Integration-Key': key },
                  body: {
                    tenantId, examId, publishedCount,
                    candidateIds: candidateIds.split(',').map(s => s.trim()).filter(Boolean),
                  },
                }), 'Exam results published')}>Notify candidates</Button>
              </Group>
            </Stack>
          </Paper>
        </Tabs.Panel>

        <Tabs.Panel value="tenants" pt="md">
          <Paper withBorder p="md">
            <Title order={4} mb="xs">Register a new tenant</Title>
            <Text size="sm" c="dimmed" mb="md">Creates an isolated center, its admin user and the default roles.</Text>
            <Stack gap="sm">
              <Group grow>
                <TextInput label="Center name" value={tName} onChange={e => setTName(e.currentTarget.value)} />
                <TextInput label="Slug" value={tSlug} onChange={e => setTSlug(e.currentTarget.value)} />
              </Group>
              <Group grow>
                <TextInput label="Admin email" value={tEmail} onChange={e => setTEmail(e.currentTarget.value)} />
                <TextInput label="Admin password" value={tPass} onChange={e => setTPass(e.currentTarget.value)} />
              </Group>
              <Group grow>
                <TextInput label="First name" value={tFirst} onChange={e => setTFirst(e.currentTarget.value)} />
                <TextInput label="Last name" value={tLast} onChange={e => setTLast(e.currentTarget.value)} />
              </Group>
              <Group>
                <Button onClick={() => run(() => api('/api/auth/register-tenant', {
                  body: { name: tName, slug: tSlug, adminEmail: tEmail, adminPassword: tPass, adminFirstName: tFirst, adminLastName: tLast },
                }), 'Tenant provisioned')}>Register tenant</Button>
              </Group>
            </Stack>
          </Paper>
        </Tabs.Panel>

        <Tabs.Panel value="enrollments" pt="md">
          <Paper withBorder p="md">
            <Title order={4} mb="xs">Change an enrolment status</Title>
            <Stack gap="sm" maw={520}>
              <TextInput label="Enrolment ID" value={enrollmentId} onChange={e => setEnrollmentId(e.currentTarget.value)} />
              <Select label="Status" data={enrolmentStatuses} value={enrollmentStatus} onChange={v => setEnrollmentStatus(v ?? 'Enrolled')} />
              <TextInput label="Reason (optional)" value={reason} onChange={e => setReason(e.currentTarget.value)} />
              <Button onClick={() => run(() => api(`/api/enrollments/${enrollmentId}/status`, {
                method: 'POST', body: { status: enrollmentStatus, reason: reason || null },
              }), 'Enrolment updated')}>Update status</Button>
            </Stack>
          </Paper>
        </Tabs.Panel>

        <Tabs.Panel value="reportcards" pt="md">
          <Paper withBorder p="md">
            <Title order={4} mb="xs">Generate a report card</Title>
            <Text size="sm" c="dimmed" mb="md">Aggregates attendance, grades and fee standing for one student and batch.</Text>
            <Group align="flex-end" mb="md">
              <TextInput label="Student ID" value={studentId} onChange={e => setStudentId(e.currentTarget.value)} />
              <TextInput label="Batch ID" value={batchId} onChange={e => setBatchId(e.currentTarget.value)} />
              <Button onClick={() => run(async () => {
                const { blob, fileName } = await apiBlob(`/api/reports/report-cards?studentId=${studentId}&batchId=${batchId}`);
                saveBlob(blob, fileName.endsWith('.pdf') ? fileName : 'report-card.pdf');
                return { downloaded: true };
              }, 'Report card downloaded')}>Generate</Button>
            </Group>
            {Array.isArray(out) && out.length > 0 && (
              <Table striped>
                <Table.Thead><Table.Tr><Table.Th>Key</Table.Th><Table.Th>Value</Table.Th></Table.Tr></Table.Thead>
                <Table.Tbody>{Object.entries(out[0]).map(([k, v]) => (
                  <Table.Tr key={k}><Table.Td>{k}</Table.Td><Table.Td>{typeof v === 'object' ? JSON.stringify(v).slice(0, 120) : String(v)}</Table.Td></Table.Tr>
                ))}</Table.Tbody>
              </Table>
            )}
            {out && !Array.isArray(out) && <Textarea label="Result" value={JSON.stringify(out, null, 2)} readOnly autosize minRows={4} mt="sm" />}
          </Paper>
        </Tabs.Panel>
      </Tabs>
    </Stack>
  );
}
