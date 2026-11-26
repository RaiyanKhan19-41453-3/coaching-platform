import { useEffect, useState } from 'react';
import { Stack, Title, Group, Button, Table, Text, Alert, Paper, FileInput, Badge, ActionIcon } from '@mantine/core';
import { IconDownload, IconTrash } from '@tabler/icons-react';
import { api, asArray } from '../api';

export function Assets() {
  const [items, setItems] = useState<any[]>([]);
  const [file, setFile] = useState<File|null>(null);
  const [err, setErr] = useState(''); const [busy, setBusy] = useState(false);
  const load = () => api('/api/assets').then(r=>setItems(asArray(r))).catch(e=>setErr(e.message));
  useEffect(()=>{ void load(); },[]);
  const upload = async () => {
    if (!file) return; setBusy(true); setErr('');
    try {
      const r:any = await api('/api/assets/upload-url',{body:{fileName:file.name,contentType:file.type||'application/octet-stream',sizeBytes:file.size,ownerType:'Material'}});
      const put = await fetch(r.url,{method:'PUT',headers:{'Content-Type':file.type||'application/octet-stream'},body:file});
      if (!put.ok) throw new Error(`Upload failed: ${put.status}`);
      await api(`/api/assets/${r.id}/complete`,{method:'POST'});
      setFile(null); await load();
    } catch(e:any){ setErr(e.message); } finally { setBusy(false); }
  };
  const download = async (id:string) => { try { const r:any=await api(`/api/assets/${id}/url`); window.open(r.url,'_blank'); } catch(e:any){ setErr(e.message); } };
  return <Stack gap="md">
    <Title order={2}>Files & materials</Title>
    {err && <Alert color="red">{err}</Alert>}
    <Paper withBorder p="md"><Group align="flex-end">
      <FileInput label="Choose a file" placeholder="Select file" value={file} onChange={setFile} clearable/>
      <Button loading={busy} disabled={!file} onClick={upload}>Upload</Button>
    </Group><Text size="xs" c="dimmed" mt="sm">Files go straight to object storage using a presigned URL.</Text></Paper>
    <Paper withBorder><Table.ScrollContainer minWidth={600}><Table>
      <Table.Thead><Table.Tr><Table.Th>File</Table.Th><Table.Th>Size</Table.Th><Table.Th>Status</Table.Th><Table.Th>Uploaded</Table.Th><Table.Th/></Table.Tr></Table.Thead>
      <Table.Tbody>{items.map(a=><Table.Tr key={a.id}>
        <Table.Td>{a.fileName}</Table.Td><Table.Td>{a.sizeBytes} B</Table.Td><Table.Td><Badge color={a.scanStatus===1?'green':'yellow'}>{a.scanStatus===1?'ready':'pending'}</Badge></Table.Td>
        <Table.Td>{a.createdAt?new Date(a.createdAt).toLocaleDateString():'—'}</Table.Td>
        <Table.Td><Group gap={4}><ActionIcon size="sm" onClick={()=>download(a.id)} title="Download"><IconDownload size={16}/></ActionIcon><ActionIcon size="sm" color="red" onClick={()=>api(`/api/assets/${a.id}`,{method:'DELETE'}).then(load)} title="Delete"><IconTrash size={16}/></ActionIcon></Group></Table.Td>
      </Table.Tr>)}</Table.Tbody>
    </Table></Table.ScrollContainer></Paper>
  </Stack>;
}
