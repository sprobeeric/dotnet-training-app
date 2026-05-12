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

INSERT INTO payment_receipts (
    receipt_number,
    payment_date_utc,
    reference_number,
    total_amount,
    received,
    change_amount,
    created_at_utc,
    updated_at_utc
)
SELECT
    format(
        'PR-%s-%s',
        to_char(d::date, 'YYYYMMDD'),
        lpad(gs::text, 6, '0')
    ) AS receipt_number,
    d + ((gs % 12) || ' hours')::interval
      + ((gs % 60) || ' minutes')::interval AS payment_date_utc,
    format(
        'REF-%s-%s-%s',
        to_char(d::date, 'YYYYMMDD'),
        lpad(gs::text, 6, '0'),
        upper(substr(md5(gs::text), 1, 6))
    ) AS reference_number,
    totals.total_amount,
    totals.received,
    totals.received - totals.total_amount AS change_amount,
    now() at time zone 'utc',
    now() at time zone 'utc'
FROM generate_series(1, 25) AS gs
CROSS JOIN LATERAL (
    SELECT
        ('2026-05-12'::date + ((gs - 1) / 5))::timestamp
) dates(d)
CROSS JOIN LATERAL (
    SELECT
        round((200 + (gs * 17 % 500))::numeric, 2) AS total_amount
) totals_base
CROSS JOIN LATERAL (
    SELECT
        totals_base.total_amount,
        ceil(totals_base.total_amount / 100.0) * 100 AS received
) totals
ON CONFLICT (receipt_number) DO NOTHING;

INSERT INTO payment_receipt_products (
    payment_receipt_id,
    product_id,
    quantity,
    unit_price,
    line_total
)
SELECT
    pr.id,
    p.id,
    qty.quantity,
    p.unit_price,
    qty.quantity * p.unit_price
FROM payment_receipts pr
CROSS JOIN LATERAL (
    SELECT ((right(pr.receipt_number, 6)::int - 1) % 20 + 1) AS id
) selected_product
JOIN products p
    ON p.id = selected_product.id
CROSS JOIN LATERAL (
    SELECT ((right(pr.receipt_number, 6)::int - 1) % 3 + 1) AS quantity
) qty
WHERE pr.receipt_number >= 'PR-20260512-000001'
  AND pr.receipt_number <= 'PR-20260516-000025'
ON CONFLICT DO NOTHING;