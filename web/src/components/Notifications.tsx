import { Stack, Title, Card, Group, Badge, Text, Button, Alert } from '@mantine/core';
import { useNotifications } from './NotificationContext';

export function Notifications() {
  const { items, status, error, markRead } = useNotifications();

  return <Stack gap="md">
    <Group justify="space-between"><Title order={2}>Notifications</Title><Badge color={status === 'connected' ? 'green' : 'gray'}>{status}</Badge></Group>
    {error && <Alert color="red">{error}</Alert>}
    {items.length === 0 && <Text c="dimmed">No notifications yet.</Text>}
    {items.map(n => <Card key={n.id} withBorder padding="sm">
      <Group justify="space-between"><Text fw={600}>{n.title}</Text><Group gap="xs"><Badge size="xs" variant="light">{n.type}</Badge>{!n.isRead && <Button size="xs" variant="subtle" onClick={() => void markRead(n.id)}>Mark read</Button>}</Group></Group>
      <Text size="sm" c="dimmed">{n.body}</Text><Text size="xs" c="dimmed">{n.sentAt ? new Date(n.sentAt).toLocaleString() : ''}</Text>
    </Card>)}
  </Stack>;
}
