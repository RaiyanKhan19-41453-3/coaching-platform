export type FieldType = 'text' | 'number' | 'select' | 'textarea' | 'date' | 'datetime' | 'password';
export interface Field { name: string; label: string; type?: FieldType; options?: { value: string; label: string }[]; required?: boolean; default?: any; placeholder?: string; }
export interface Action {
  label: string;
  path: (id: string, values?: Record<string, any>) => string;
  method?: string;
  body?: Record<string, unknown>;
  jsonFields?: string[];
  kind?: 'download';
  confirm?: string;
}
export interface Resource {
  key: string; label: string; path: string; fields?: Field[]; columns: string[];
  createPath?: string; actions?: Action[]; noCreate?: boolean;
  parent?: { param: string; label: string; prefix: string };
  detailPath?: (id: string) => string;
}

const level = [{value:'Beginner',label:'Beginner'},{value:'Intermediate',label:'Intermediate'},{value:'Advanced',label:'Advanced'},{value:'Professional',label:'Professional'}];

export const resources: Resource[] = [
  // ACADEMICS
  { key:'academic-years', label:'Academic years', path:'/api/academic-years', fields:[
    {name:'name',label:'Year (e.g. 2026-27)',required:true},{name:'startDate',label:'Start',type:'date',required:true},{name:'endDate',label:'End',type:'date',required:true}],
    columns:['id','name','startDate','endDate','isActive'] },
  { key:'courses', label:'Courses', path:'/api/courses', fields:[
    {name:'code',label:'Code',required:true},{name:'name',label:'Name',required:true},{name:'description',label:'Description',type:'textarea'},
    {name:'level',label:'Level',type:'select',options:level,default:'Beginner'},{name:'durationMonths',label:'Duration (months)',type:'number',default:6}],
    columns:['id','code','name','level','durationMonths'] },
  { key:'batches', label:'Batches', path:'/api/batches', fields:[
    {name:'courseId',label:'Course ID',required:true},{name:'academicYearId',label:'Academic Year ID',required:true},{name:'termId',label:'Term ID'},
    {name:'name',label:'Name',required:true},{name:'code',label:'Code',required:true},
    {name:'startDate',label:'Start date',type:'date'},{name:'endDate',label:'End date',type:'date'},{name:'capacity',label:'Capacity',type:'number',default:30}],
    columns:['id','code','name','status','startDate','endDate','capacity'],
    actions:[{label:'Enrol student',path:id=>`/api/batches/${id}/enrollments`,method:'POST',body:{studentId:''}},
      {label:'Assign teacher',path:id=>`/api/batches/${id}/teachers`,method:'POST',body:{teacherId:'',isLead:false}},
      {label:'Remove teacher',path:(id,v)=>`/api/batches/${id}/teachers/${v?.teacherId ?? ''}`,method:'DELETE',body:{teacherId:''}}] },
  { key:'enrollments', label:'Enrolments', path:'/api/batches', noCreate:true, columns:['id','code','name','status'],
    detailPath:id=>`/api/batches/${id}/enrollments` },

  // SCHEDULING
  { key:'schedule-rules', label:'Schedule rules', path:'/api/schedule-rules', fields:[
    {name:'batchId',label:'Batch ID',required:true},{name:'dayOfWeek',label:'Day of week (0=Sun)',type:'number',default:1},
    {name:'startTime',label:'Start time',placeholder:'18:00:00'},{name:'endTime',label:'End time',placeholder:'20:00:00'},
    {name:'validFrom',label:'Valid from',type:'date'},{name:'validTo',label:'Valid to',type:'date'},
    {name:'teacherId',label:'Teacher ID'},{name:'roomId',label:'Room ID'},{name:'recurrenceRule',label:'Recurrence (RRULE)'}],
    columns:['id','batchId','dayOfWeek','startTime','endTime','isActive'],
    actions:[{label:'Generate sessions',path:id=>`/api/schedule-rules/${id}/sessions`,method:'POST',body:{from:'2026-01-01',to:'2026-12-31'}}] },
  { key:'rooms', label:'Rooms', path:'/api/rooms', fields:[
    {name:'name',label:'Name',required:true},{name:'code',label:'Code'},{name:'type',label:'Type',type:'select',options:[{value:'Classroom',label:'Classroom'},{value:'Lab',label:'Lab'},{value:'Hall',label:'Hall'}],default:'Classroom'},
    {name:'capacity',label:'Capacity',type:'number',default:30},{name:'building',label:'Building'},{name:'floor',label:'Floor'}],
    columns:['id','name','code','type','capacity','building'],
    actions:[{label:'Check availability',path:id=>`/api/rooms/${id}/availability`,method:'POST',body:{startUtc:'',endUtc:''}}] },
  { key:'leave-requests', label:'Leave requests', path:'/api/scheduling/leave-requests', fields:[
    {name:'studentId',label:'Student ID',required:true},{name:'startDate',label:'From',type:'date'},{name:'endDate',label:'To',type:'date'},{name:'reason',label:'Reason',type:'textarea',required:true}],
    columns:['id','studentId','status','startDate','endDate'],
    actions:[{label:'Approve',path:id=>`/api/scheduling/leave-requests/${id}/review?approve=true`,method:'POST'},{label:'Reject',path:id=>`/api/scheduling/leave-requests/${id}/review?approve=false`,method:'POST'}] },

  // ASSESSMENT
  { key:'questions', label:'Question bank', path:'/api/questions', fields:[
    {name:'subjectId',label:'Subject ID',required:true},{name:'topicId',label:'Topic ID'},
    {name:'type',label:'Type',type:'select',options:[{value:'SingleChoice',label:'Single choice'},{value:'MultipleChoice',label:'Multiple choice'},{value:'TrueFalse',label:'True / false'},{value:'ShortAnswer',label:'Short answer'},{value:'Essay',label:'Essay'},{value:'Numeric',label:'Numeric'},{value:'Coding',label:'Coding'}],default:'SingleChoice'},
    {name:'difficulty',label:'Difficulty',type:'select',options:[{value:'Easy',label:'Easy'},{value:'Medium',label:'Medium'},{value:'Hard',label:'Hard'}],default:'Medium'},
    {name:'text',label:'Question',type:'textarea',required:true},{name:'optionsJson',label:'Options JSON',type:'textarea',placeholder:'[{"id":"a","text":"A"}]'},
    {name:'correctAnswerJson',label:'Correct answer JSON',type:'textarea',placeholder:'{"value":"a"}'},{name:'explanation',label:'Explanation',type:'textarea'},
    {name:'marks',label:'Marks',type:'number',default:1},{name:'negativeMarks',label:'Negative marks',type:'number',default:0},{name:'tagsJson',label:'Tags JSON',placeholder:'["algebra"]'}],
    columns:['id','text','type','difficulty','marks'],
    actions:[{label:'Edit',path:id=>`/api/questions/${id}`,method:'PUT',body:{text:'',optionsJson:'',correctAnswerJson:'',explanation:'',marks:1},jsonFields:['optionsJson','correctAnswerJson']},
             {label:'Delete',path:id=>`/api/questions/${id}`,method:'DELETE'}] },
  { key:'assessments', label:'Assessments', path:'/api/assessments', fields:[
    {name:'subjectId',label:'Subject ID',required:true},{name:'batchId',label:'Batch ID'},
    {name:'type',label:'Type',type:'select',options:[{value:'Quiz',label:'Quiz'},{value:'Assignment',label:'Assignment'},{value:'Midterm',label:'Midterm'},{value:'Final',label:'Final'}],default:'Quiz'},
    {name:'title',label:'Title',required:true},{name:'totalMarks',label:'Total marks',type:'number',default:10},
    {name:'startUtc',label:'Starts at',type:'datetime'},{name:'endUtc',label:'Ends at',type:'datetime'}],
    columns:['id','title','type','totalMarks','status'],
    actions:[{label:'Publish',path:id=>`/api/assessments/${id}/publish`,method:'POST'},
      {label:'Add question',path:id=>`/api/assessments/${id}/questions`,method:'POST',body:{questionId:'',marks:1,order:1}},
      {label:'Submit attempt',path:id=>`/api/assessments/${id}/attempts`,method:'POST',body:{studentId:'',answers:'[]'},jsonFields:['answers']}] },

  // FINANCE
  { key:'fee-structures', label:'Fee structures', path:'/api/fee-structures', fields:[
    {name:'name',label:'Name',required:true},{name:'code',label:'Code'},
    {name:'amount',label:'Amount',type:'number',required:true},{name:'currency',label:'Currency',default:'BDT'},
    {name:'frequency',label:'Frequency',type:'select',options:[{value:'OneTime',label:'One time'},{value:'Monthly',label:'Monthly'},{value:'Quarterly',label:'Quarterly'},{value:'Termly',label:'Termly'},{value:'Yearly',label:'Yearly'}],default:'Monthly'},
    {name:'taxPercent',label:'Tax %',type:'number',default:0},{name:'effectiveFrom',label:'Effective from',type:'date'}],
    columns:['id','name','code','amount','currency','frequency'],
    actions:[{label:'Generate installments',path:id=>`/api/fee-structures/${id}/installments`,method:'POST',body:{studentId:'',count:3,firstDueDate:'2026-01-01'}}] },
  { key:'invoices', label:'Invoices', path:'/api/invoices', fields:[
    {name:'studentId',label:'Student ID',required:true},{name:'enrollmentId',label:'Enrolment ID'},
    {name:'lines',label:'Lines JSON',type:'textarea',placeholder:'[{"description":"Tuition","quantity":1,"unitPrice":1000,"currency":"BDT"}]',default:'[]'},
    {name:'discount',label:'Discount',type:'number',default:0},{name:'taxPercent',label:'Tax %',type:'number',default:0},{name:'discountCode',label:'Discount code'}],
    columns:['id','invoiceNumber','studentId','total','amountPaid','status','dueDate'],
    actions:[{label:'Record payment',path:id=>`/api/invoices/${id}/payments`,method:'POST',body:{amount:0,currency:'BDT',method:'Cash'}},
      {label:'Download PDF',path:id=>`/api/invoices/${id}/pdf`,kind:'download'}] },
  { key:'discounts', label:'Discounts', path:'/api/discounts', fields:[
    {name:'code',label:'Code',required:true},{name:'name',label:'Name',required:true},
    {name:'type',label:'Type',type:'select',options:[{value:'Percentage',label:'Percentage'},{value:'FixedAmount',label:'Fixed'}],default:'Percentage'},
    {name:'value',label:'Value',type:'number',required:true},{name:'validFrom',label:'Valid from',type:'date'}],
    columns:['id','code','name','type','value','usageCount'] },
  { key:'scholarships', label:'Scholarships', path:'/api/scholarships', fields:[
    {name:'studentId',label:'Student ID',required:true},{name:'name',label:'Name',required:true},
    {name:'type',label:'Type',type:'select',options:[{value:'Percentage',label:'Percentage'},{value:'FixedAmount',label:'Fixed'}],default:'Percentage'},
    {name:'value',label:'Value',type:'number'},{name:'startDate',label:'Start',type:'date'}],
    columns:['id','studentId','name','type','value','isActive'] },

  // CRM
  { key:'leads', label:'Leads', path:'/api/crm/leads', fields:[
    {name:'firstName',label:'First name',required:true},{name:'lastName',label:'Last name',required:true},
    {name:'email',label:'Email'},{name:'phoneNumber',label:'Phone'},
    {name:'source',label:'Source',type:'select',options:[{value:'Website',label:'Website'},{value:'Referral',label:'Referral'},{value:'WalkIn',label:'Walk-in'},{value:'SocialMedia',label:'Social'}],default:'Website'},
    {name:'courseId',label:'Course ID'},{name:'notes',label:'Notes',type:'textarea'}],
    columns:['id','name','email','status','createdAt'],
    actions:[{label:'Mark qualified',path:id=>`/api/crm/leads/${id}/status`,method:'POST',body:{status:'Qualified'}}] },
  { key:'applications', label:'Applications', path:'/api/crm/applications', fields:[
    {name:'courseId',label:'Course ID',required:true},{name:'leadId',label:'Lead ID'},
    {name:'firstName',label:'First name',required:true},{name:'lastName',label:'Last name',required:true},
    {name:'email',label:'Email'},{name:'guardianName',label:'Guardian'}],
    columns:['id','name','courseId','status','appliedAt'],
    actions:[{label:'Accept',path:id=>`/api/crm/applications/${id}/review`,method:'POST',body:{status:'Accepted'}},
             {label:'Enrol',path:id=>`/api/crm/applications/${id}/enroll`,method:'POST',body:{batchId:'',password:''}}] },
  { key:'follow-ups', label:'Follow-ups', path:'/api/crm/follow-ups', fields:[
    {name:'leadId',label:'Lead ID'},{name:'applicationId',label:'Application ID'},{name:'note',label:'Note',type:'textarea',required:true},{name:'scheduledAt',label:'Scheduled at',type:'datetime'}],
    columns:['id','leadId','note','scheduledAt','status'],
    actions:[{label:'Complete',path:id=>`/api/crm/follow-ups/${id}/complete`,method:'POST'}] },

  // USERS & COMMUNICATION
  { key:'users', label:'Users', path:'/api/users', createPath:'/api/users/invite', fields:[
    {name:'email',label:'Email',required:true},{name:'firstName',label:'First name',required:true},{name:'lastName',label:'Last name',required:true},
    {name:'phoneNumber',label:'Phone'},{name:'password',label:'Temporary password',type:'password'},
    {name:'roles',label:'Roles (comma separated)',placeholder:'Teacher'}],
    columns:['id','email','fullName','status','roles'],
    actions:[{label:'Assign role',path:id=>`/api/users/${id}/roles`,method:'POST',body:{role:'Teacher'}},
      {label:'Remove role',path:(id,v)=>`/api/users/${id}/roles/${v?.role ?? ''}`,method:'DELETE',body:{role:''}},
      {label:'Set status',path:id=>`/api/users/${id}/status`,method:'POST',body:{status:'Active'}}] },
  { key:'announcements', label:'Announcements', path:'/api/communication/announcements', fields:[
    {name:'title',label:'Title',required:true},{name:'body',label:'Body',type:'textarea',required:true},
    {name:'targetAudienceJson',label:'Audience JSON',placeholder:'{"roles":["Student"]}',default:'{}'},
    {name:'priority',label:'Priority',type:'select',options:[{value:'0',label:'Low'},{value:'1',label:'Normal'},{value:'2',label:'High'},{value:'3',label:'Critical'}],default:'1'},
    {name:'isPinned',label:'Pinned',type:'select',options:[{value:'true',label:'Yes'},{value:'false',label:'No'}],default:'false'}],
    columns:['id','title','priority','isPinned','publishedAt'] },

  // MATERIALS & MESSAGING
  { key:'materials', label:'Course materials', path:'/api/assets/materials', fields:[
    {name:'batchId',label:'Batch ID'},{name:'topicId',label:'Topic ID'},{name:'title',label:'Title',required:true},
    {name:'type',label:'Type',type:'select',options:[{value:'Document',label:'Document'},{value:'Video',label:'Video'},{value:'Link',label:'Link'},{value:'Assignment',label:'Assignment'}],default:'Document'},
    {name:'assetId',label:'Uploaded asset ID'},{name:'externalUrl',label:'External URL'},{name:'durationMinutes',label:'Duration (min)',type:'number'},{name:'order',label:'Order',type:'number',default:0}],
    columns:['id','title','type','storageKey','externalUrl','isPublished'],
    actions:[{label:'Publish',path:id=>`/api/assets/materials/${id}/publish`,method:'POST',body:{publish:true}},
             {label:'Unpublish',path:id=>`/api/assets/materials/${id}/publish`,method:'POST',body:{publish:false}}] },
  { key:'conversations', label:'Conversations', path:'/api/communication/conversations', noCreate:true,
    columns:['id','subject','lastMessageAt','unreadCount'] },
];

export const resourceByKey = (key: string) => resources.find(r => r.key === key);
