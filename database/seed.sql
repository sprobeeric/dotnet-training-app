INSERT INTO documents
    (title, document_number, department, owner_name, status, description, created_at_utc, updated_at_utc)
VALUES
    ('Expense Policy', 'FIN-001', 'Finance', 'Avery Santos', 'Approved', 'Policy for employee expense reimbursement.', now() at time zone 'utc', now() at time zone 'utc'),
    ('Onboarding Checklist', 'HR-014', 'Human Resources', 'Jordan Lee', 'Review', 'Checklist used for new employee onboarding.', now() at time zone 'utc', now() at time zone 'utc'),
    ('API Support Runbook', 'ENG-022', 'Engineering', 'Mika Reyes', 'Draft', 'Support steps for common API incidents.', now() at time zone 'utc', now() at time zone 'utc'),
    ('Records Retention Guide', 'OPS-008', 'Operations', 'Sam Chen', 'Archived', 'Archived retention policy reference.', now() at time zone 'utc', now() at time zone 'utc')
ON CONFLICT (document_number) DO NOTHING;
