import { useEffect, useState } from 'react';
import { Stack, Title, Text, Paper, Tabs, Button, Group, TextInput, Textarea, Select, Checkbox, Badge, Alert, Table, ScrollArea } from '@mantine/core';
import { api, asArray } from '../api';

/** Direct messaging and notification preferences. */
export function Messaging() {
  const [conversations, setConversations] = useState<any[]>([]);
  const [active, setActive] = useState<string>('');
  const [messages, setMessages] = useState<any[]>([]);
  const [recipientId, setRecipientId] = useState('');
  const [body, setBody] = useState('');
  const [msg, setMsg] = useState(''), [err, setErr] = useState('');

  const [channel, setChannel] = useState('InApp'), [type, setType] = useState('announcement.published');
  const [enabled, setEnabled] = useState(true), [digest, setDigest] = useState('Immediate');

  const loadConversations = () => api('/api/communication/conversations').then(r => {
    const list = asArray(r); setConversations(list); if (list[0] && !active) setActive(list[0].id);
  }).catch(e => setErr(e.message));

  const loadMessages = (id: string) => api(`/api/communication/conversations/${id}/messages`)
    .then(r => setMessages(asArray(r))).catch(e => setErr(e.message));

  useEffect(() => { void loadConversations(); }, []);

  const send = async () => {
    setErr(''); setMsg('');
    try {
      const r: any = await api('/api/communication/messages', { body: { conversationId: active || null, recipientId, body } });
      setMsg('Message sent'); setBody('');
      await loadConversations(); if (r.conversationId ?? active) await loadMessages(r.conversationId ?? active);
    } catch (e: any) { setErr(e.message); }
  };

  const savePreference = async () => {
    setErr(''); setMsg('');
    try {
      await api('/api/communication/notification-preferences', { method: 'PUT', body: { channel, type, isEnabled: enabled, digestFrequency: digest } });
      setMsg('Preference saved');
    } catch (e: any) { setErr(e.message); }
  };

  return (
    <Stack gap="md">
      <div><Title order={2}>Messages & preferences</Title><Text c="dimmed">Conversations, direct messages and notification delivery channels</Text></div>
      {err && <Alert color="red">{err}</Alert>}
      {msg && <Alert color="green">{msg}</Alert>}

      <Tabs defaultValue="messages">
        <Tabs.List><Tabs.Tab value="messages">Messages</Tabs.Tab><Tabs.Tab value="preferences">Notification preferences</Tabs.Tab></Tabs.List>

        <Tabs.Panel value="messages" pt="md">
          <Group align="flex-start" grow>
            <Paper withBorder p="sm" maw={320} w="100%">
              <Title order={5} mb="sm">Conversations</Title>
              <Stack gap={4}>
                {conversations.length === 0 && <Text size="sm" c="dimmed">No conversations yet</Text>}
                {conversations.map(c => <Button key={c.id} variant={active === c.id ? 'filled' : 'subtle'} justify="flex-start"
                  onClick={() => { setActive(c.id); void loadMessages(c.id); }} fullWidth>
                  {c.subject ?? c.title ?? String(c.id).slice(0, 8)} {c.unreadCount > 0 && <Badge size="xs" color="red" ml="xs">{c.unreadCount}</Badge>}
                </Button>)}
              </Stack>
              <Button size="xs" variant="light" mt="sm" fullWidth onClick={loadConversations}>Refresh</Button>
            </Paper>

            <Stack gap="sm">
              <Paper withBorder p="md">
                <Group justify="space-between">
                  <Title order={4}>Thread</Title>
                  {active && <Button size="xs" variant="subtle" onClick={() => api(`/api/communication/conversations/${active}/read`, { method: 'POST' }).then(() => loadConversations())}>Mark read</Button>}
                </Group>
                <ScrollArea h={280} mt="sm">
                  {messages.length === 0 && <Text size="sm" c="dimmed">Select a conversation</Text>}
                  {messages.map(m => <Paper key={m.id} withBorder p="xs" mb="xs" bg="gray.0">
                    <Text size="sm">{m.body}</Text>
                    <Text size="xs" c="dimmed">{m.senderName ?? m.senderId ?? ''} · {m.sentAt ? new Date(m.sentAt).toLocaleString() : ''}</Text>
                  </Paper>)}
                </ScrollArea>
              </Paper>
              <Paper withBorder p="md">
                <Title order={5} mb="sm">New message</Title>
                <Stack gap="sm">
                  <TextInput label="Recipient user ID" value={recipientId} onChange={e => setRecipientId(e.currentTarget.value)} />
                  <Textarea label="Message" value={body} onChange={e => setBody(e.currentTarget.value)} autosize minRows={3} />
                  <Group justify="flex-end"><Button onClick={send} disabled={!recipientId || !body.trim()}>Send</Button></Group>
                </Stack>
              </Paper>
            </Stack>
          </Group>
        </Tabs.Panel>

        <Tabs.Panel value="preferences" pt="md">
          <Paper withBorder p="md" maw={560}>
            <Title order={4} mb="xs">Notification channel preference</Title>
            <Text size="sm" c="dimmed" mb="md">Control how one notification type is delivered.</Text>
            <Stack gap="sm">
              <Select label="Channel" data={[{ value: 'InApp', label: 'In app' }, { value: 'Email', label: 'Email' }, { value: 'Sms', label: 'SMS' }, { value: 'Push', label: 'Push' }]}
                value={channel} onChange={v => setChannel(v ?? 'InApp')} />
              <TextInput label="Notification type" value={type} onChange={e => setType(e.currentTarget.value)} />
              <Checkbox label="Enabled" checked={enabled} onChange={e => setEnabled(e.currentTarget.checked)} />
              <Select label="Digest frequency" data={['Immediate', 'Hourly', 'Daily', 'Weekly'].map(v => ({ value: v, label: v }))}
                value={digest} onChange={v => setDigest(v ?? 'Immediate')} />
              <Group><Button onClick={savePreference}>Save preference</Button></Group>
            </Stack>
          </Paper>
          <Paper withBorder p="md" mt="md">
            <Title order={4} mb="sm">Preference overview</Title>
            <Table striped>
              <Table.Thead><Table.Tr><Table.Th>Channel</Table.Th><Table.Th>Type</Table.Th><Table.Th>Enabled</Table.Th><Table.Th>Digest</Table.Th></Table.Tr></Table.Thead>
              <Table.Tbody><Table.Tr><Table.Td>{channel}</Table.Td><Table.Td>{type}</Table.Td><Table.Td>{enabled ? 'Yes' : 'No'}</Table.Td><Table.Td>{digest}</Table.Td></Table.Tr></Table.Tbody>
            </Table>
          </Paper>
        </Tabs.Panel>
      </Tabs>
    </Stack>
  );
}
