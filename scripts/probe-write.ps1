$base = "http://localhost:7048/BC260/api/v2.0/companies(2112d28e-f0dc-f111-9b9b-5c879cb5ec66)"
$api  = "http://localhost:7048/BC260/api/techworks/universityPortal/v1.0/companies(2112d28e-f0dc-f111-9b9b-5c879cb5ec66)"

function Bc($method, $url, $body) {
    $p = @{ Uri = $url; Method = $method; ContentType = "application/json"; TimeoutSec = 120
            SkipHttpErrorCheck = $true; StatusCodeVariable = "sc"
            UseDefaultCredentials = $true; AllowUnencryptedAuthentication = $true }
    if ($body) { $p.Body = ($body | ConvertTo-Json -Depth 6) }
    if ($method -eq "Delete") { $p.Headers = @{ "If-Match" = "*" } }
    $r = Invoke-RestMethod @p
    Write-Host "HTTP $sc"
    $r
}

Write-Host "=== R. Where is the invoice now? ==="
$f = [uri]::EscapeDataString("externalDocumentNumber eq 'PROBE-DEEP-OK'")
$found = @((Bc Get "$base/salesInvoices?`$filter=$f").value)
$found | Select-Object id, number, status, externalDocumentNumber, invoiceDate, postingDate, dueDate, totalAmountIncludingTax, remainingAmount

if ($found.Count -eq 0) {
    Write-Host "Not found by key. Showing today's invoices for customer 20000 with total 220:"
    $g = [uri]::EscapeDataString("customerNumber eq '20000' and totalAmountIncludingTax eq 220")
    (Bc Get "$base/salesInvoices?`$filter=$g").value |
        Select-Object id, number, status, externalDocumentNumber, invoiceDate, totalAmountIncludingTax
    return
}
$inv = $found[0]

if ($inv.status -eq "Draft") {
    Write-Host "=== C. Post it (PERMANENT) ==="
    Bc Post "$base/salesInvoices($($inv.id))/Microsoft.NAV.post" $null | ConvertTo-Json -Depth 4
} else {
    Write-Host "Already posted (status $($inv.status)). Skipping C."
}

Write-Host "=== D. Does the original id still resolve? ==="
Bc Get "$base/salesInvoices($($inv.id))" | Select-Object id, number, status, remainingAmount

Write-Host "=== E. Find it again by our key ==="
$after = @((Bc Get "$base/salesInvoices?`$filter=$f").value)
$after | Select-Object id, number, status, externalDocumentNumber, invoiceDate, postingDate, dueDate, totalAmountIncludingTax, remainingAmount

Write-Host "=== F. The ledger entry for the posted number ==="
if ($after.Count -gt 0) {
    $lf = [uri]::EscapeDataString("documentNumber eq '$($after[0].number)'")
    (Bc Get "$api/customerLedgerEntries?`$filter=$lf").value |
        Select-Object entryNumber, documentType, documentNumber, amount, remainingAmount, open
}

Write-Host "=== G. Post the same invoice a second time ==="
Bc Post "$base/salesInvoices($($inv.id))/Microsoft.NAV.post" $null | ConvertTo-Json -Depth 4

Write-Host "=== Cleanup: remove the draft if posting failed ==="
$state = Bc Get "$base/salesInvoices($($inv.id))"
if ($state.status -eq "Draft") { Bc Delete "$base/salesInvoices($($inv.id))" }