import { useEffect, useState } from 'react';
import { Stack, Title, Group, Button, Table, Paper, Tabs, TextInput, Textarea, Alert, Badge, Text } from '@mantine/core';
import { api, asArray } from '../api';

const sections = [
  { key:'attendance', label:'Attendance', path:'/api/reports/attendance', cols:['studentId','batchId','period','sessions','present','absent','attendancePercent'] },
  { key:'fees', label:'Fees', path:'/api/reports/fees', cols:['studentId','period','invoiced','paid','outstanding','currency'] },
  { key:'utilization', label:'Utilization', path:'/api/reports/utilization', cols:['batchId','capacity','enrolled','waitlisted','utilizationPercent'] },
  { key:'grades', label:'Grades', path:'/api/reports/grades', cols:['studentId','subjectId','average','highest','lowest','assessments'] },
  { key:'audit', label:'Audit log', path:'/api/reports/audit', cols:['entityName','entityId','action','actorEmail','occurredAt'] },
];

export function Reports() {
  const [tab, setTab] = useState<string|null>('attendance');
  const [rows, setRows] = useState<any[]>([]);
  const [err, setErr] = useState('');
  const [flagKey, setFlagKey] = useState(''); const [flagValue, setFlagValue] = useState('');
  const [exportJson, setExportJson] = useState('');
  const active = sections.find(s=>s.key===tab);
  useEffect(()=>{
    if (!active) return;
    setErr(''); api(active.path).then(r=>setRows(asArray(r))).catch(e=>setErr(e.message));
  },[tab]);
  return <Stack gap="md">
    <Title order={2}>Reports & administration</Title>
    {err && <Alert color="red">{err}</Alert>}
    <Tabs value={tab} onChange={setTab}>
      <Tabs.List>{sections.map(s=><Tabs.Tab key={s.key} value={s.key}>{s.label}</Tabs.Tab>)}<Tabs.Tab value="tools">Tools</Tabs.Tab></Tabs.List>
      {sections.map(s=><Tabs.Panel key={s.key} value={s.key} pt="md">
        <Paper withBorder><Table.ScrollContainer minWidth={700}><Table striped>
          <Table.Thead><Table.Tr>{s.cols.map(c=><Table.Th key={c}>{c}</Table.Th>)}</Table.Tr></Table.Thead>
          <Table.Tbody>{rows.length===0 && <Table.Tr><Table.Td colSpan={s.cols.length}><Text c="dimmed">No data</Text></Table.Td></Table.Tr>}
            {rows.map((r:any,i)=><Table.Tr key={i}>{s.cols.map(c=><Table.Td key={c}>{String(r[c] ?? '—').slice(0,80)}</Table.Td>)}</Table.Tr>)}
          </Table.Tbody></Table></Table.ScrollContainer></Paper>
      </Tabs.Panel>)}
      <Tabs.Panel value="tools" pt="md"><Stack>
        <Paper withBorder p="md"><Title order={4} mb="sm">Feature flags</Title><Group>
          <TextInput label="Key" value={flagKey} onChange={e=>setFlagKey(e.currentTarget.value)}/>
          <TextInput label="Targeting JSON" value={flagValue} onChange={e=>setFlagValue(e.currentTarget.value)} placeholder='{"rollout":100}'/>
          <Button onClick={()=>api('/api/reports/features',{body:{key:flagKey,description:'',isEnabled:true,targetingJson:flagValue||null}}).then(()=>setErr('Saved')).catch(e=>setErr(e.message))}>Enable</Button>
        </Group></Paper>
        <Paper withBorder p="md"><Title order={4} mb="sm">Data export / erasure</Title><Group>
          <TextInput label="Student ID" id="student-id" placeholder="GUID"/>
          <Button onClick={()=>{const id=(document.getElementById('student-id') as HTMLInputElement).value; api(`/api/students/${id}/export`).then(r=>setExportJson(JSON.stringify(r,null,2))).catch(e=>setErr(e.message));}}>Export</Button>
          <Button color="red" onClick={()=>{const id=(document.getElementById('student-id') as HTMLInputElement).value; api(`/api/students/${id}/erase`,{method:'POST'}).then(()=>setErr('Erased')).catch(e=>setErr(e.message));}}>Erase</Button>
        </Group>{exportJson && <Textarea mt="sm" label="Export result" value={exportJson} readOnly autosize minRows={4}/>}</Paper>
        <Badge color="blue">Audit events are append-only and secret-redacted</Badge>
      </Stack></Tabs.Panel>
    </Tabs>
  </Stack>;
}
