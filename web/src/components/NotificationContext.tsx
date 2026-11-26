import { createContext, useCallback, useContext, useEffect, useState } from 'react';
import { HubConnectionBuilder } from '@microsoft/signalr';
import { api, asArray, getToken } from '../api';

interface NotificationState {
  items: any[];
  status: string;
  error: string;
  unreadCount: number;
  refresh: () => Promise<void>;
  markRead: (id: string) => Promise<void>;
}

const NotificationContext = createContext<NotificationState>({
  items: [], status: 'offline', error: '', unreadCount: 0,
  refresh: async () => {}, markRead: async () => {},
});

export const useNotifications = () => useContext(NotificationContext);

/** Owns the single SignalR connection and notification state shared by the header bell and the full page. */
export function NotificationsProvider({ children }: { children: React.ReactNode }) {
  const [items, setItems] = useState<any[]>([]);
  const [status, setStatus] = useState('connecting…');
  const [error, setError] = useState('');

  const refresh = useCallback(async () => {
    try {
      const r = await api('/api/communication/notifications');
      setItems(asArray(r));
      setError('');
    } catch (e: any) { setError(e.message); }
  }, []);

  useEffect(() => {
    void refresh();
    const connection = new HubConnectionBuilder()
      .withUrl('/hubs/notifications', { accessTokenFactory: () => getToken() })
      .withAutomaticReconnect()
      .build();
    connection.on('notification', (n: any) => setItems(prev => [n, ...prev]));
    connection.start()
      .then(() => setStatus('connected'))
      .catch(() => setStatus('offline'));
    return () => { void connection.stop(); };
  }, [refresh]);

  const markRead = useCallback(async (id: string) => {
    try {
      await api(`/api/communication/notifications/${id}/read`, { method: 'POST' });
      setItems(prev => prev.map(n => n.id === id ? { ...n, isRead: true } : n));
    } catch (e: any) { setError(e.message); }
  }, []);

  const unreadCount = items.filter(n => !n.isRead).length;

  return (
    <NotificationContext.Provider value={{ items, status, error, unreadCount, refresh, markRead }}>
      {children}
    </NotificationContext.Provider>
  );
}
