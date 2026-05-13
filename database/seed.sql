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

INSERT INTO payment_receipts
    (
        receipt_number,
        invoice_id,
        payment_date,
        amount_paid,
        payment_method,
        reference_number,
        notes,
        created_at_utc,
        updated_at_utc
    )
VALUES
    ('PR-1001', 1, current_date - 13, 500.00, 'Bank Transfer', 'REF-1001', 'Partial payment received.', now() at time zone 'utc', now() at time zone 'utc'),
    ('PR-1002', 1, current_date - 10, 875.00, 'Cash', 'REF-1002', 'Remaining balance paid.', now() at time zone 'utc', now() at time zone 'utc'),
    ('PR-1003', 2, current_date - 28, 902.00, 'Credit Card', 'REF-1003', 'Full payment settled.', now() at time zone 'utc', now() at time zone 'utc'),
    ('PR-1004', 1, current_date - 9, 250.00, 'Debit Card', 'REF-1004', 'Additional service payment.', now() at time zone 'utc', now() at time zone 'utc'),
    ('PR-1005', 2, current_date - 25, 300.00, 'Check', 'REF-1005', 'Advance payment.', now() at time zone 'utc', now() at time zone 'utc'),
    ('PR-1006', 1, current_date - 8, 125.00, 'Cash', 'REF-1006', 'Late fee payment.', now() at time zone 'utc', now() at time zone 'utc'),
    ('PR-1007', 2, current_date - 22, 150.00, 'Bank Transfer', 'REF-1007', 'Support extension.', now() at time zone 'utc', now() at time zone 'utc'),
    ('PR-1008', 1, current_date - 7, 200.00, 'Credit Card', 'REF-1008', 'Consulting add-on.', now() at time zone 'utc', now() at time zone 'utc'),
    ('PR-1009', 2, current_date - 20, 100.00, 'Debit Card', 'REF-1009', 'Adjustment payment.', now() at time zone 'utc', now() at time zone 'utc'),
    ('PR-1010', 1, current_date - 6, 75.00, 'Cash', 'REF-1010', 'Miscellaneous fee.', now() at time zone 'utc', now() at time zone 'utc'),
    ('PR-1011', 2, current_date - 18, 50.00, 'Check', 'REF-1011', 'Small balance payment.', now() at time zone 'utc', now() at time zone 'utc'),
    ('PR-1012', 1, current_date - 5, 320.00, 'Bank Transfer', 'REF-1012', 'Additional project payment.', now() at time zone 'utc', now() at time zone 'utc'),
    ('PR-1013', 2, current_date - 16, 225.00, 'Credit Card', 'REF-1013', 'Hardware reimbursement.', now() at time zone 'utc', now() at time zone 'utc'),
    ('PR-1014', 1, current_date - 4, 180.00, 'Debit Card', 'REF-1014', 'Training session payment.', now() at time zone 'utc', now() at time zone 'utc'),
    ('PR-1015', 2, current_date - 15, 400.00, 'Cash', 'REF-1015', 'Quarterly adjustment.', now() at time zone 'utc', now() at time zone 'utc'),
    ('PR-1016', 1, current_date - 3, 90.00, 'Check', 'REF-1016', 'Documentation fee.', now() at time zone 'utc', now() at time zone 'utc'),
    ('PR-1017', 2, current_date - 12, 210.00, 'Bank Transfer', 'REF-1017', 'Operational support.', now() at time zone 'utc', now() at time zone 'utc'),
    ('PR-1018', 1, current_date - 2, 140.00, 'Credit Card', 'REF-1018', 'Technical assistance.', now() at time zone 'utc', now() at time zone 'utc'),
    ('PR-1019', 2, current_date - 11, 350.00, 'Debit Card', 'REF-1019', 'Additional maintenance.', now() at time zone 'utc', now() at time zone 'utc'),
    ('PR-1020', 1, current_date - 1, 260.00, 'Cash', 'REF-1020', 'Expedited service payment.', now() at time zone 'utc', now() at time zone 'utc'),
    ('PR-1021', 2, current_date - 9, 120.00, 'Check', 'REF-1021', 'Small support charge.', now() at time zone 'utc', now() at time zone 'utc'),
    ('PR-1022', 1, current_date - 8, 610.00, 'Bank Transfer', 'REF-1022', 'Bulk consulting fee.', now() at time zone 'utc', now() at time zone 'utc'),
    ('PR-1023', 2, current_date - 7, 80.00, 'Credit Card', 'REF-1023', 'License renewal.', now() at time zone 'utc', now() at time zone 'utc'),
    ('PR-1024', 1, current_date - 6, 95.00, 'Debit Card', 'REF-1024', 'Administrative fee.', now() at time zone 'utc', now() at time zone 'utc'),
    ('PR-1025', 2, current_date - 5, 175.00, 'Cash', 'REF-1025', 'Final adjustment payment.', now() at time zone 'utc', now() at time zone 'utc')
ON CONFLICT (receipt_number) DO NOTHING;
