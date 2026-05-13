### Summary
- Added the Payment Receipt details page for viewing a single receipt.
- The page shows the full receipt information with linked invoice number and customer context.

### What changed
- Added the `/PaymentReceipts/Details/{id}` details flow.
- Added repository loading for a single payment receipt by id.
- Joined `payment_receipts` with `invoices` so the details page can show invoice number and customer name.
- Added the payment receipt details view model.
- Added the Razor details view for `/PaymentReceipts/Details/{id}`.
- Updated the details page layout to present receipt information, invoice context, and system timestamps clearly.
- Added tests for details behavior.

### Why it is safe
- SQL uses Dapper parameters for the receipt id.
- Details excludes deleted receipts using `pr.deleted_at_utc IS NULL`.
- Missing or deleted receipts return not found.
- Views use view models instead of binding directly to database models.
- The details page is read-only and does not introduce overposting risk.

### Tests
- Ran `dotnet test --no-restore`.
- Result: all tests passed.

### Risks / limitations
- Create, edit, and soft delete are intentionally not included in this branch because they belong to separate assigned receipt tasks.
- Receipt details depends on the agreed invoice foundation data because it shows linked invoice/customer context.
