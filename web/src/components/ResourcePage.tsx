import { useEffect, useState } from 'react';
import { Button, Group, Modal, Stack, Table, TextInput, Select, NumberInput, Textarea, Title, Paper, Badge, Text, ActionIcon, Alert } from '@mantine/core';
import { useDisclosure } from '@mantine/hooks';
import { IconPlus, IconPencil } from '@tabler/icons-react';
import { api, apiBlob, asArray, saveBlob } from '../api';
import type { Resource, Field, Action } from '../resources';

function convert(value: any, field: Field): any {
  if (value === '' || value === undefined) return undefined;
  if (field.name === 'roles') return String(value).split(',').map(s=>s.trim()).filter(Boolean);
  if (field.name === 'lines' || field.name.endsWith('Json')) { try { return JSON.parse(value); } catch { return value; } }
  if (field.type === 'select' && (value === 'true' || value === 'false')) return value === 'true';
  if (field.type === 'number' || (field.type === 'select' && !isNaN(Number(value)))) return Number(value);
  return value;
}

function FieldInput({ field, value, onChange }: { field: Field; value: any; onChange: (v:any)=>void }) {
  const common = { label: field.label, placeholder: field.placeholder, required: field.required, value: value ?? '' };
  if (field.type === 'select') return <Select {...common} data={field.options ?? []} onChange={onChange}/>;
  if (field.type === 'number') return <NumberInput {...common} onChange={onChange}/>;
  if (field.type === 'textarea') return <Textarea {...common} onChange={(e)=>onChange(e.currentTarget.value)} autosize minRows={2}/>;
  if (field.type === 'datetime') return <TextInput {...common} type="datetime-local" onChange={(e)=>onChange(e.currentTarget.value ? new Date(e.currentTarget.value).toISOString() : '')}/>;
  if (field.type === 'date') return <TextInput {...common} type="date" onChange={(e)=>onChange(e.currentTarget.value)}/>;
  return <TextInput {...common} type={field.type === 'password' ? 'password' : 'text'} onChange={(e)=>onChange(e.currentTarget.value)}/>;
}

export function ResourcePage({ resource }: { resource: Resource }) {
  const [rows, setRows] = useState<any[]>([]);
  const [values, setValues] = useState<Record<string, any>>({});
  const [error, setError] = useState('');
  const [busy, setBusy] = useState(false);
  const [action, setAction] = useState<{ def: Action; id: string; values: Record<string,any> } | null>(null);
  const [opened, { open, close }] = useDisclosure(false);

  const load = async () => {
    setError('');
    try { setRows(asArray(await api(resource.path))); }
    catch (e:any) { setError(e.message); }
  };
  useEffect(()=>{ void load(); }, [resource.key]);

  const submit = async () => {
    setBusy(true); setError('');
    try {
      const body: Record<string,any> = {};
      for (const f of resource.fields ?? []) {
        const v = convert(values[f.name], f);
        if (v !== undefined) body[f.name] = v;
      }
      await api(resource.createPath ?? resource.path, { body });
      setValues({}); close(); await load();
    } catch (e:any) { setError(e.message); }
    finally { setBusy(false); }
  };

  const runAction = async () => {
    if (!action) return;
    setBusy(true); setError('');
    try {
      const body: Record<string,any> = {};
      for (const [key, initial] of Object.entries(action.def.body ?? {})) {
        let v: any = action.values[key] ?? initial;
        if (action.def.jsonFields?.includes(key)) { try { v = JSON.parse(v); } catch { /* send raw */ } }
        else if (typeof initial === 'number') v = Number(v);
        else if (typeof initial === 'boolean') v = v === true || v === 'true';
        body[key] = v;
      }
      const path = action.def.path(action.id, body);
      if (action.def.kind === 'download') {
        const { blob, fileName } = await apiBlob(path);
        saveBlob(blob, fileName);
      } else {
        await api(path, { method: action.def.method ?? 'POST', body: Object.keys(body).length ? body : undefined });
      }
      setAction(null); await load();
    } catch (e:any) { setError(e.message); }
    finally { setBusy(false); }
  };

  const renderCell = (row:any, col:string) => {
    const v = row[col];
    if (v === null || v === undefined) return '—';
    if (typeof v === 'boolean') return <Badge color={v?'green':'gray'}>{v?'Yes':'No'}</Badge>;
    if (Array.isArray(v)) return v.join(', ');
    if (typeof v === 'object') return JSON.stringify(v).slice(0,80);
    return String(v).slice(0,120);
  };

  return (
    <Stack gap="md">
      <Group justify="space-between">
        <div><Title order={2}>{resource.label}</Title><Text c="dimmed" size="sm">{rows.length} record{rows.length===1?'':'s'}</Text></div>
        {!resource.noCreate && <Button leftSection={<IconPlus size={16}/>} onClick={open}>New {resource.label.replace(/s$/,'')}</Button>}
      </Group>
      {error && <Alert color="red">{error}</Alert>}
      <Paper withBorder p={0}>
        <Table.ScrollContainer minWidth={700}>
          <Table striped highlightOnHover>
            <Table.Thead><Table.Tr>{resource.columns.map(c=><Table.Th key={c}>{c.replace(/([A-Z])/g,' $1').replace(/^./,s=>s.toUpperCase())}</Table.Th>)}<Table.Th>Actions</Table.Th></Table.Tr></Table.Thead>
            <Table.Tbody>
              {rows.length===0 && <Table.Tr><Table.Td colSpan={resource.columns.length+1}><Text c="dimmed" ta="center" py="lg">No records yet</Text></Table.Td></Table.Tr>}
              {rows.map((row:any)=><Table.Tr key={row.id}>
                {resource.columns.map(c=><Table.Td key={c}>{renderCell(row,c)}</Table.Td>)}
                <Table.Td><Group gap={4} wrap="nowrap">
                  {resource.actions?.map(a=><Button key={a.label} size="xs" variant="light" onClick={()=>setAction({def:a,id:row.id,values:{}})}>{a.label}</Button>)}
                  {resource.detailPath && <ActionIcon size="sm" variant="subtle" title="Details" onClick={()=>void api(resource.detailPath!(row.id)).then(r=>alert(JSON.stringify(r,null,2))).catch((e:any)=>setError(e.message))}><IconPencil size={14}/></ActionIcon>}
                </Group></Table.Td>
              </Table.Tr>)}
            </Table.Tbody>
          </Table>
        </Table.ScrollContainer>
      </Paper>

      <Modal opened={opened} onClose={close} title={`New ${resource.label.replace(/s$/,'')}`} size="lg">
        <Stack gap="sm">
          {(resource.fields ?? []).map(f=><FieldInput key={f.name} field={f} value={values[f.name]} onChange={v=>setValues(s=>({...s,[f.name]:v}))}/>)}
          <Group justify="flex-end"><Button variant="subtle" onClick={close}>Cancel</Button><Button loading={busy} onClick={submit}>Create</Button></Group>
        </Stack>
      </Modal>

      <Modal opened={!!action} onClose={()=>setAction(null)} title={action?.def.label ?? ''} size="sm">
        <Stack gap="sm">
          {action && Object.keys(action.def.body ?? {}).map(k=><TextInput key={k} label={k.replace(/([A-Z])/g,' $1')} value={action.values[k] ?? ''} onChange={e=>setAction({...action,values:{...action.values,[k]:e.currentTarget.value}})}/>)}
          <Group justify="flex-end"><Button variant="subtle" onClick={()=>setAction(null)}>Cancel</Button><Button loading={busy} onClick={runAction}>Run</Button></Group>
        </Stack>
      </Modal>
    </Stack>
  );
}
