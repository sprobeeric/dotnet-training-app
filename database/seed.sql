INSERT INTO documents
    (title, document_number, department, owner_name, status, description, created_at_utc, updated_at_utc)
VALUES
    ('Expense Policy', 'FIN-001', 'Finance', 'Avery Santos', 'Approved', 'Policy for employee expense reimbursement.', now() at time zone 'utc', now() at time zone 'utc'),
    ('Onboarding Checklist', 'HR-014', 'Human Resources', 'Jordan Lee', 'Review', 'Checklist used for new employee onboarding.', now() at time zone 'utc', now() at time zone 'utc'),
    ('API Support Runbook', 'ENG-022', 'Engineering', 'Mika Reyes', 'Draft', 'Support steps for common API incidents.', now() at time zone 'utc', now() at time zone 'utc'),
    ('Records Retention Guide', 'OPS-008', 'Operations', 'Sam Chen', 'Archived', 'Archived retention policy reference.', now() at time zone 'utc', now() at time zone 'utc')
ON CONFLICT (document_number) DO NOTHING;

INSERT INTO products (
    name, unit_price, created_at_utc, updated_at_utc) 
VALUES
    ('Classic Pearl Milk Tea', 120.00, now() at time zone 'utc', now() at time zone 'utc'),
    ('Wintermelon Milk Tea', 110.00, now() at time zone 'utc', now() at time zone 'utc'),
    ('Okinawa Milk Tea', 130.00, now() at time zone 'utc', now() at time zone 'utc'),
    ('Hokkaido Milk Tea', 135.00, now() at time zone 'utc', now() at time zone 'utc'),
    ('Taro Milk Tea', 125.00, now() at time zone 'utc', now() at time zone 'utc'),
    ('Matcha Milk Tea', 140.00, now() at time zone 'utc', now() at time zone 'utc'),
    ('Brown Sugar Milk Tea', 145.00, now() at time zone 'utc', now() at time zone 'utc'),
    ('Thai Milk Tea', 130.00, now() at time zone 'utc', now() at time zone 'utc'),
    ('Chocolate Milk Tea', 120.00, now() at time zone 'utc', now() at time zone 'utc'),
    ('Coffee Jelly Milk Tea', 135.00, now() at time zone 'utc', now() at time zone 'utc'),
    ('Cheesecake Milk Tea', 150.00, now() at time zone 'utc', now() at time zone 'utc'),
    ('Strawberry Milk Tea', 140.00, now() at time zone 'utc', now() at time zone 'utc'),
    ('Mango Fruit Tea', 115.00, now() at time zone 'utc', now() at time zone 'utc'),
    ('Passion Fruit Tea', 115.00, now() at time zone 'utc', now() at time zone 'utc'),
    ('Lychee Fruit Tea', 120.00, now() at time zone 'utc', now() at time zone 'utc'),
    ('Red Velvet Milk Tea', 150.00, now() at time zone 'utc', now() at time zone 'utc'),
    ('Cookies and Cream Milk Tea', 145.00, now() at time zone 'utc', now() at time zone 'utc'),
    ('Salted Caramel Milk Tea', 145.00, now() at time zone 'utc', now() at time zone 'utc'),
    ('Yakult Green Tea', 125.00, now() at time zone 'utc', now() at time zone 'utc'),
    ('Black Tea Macchiato', 135.00, now() at time zone 'utc', now() at time zone 'utc')
ON CONFLICT (name) DO NOTHING;