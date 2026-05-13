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
    ('INV-1001', 'Northwind Traders', current_date - 14, current_date + 16, 'Sent', 1250.00, 125.00, 1375.00, 'Initial consulting invoice.', now() at time zone 'utc', now() at time zone 'utc'),
    ('INV-1002', 'Contoso Ltd.', current_date - 30, current_date, 'Paid', 820.00, 82.00, 902.00, 'Monthly support retainer.', now() at time zone 'utc', now() at time zone 'utc'),
    ('INV-1003', 'Adventure Works', current_date - 7, current_date + 23, 'Draft', 460.00, 46.00, 506.00, 'Draft invoice awaiting review.', now() at time zone 'utc', now() at time zone 'utc'),
    ('INV-1004', 'Litware Inc.', current_date - 18, current_date + 12, 'Sent', 2100.00, 210.00, 2310.00, 'Implementation milestone billing.', now() at time zone 'utc', now() at time zone 'utc'),
    ('INV-1005', 'Fabrikam Corp.', current_date - 45, current_date - 15, 'Paid', 975.00, 97.50, 1072.50, 'Quarterly maintenance services.', now() at time zone 'utc', now() at time zone 'utc'),
    ('INV-1006', 'Tailspin Toys', current_date - 3, current_date + 27, 'Draft', 680.00, 68.00, 748.00, 'Draft hardware procurement invoice.', now() at time zone 'utc', now() at time zone 'utc'),
    ('INV-1007', 'Wide World Importers', current_date - 22, current_date + 8, 'Sent', 1540.00, 154.00, 1694.00, 'Infrastructure assessment engagement.', now() at time zone 'utc', now() at time zone 'utc'),
    ('INV-1008', 'Blue Yonder Airlines', current_date - 60, current_date - 30, 'Paid', 3100.00, 310.00, 3410.00, 'Training workshop series.', now() at time zone 'utc', now() at time zone 'utc'),
    ('INV-1009', 'Alpine Ski House', current_date - 10, current_date + 20, 'Cancelled', 540.00, 54.00, 594.00, 'Cancelled duplicate invoice.', now() at time zone 'utc', now() at time zone 'utc'),
    ('INV-1010', 'Humongous Insurance', current_date - 12, current_date + 18, 'Sent', 1890.00, 189.00, 2079.00, 'Security review and recommendations.', now() at time zone 'utc', now() at time zone 'utc'),
    ('INV-1011', 'Graphic Design Institute', current_date - 5, current_date + 25, 'Draft', 430.00, 43.00, 473.00, 'Creative asset revision package.', now() at time zone 'utc', now() at time zone 'utc'),
    ('INV-1012', 'Woodgrove Bank', current_date - 35, current_date - 5, 'Paid', 2650.00, 265.00, 2915.00, 'Compliance documentation update.', now() at time zone 'utc', now() at time zone 'utc'),
    ('INV-1013', 'Proseware', current_date - 9, current_date + 21, 'Sent', 720.00, 72.00, 792.00, 'Application support subscription.', now() at time zone 'utc', now() at time zone 'utc'),
    ('INV-1014', 'Coho Vineyard', current_date - 2, current_date + 28, 'Draft', 1180.00, 118.00, 1298.00, 'Draft invoice for analytics setup.', now() at time zone 'utc', now() at time zone 'utc'),
    ('INV-1015', 'Lucerne Publishing', current_date - 25, current_date + 5, 'Sent', 860.00, 86.00, 946.00, 'Editorial workflow automation.', now() at time zone 'utc', now() at time zone 'utc'),
    ('INV-1016', 'Trey Research', current_date - 55, current_date - 25, 'Paid', 1430.00, 143.00, 1573.00, 'Research portal enhancements.', now() at time zone 'utc', now() at time zone 'utc'),
    ('INV-1017', 'A Datum Corporation', current_date - 16, current_date + 14, 'Cancelled', 990.00, 99.00, 1089.00, 'Project paused before approval.', now() at time zone 'utc', now() at time zone 'utc'),
    ('INV-1018', 'Consolidated Messenger', current_date - 20, current_date + 10, 'Sent', 1320.00, 132.00, 1452.00, 'Mobile app deployment support.', now() at time zone 'utc', now() at time zone 'utc'),
    ('INV-1019', 'Margies Travel', current_date - 6, current_date + 24, 'Draft', 575.00, 57.50, 632.50, 'Draft invoice for booking portal fixes.', now() at time zone 'utc', now() at time zone 'utc'),
    ('INV-1020', 'School of Fine Art', current_date - 40, current_date - 10, 'Paid', 1240.00, 124.00, 1364.00, 'Semester content migration services.', now() at time zone 'utc', now() at time zone 'utc')
ON CONFLICT (invoice_number) DO NOTHING;
