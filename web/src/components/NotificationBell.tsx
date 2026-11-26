import { ActionIcon, Indicator, Popover, Stack, Group, Text, Badge, Button, ScrollArea, Divider } from '@mantine/core';
import { IconBell } from '@tabler/icons-react';
import { Link } from 'react-router-dom';
import { useNotifications } from './NotificationContext';

/** Header bell: unread badge, live dropdown with quick mark-read, and a link to the full page. */
export function NotificationBell() {
  const { items, status, unreadCount, markRead, refresh } = useNotifications();

  return (
    <Popover width={380} position="bottom-end" withArrow shadow="md" withinPortal>
      <Popover.Target>
        <ActionIcon
          variant="subtle"
          size="lg"
          aria-label="Notifications"
          title="Notifications"
          onClick={() => void refresh()}
        >
          <Indicator
            label={unreadCount > 9 ? '9+' : unreadCount}
            disabled={unreadCount === 0}
            size={17}
            color="red"
            offset={6}
          >
            <IconBell size={20} />
          </Indicator>
        </ActionIcon>
      </Popover.Target>
      <Popover.Dropdown p={0}>
        <Group justify="space-between" px="md" py="sm">
          <Text fw={700}>Notifications</Text>
          <Badge color={status === 'connected' ? 'green' : 'gray'} size="sm" variant="light">
            {status}
          </Badge>
        </Group>
        <Divider />
        <ScrollArea h={330}>
          <Stack gap={0}>
            {items.length === 0 && (
              <Text c="dimmed" size="sm" ta="center" py="xl">No notifications yet.</Text>
            )}
            {items.slice(0, 25).map(n => (
              <div key={n.id} style={{ padding: '10px 16px', borderBottom: '1px solid #eceef0', background: n.isRead ? undefined : '#f0fdfa' }}>
                <Group justify="space-between" wrap="nowrap" gap="xs">
                  <Text size="sm" fw={n.isRead ? 500 : 700} style={{ flex: 1 }} lineClamp={1}>{n.title}</Text>
                  {!n.isRead && (
                    <Button size="xs" variant="subtle" onClick={() => void markRead(n.id)}>Mark read</Button>
                  )}
                </Group>
                <Text size="xs" c="dimmed" lineClamp={2} mt={2}>{n.body}</Text>
                <Text size="xs" c="dimmed" mt={2}>
                  {n.sentAt ? new Date(n.sentAt).toLocaleString() : ''}
                </Text>
              </div>
            ))}
          </Stack>
        </ScrollArea>
        <Divider />
        <Group justify="space-between" px="md" py="xs">
          <Text size="xs" c="dimmed">{unreadCount} unread</Text>
          <Button component={Link} to="/notifications" size="xs" variant="subtle">View all</Button>
        </Group>
      </Popover.Dropdown>
    </Popover>
  );
}
