INSERT INTO documents
    (title, document_number, department, owner_name, status, description, created_at_utc, updated_at_utc)
VALUES
    ('Expense Policy', 'FIN-001', 'Finance', 'Avery Santos', 'Approved', 'Policy for employee expense reimbursement.', now() at time zone 'utc', now() at time zone 'utc'),
    ('Onboarding Checklist', 'HR-014', 'Human Resources', 'Jordan Lee', 'Review', 'Checklist used for new employee onboarding.', now() at time zone 'utc', now() at time zone 'utc'),
    ('API Support Runbook', 'ENG-022', 'Engineering', 'Mika Reyes', 'Draft', 'Support steps for common API incidents.', now() at time zone 'utc', now() at time zone 'utc'),
    ('Records Retention Guide', 'OPS-008', 'Operations', 'Sam Chen', 'Archived', 'Archived retention policy reference.', now() at time zone 'utc', now() at time zone 'utc')
ON CONFLICT (document_number) DO NOTHING;

INSERT INTO invoices
    (invoice_number, customer_name, invoice_date, due_date, status, subtotal, tax_amount, total_amount, notes, created_at_utc, updated_at_utc)
VALUES
    ('INV-1001', 'Northwind Traders', current_date - 14, current_date + 16, 'Pending', 1250.00, 125.00, 1375.00, 'Initial consulting invoice.', now() at time zone 'utc', now() at time zone 'utc'),
    ('INV-1002', 'Contoso Ltd.', current_date - 30, current_date, 'Paid', 820.00, 82.00, 902.00, 'Monthly support retainer.', now() at time zone 'utc', now() at time zone 'utc'),
    ('INV-1003', 'Adventure Works', current_date - 7, current_date + 23, 'Draft', 460.00, 46.00, 506.00, 'Draft invoice awaiting review.', now() at time zone 'utc', now() at time zone 'utc')
ON CONFLICT (invoice_number) DO NOTHING;
