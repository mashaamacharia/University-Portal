4. Test it

dotnet build and dotnet test first. For these tests I changed the Api helper so it prints the response and returns it, so you can reuse values like the invoice id. Replace your earlier helper with this one, and keep Login, NewKey, $officerTok and $body from the S6.4 tests:

powershell
function Api($method, $path, $tok, $body, $headers) {
    $p = @{ Uri = "$apiBase$path"; Method = $method; SkipHttpErrorCheck = $true
            StatusCodeVariable = "sc"; ContentType = "application/json" }
    if ($tok)     { $p.Authentication = "Bearer"; $p.Token = $tok }
    if ($body)    { $p.Body = ($body | ConvertTo-Json -Depth 6) }
    if ($headers) { $p.Headers = $headers }
    $r = Invoke-RestMethod @p
    Write-Host "status: $sc"
    if ($r) { $r | ConvertTo-Json -Depth 6 | Write-Host }
    $r
}

$api = "http://localhost:7048/BC260/api/techworks/universityPortal/v1.0/companies(2112d28e-f0dc-f111-9b9b-5c879cb5ec66)"
$h = @{ UseDefaultCredentials = $true; AllowUnencryptedAuthentication = $true }

A. Post a draft (this creates one more permanent 220 invoice for customer 20000 in UniversityDev):

powershell
$d = Api POST "/api/officer/finance/invoices" $officerTok $body @{ "Idempotency-Key" = (NewKey) }
$id = $d.bcInvoiceId
"Draft number: $($d.invoiceNumber)"

# Expect 200: status Open, a NEW invoice number, paymentStatus Unpaid, amountRemaining 220

$p = Api POST "/api/officer/finance/invoices/$id/post" $officerTok

# Post again: expect 200 and the same posted invoice, not an error

Api POST "/api/officer/finance/invoices/$id/post" $officerTok

B. Check BC agrees:

powershell

# The ledger entry for the posted number: amount 220, remaining 220, open

$lf = [uri]::EscapeDataString("documentNumber eq '$($p.invoiceNumber)'")
(Invoke-RestMethod "$api/customerLedgerEntries?`$filter=$lf" @h).value |
    Select-Object entryNumber, documentType, documentNumber, amount, remainingAmount, open

Then call the student’s statement as customer 20000 (GET /api/student/finance/statement) and confirm the new posted invoice appears, unpaid, with the balance up by 220. The officer dashboard’s totalOutstanding should rise by 220, and its draftInvoices count should be unchanged by the posted invoice.

C. Discard a draft:

powershell
$key2 = NewKey
$d2 = Api POST "/api/officer/finance/invoices" $officerTok $body @{ "Idempotency-Key" = $key2 }

Api DELETE "/api/officer/finance/invoices/$($d2.bcInvoiceId)" $officerTok     # 204
Api DELETE "/api/officer/finance/invoices/$($d2.bcInvoiceId)" $officerTok     # 204 again: already discarded
Api GET "/api/officer/finance/invoices/$($d2.bcInvoiceId)" $officerTok        # 404: it's gone from BC

# The key is free again: the same request creates a NEW draft (201), which we discard to tidy up

$d3 = Api POST "/api/officer/finance/invoices" $officerTok $body @{ "Idempotency-Key" = $key2 }
Api DELETE "/api/officer/finance/invoices/$($d3.bcInvoiceId)" $officerTok

D. Rules:

powershell

# A posted invoice cannot be discarded: 422 pointing to a credit memo

Api DELETE "/api/officer/finance/invoices/$id" $officerTok

# A draft the portal didn't create (take an id from GET /api/officer/finance/invoices?status=Draft): 422

Api POST "/api/officer/finance/invoices/<id of a demo draft></id>/post" $officerTok

# Made-up id: 404

Api POST "/api/officer/finance/invoices/$([guid]::NewGuid())/post" $officerTok

# Student or admin: 403. No token: 401.

$studentTok = Login "<student email></student>" "<student password></student>"
Api POST "/api/officer/finance/invoices/$id/post" $studentTok
Api POST "/api/officer/finance/invoices/$id/post" $null

E. The two-officer rule. In appsettings.Development.json set PostingRequiresDifferentOfficer to true and restart. Create a second officer if you don’t have one (POST /api/admin/users with role FinanceOfficer). Then:

powershell
$d4 = Api POST "/api/officer/finance/invoices" $officerTok $body @{ "Idempotency-Key" = (NewKey) }
Api POST "/api/officer/finance/invoices/$($d4.bcInvoiceId)/post" $officerTok     # 403: second officer required
$officer2Tok = Login "<second officer email></second>" "<second officer password></second>"
Api POST "/api/officer/finance/invoices/$($d4.bcInvoiceId)/post" $officer2Tok    # 200

Afterwards set it back to false if you want a one-officer dev setup. This posts another permanent invoice, which is fine in the sandbox.
