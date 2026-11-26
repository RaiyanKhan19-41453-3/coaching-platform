import { useState } from 'react';
import { AppShell, NavLink, Group, Title, Badge, Button, Text } from '@mantine/core';
import { IconGauge, IconSchool, IconCalendar, IconCalendarEvent, IconClipboardCheck, IconCash, IconUsers, IconMessages, IconMessage, IconChartBar, IconFile, IconBook, IconBabyCarriage, IconSearch, IconSettings, IconLogout } from '@tabler/icons-react';
import { Link, useLocation, useNavigate } from 'react-router-dom';
import { clearToken } from '../api';
import { NotificationBell } from './NotificationBell';

export const nav = [
  { to:'/', label:'Dashboard', icon:IconGauge },
  { to:'/academics', label:'Academics', icon:IconSchool },
  { to:'/scheduling', label:'Scheduling', icon:IconCalendar },
  { to:'/timetable', label:'Timetable & attendance', icon:IconCalendarEvent },
  { to:'/assessment', label:'Assessment', icon:IconClipboardCheck },
  { to:'/finance', label:'Finance', icon:IconCash },
  { to:'/crm', label:'CRM / Admissions', icon:IconUsers },
  { to:'/users', label:'Users', icon:IconUsers },
  { to:'/communication', label:'Communication', icon:IconMessages },
  { to:'/messages', label:'Messages', icon:IconMessage },
  { to:'/reports', label:'Reports', icon:IconChartBar },
  { to:'/files', label:'File uploads', icon:IconFile },
  { to:'/materials', label:'Course materials', icon:IconBook },
  { to:'/parent', label:'Parent portal', icon:IconBabyCarriage },
  { to:'/search', label:'Search', icon:IconSearch },
  { to:'/operations', label:'Operations', icon:IconSettings },
];

export function Layout({ children }: { children: React.ReactNode }) {
  const location = useLocation();
  const navigate = useNavigate();
  const [opened, setOpened] = useState(false);
  return (
    <AppShell header={{ height: 60 }} navbar={{ width: 240, breakpoint: 'sm', collapsed: { mobile: !opened } }} padding="md">
      <AppShell.Header>
        <Group h="100%" px="md" justify="space-between">
          <Group><Title order={4}>Coaching Platform</Title><Badge color="teal" variant="light">Multi-tenant</Badge></Group>
          <Group gap="xs">
            <NotificationBell />
            <Button variant="subtle" color="red" leftSection={<IconLogout size={16}/>} onClick={()=>{clearToken(); navigate('/login');}}>Sign out</Button>
          </Group>
        </Group>
      </AppShell.Header>
      <AppShell.Navbar p="md">
        <Text fw={600} size="xs" c="dimmed" mb="sm">WORKSPACE</Text>
        {nav.map(n => <NavLink key={n.to} component={Link} to={n.to} label={n.label} leftSection={<n.icon size={18}/>} active={location.pathname===n.to || (n.to!=='/' && location.pathname.startsWith(n.to))} onClick={()=>setOpened(false)} mb={2}/>)}
      </AppShell.Navbar>
      <AppShell.Main>{children}</AppShell.Main>
    </AppShell>
  );
}
