import { useState } from 'react';
import { MantineProvider, createTheme } from '@mantine/core';
import { Notifications } from '@mantine/notifications';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { getToken } from './api';
import { Layout } from './components/Layout';
import { Login } from './components/Login';
import { Dashboard } from './components/Dashboard';
import { ResourcePage } from './components/ResourcePage';
import { Assets } from './components/Assets';
import { Reports } from './components/Reports';
import { Parent } from './components/Parent';
import { Search } from './components/Search';
import { Notifications as NotificationsPage } from './components/Notifications';
import { Operations } from './components/Operations';
import { Timetable } from './components/Timetable';
import { Messaging } from './components/Messaging';
import { resourceByKey } from './resources';

const theme = createTheme({ primaryColor:'teal', fontFamily:'Inter, system-ui, sans-serif', defaultRadius:'md' });
const R = ({ k }: { k:string }) => { const r=resourceByKey(k); return r ? <ResourcePage resource={r}/> : <Navigate to="/"/>; };

export default function App() {
  const [token, setToken] = useState(getToken());
  if (!token) return <MantineProvider theme={theme}><Notifications/><Login onSuccess={()=>setToken(getToken())}/></MantineProvider>;
  return <MantineProvider theme={theme}><Notifications/><BrowserRouter><Layout><Routes>
    <Route path="/" element={<Dashboard/>}/>
    <Route path="/academics" element={<R k="courses"/>}/>
    <Route path="/academics/courses" element={<R k="courses"/>}/>
    <Route path="/academics/batches" element={<R k="batches"/>}/>
    <Route path="/academics/enrollments" element={<R k="enrollments"/>}/>
    <Route path="/scheduling" element={<R k="schedule-rules"/>}/>
    <Route path="/scheduling/rules" element={<R k="schedule-rules"/>}/>
    <Route path="/scheduling/rooms" element={<R k="rooms"/>}/>
    <Route path="/scheduling/leave" element={<R k="leave-requests"/>}/>
    <Route path="/timetable" element={<Timetable/>}/>
    <Route path="/assessment" element={<R k="assessments"/>}/>
    <Route path="/assessment/assessments" element={<R k="assessments"/>}/>
    <Route path="/assessment/questions" element={<R k="questions"/>}/>
    <Route path="/finance" element={<R k="fee-structures"/>}/>
    <Route path="/finance/fees" element={<R k="fee-structures"/>}/>
    <Route path="/finance/invoices" element={<R k="invoices"/>}/>
    <Route path="/finance/discounts" element={<R k="discounts"/>}/>
    <Route path="/finance/scholarships" element={<R k="scholarships"/>}/>
    <Route path="/crm" element={<R k="leads"/>}/>
    <Route path="/crm/leads" element={<R k="leads"/>}/>
    <Route path="/crm/applications" element={<R k="applications"/>}/>
    <Route path="/crm/follow-ups" element={<R k="follow-ups"/>}/>
    <Route path="/users" element={<R k="users"/>}/>
    <Route path="/communication" element={<R k="announcements"/>}/>
    <Route path="/communication/announcements" element={<R k="announcements"/>}/>
    <Route path="/messages" element={<Messaging/>}/>
    <Route path="/reports" element={<Reports/>}/>
    <Route path="/files" element={<Assets/>}/>
    <Route path="/materials" element={<R k="materials"/>}/>
    <Route path="/parent" element={<Parent/>}/>
    <Route path="/search" element={<Search/>}/>
    <Route path="/notifications" element={<NotificationsPage/>}/>
    <Route path="/operations" element={<Operations/>}/>
    <Route path="*" element={<Navigate to="/"/>}/>
  </Routes></Layout></BrowserRouter></MantineProvider>;
}
