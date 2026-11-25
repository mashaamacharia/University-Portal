using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using UniversityPortal.API.Integrations.BusinessCentral.DTOs;

namespace UniversityPortal.API.Tests;

public class ReceiptTests : IClassFixture<PortalApiFactory>
{
    private readonly PortalApiFactory _factory;

    public ReceiptTests(PortalApiFactory factory)
    {
        _factory = factory;
        _factory.Bc.ResetPaymentState();
    }

    [Fact]
    public async Task Student_receipt_uses_ledger_and_portal_payment_details()
    {
        using var officer = await _factory.ClientForRoleAsync("FinanceOfficer");
        using var payment = await RecordPaymentAsync(officer);
        var receiptNumber = payment.RootElement.GetProperty("receiptNumber").GetString()
            ?? throw new InvalidOperationException("The payment response has no receipt number.");
        using var student = await _factory.ClientForRoleAsync("Student");

        using var response = await student.GetAsync(
            $"/api/student/finance/receipts/{Uri.EscapeDataString(receiptNumber)}");
        using var body = await ReadBodyAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(receiptNumber, body.RootElement.GetProperty("receiptNumber").GetString());
        Assert.Equal("Student A", body.RootElement.GetProperty("student").GetProperty("name").GetString());
        Assert.Equal(125m, body.RootElement.GetProperty("amountReceived").GetDecimal());
        Assert.Equal("Cash", body.RootElement.GetProperty("method").GetString());
        Assert.Equal("TEST-RECEIPT", body.RootElement.GetProperty("reference").GetString());
        Assert.Equal("Chuka University", body.RootElement.GetProperty("issuer").GetProperty("name").GetString());
        Assert.Equal(1000m, body.RootElement.GetProperty("currentAccountBalance").GetDecimal());
        Assert.Equal(875m, body.RootElement.GetProperty("invoiceBalances")[0]
            .GetProperty("remainingBalance").GetDecimal());
        Assert.Equal(0, body.RootElement.GetProperty("printCount").GetInt32());
        Assert.Equal(JsonValueKind.Null, body.RootElement.GetProperty("copyNumber").ValueKind);
    }

    [Fact]
    public async Task Printing_receipt_records_original_then_duplicate_copy_numbers()
    {
        using var officer = await _factory.ClientForRoleAsync("FinanceOfficer");
        using var payment = await RecordPaymentAsync(officer);
        var receiptNumber = payment.RootElement.GetProperty("receiptNumber").GetString()
            ?? throw new InvalidOperationException("The payment response has no receipt number.");
        using var student = await _factory.ClientForRoleAsync("Student");

        using var originalResponse = await student.PostAsync(
            $"/api/student/finance/receipts/{Uri.EscapeDataString(receiptNumber)}/print",
            content: null);
        using var original = await ReadBodyAsync(originalResponse);
        using var duplicateResponse = await student.PostAsync(
            $"/api/student/finance/receipts/{Uri.EscapeDataString(receiptNumber)}/print",
            content: null);
        using var duplicate = await ReadBodyAsync(duplicateResponse);

        Assert.Equal(HttpStatusCode.OK, originalResponse.StatusCode);
        Assert.Equal(0, original.RootElement.GetProperty("copyNumber").GetInt32());
        Assert.Equal(1, original.RootElement.GetProperty("printCount").GetInt32());
        Assert.Equal(HttpStatusCode.OK, duplicateResponse.StatusCode);
        Assert.Equal(1, duplicate.RootElement.GetProperty("copyNumber").GetInt32());
        Assert.Equal(2, duplicate.RootElement.GetProperty("printCount").GetInt32());
    }

    [Fact]
    public async Task Reversed_receipt_can_be_viewed_but_not_printed()
    {
        using var officer = await _factory.ClientForRoleAsync("FinanceOfficer");
        using var payment = await RecordPaymentAsync(officer);
        var receiptNumber = payment.RootElement.GetProperty("receiptNumber").GetString()
            ?? throw new InvalidOperationException("The payment response has no receipt number.");
        _factory.Bc.MarkPaymentReversed(receiptNumber);
        using var student = await _factory.ClientForRoleAsync("Student");

        using var get = await student.GetAsync(
            $"/api/student/finance/receipts/{Uri.EscapeDataString(receiptNumber)}");
        using var body = await ReadBodyAsync(get);
        using var print = await student.PostAsync(
            $"/api/student/finance/receipts/{Uri.EscapeDataString(receiptNumber)}/print",
            content: null);

        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        Assert.Equal("Reversed", body.RootElement.GetProperty("status").GetString());
        Assert.Equal(HttpStatusCode.Conflict, print.StatusCode);
    }

    [Fact]
    public async Task Officer_receipt_lookup_requires_student_number_and_correct_role()
    {
        using var officer = await _factory.ClientForRoleAsync("FinanceOfficer");
        using var payment = await RecordPaymentAsync(officer);
        var receiptNumber = payment.RootElement.GetProperty("receiptNumber").GetString()
            ?? throw new InvalidOperationException("The payment response has no receipt number.");

        using var valid = await officer.GetAsync(
            $"/api/officer/finance/receipts/{Uri.EscapeDataString(receiptNumber)}?studentNumber={FakeBusinessCentralClient.StudentA}");
        using var missingStudentNumber = await officer.GetAsync(
            $"/api/officer/finance/receipts/{Uri.EscapeDataString(receiptNumber)}");
        using var student = await _factory.ClientForRoleAsync("Student");
        using var forbidden = await student.GetAsync(
            $"/api/officer/finance/receipts/{Uri.EscapeDataString(receiptNumber)}?studentNumber={FakeBusinessCentralClient.StudentA}");

        Assert.Equal(HttpStatusCode.OK, valid.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, missingStudentNumber.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);
    }

    [Fact]
    public async Task A_student_cannot_read_another_students_receipt()
    {
        using var officer = await _factory.ClientForRoleAsync("FinanceOfficer");
        using var payment = await RecordPaymentAsync(officer);
        var receiptNumber = payment.RootElement.GetProperty("receiptNumber").GetString()
            ?? throw new InvalidOperationException("The payment response has no receipt number.");
        var studentB = await _factory.CreateAccountAsync("Student", FakeBusinessCentralClient.StudentB);
        using var otherStudent = await _factory.LoginAsync(studentB.Email, studentB.Password);

        using var response = await otherStudent.GetAsync(
            $"/api/student/finance/receipts/{Uri.EscapeDataString(receiptNumber)}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Direct_business_central_receipt_infers_invoices_closed_by_the_payment()
    {
        const string receiptNumber = "BC-RECEIPT-1";
        const int paymentEntryNumber = 91;
        var date = new DateOnly(2026, 11, 24);
        _factory.Bc.AddLedgerEntry(new BcCustomerLedgerEntryDto
        {
            EntryNumber = paymentEntryNumber,
            CustomerNumber = FakeBusinessCentralClient.StudentA,
            PostingDate = date,
            DocumentType = "Payment",
            DocumentNumber = receiptNumber,
            Amount = -250,
            CurrencyCode = "KES"
        });
        _factory.Bc.AddLedgerEntry(new BcCustomerLedgerEntryDto
        {
            EntryNumber = paymentEntryNumber + 1,
            CustomerNumber = FakeBusinessCentralClient.StudentA,
            PostingDate = date,
            DocumentType = "Invoice",
            DocumentNumber = FakeBusinessCentralClient.InvoiceOfA,
            Amount = 1000,
            ClosedByEntryNumber = paymentEntryNumber,
            CurrencyCode = "KES"
        });
        using var student = await _factory.ClientForRoleAsync("Student");

        using var response = await student.GetAsync(
            $"/api/student/finance/receipts/{Uri.EscapeDataString(receiptNumber)}");
        using var body = await ReadBodyAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(250m, body.RootElement.GetProperty("amountReceived").GetDecimal());
        Assert.Equal(JsonValueKind.Null, body.RootElement.GetProperty("method").ValueKind);
        Assert.Equal(
            FakeBusinessCentralClient.InvoiceOfA,
            body.RootElement.GetProperty("settledInvoices")[0].GetString());
    }

    private static async Task<JsonDocument> RecordPaymentAsync(HttpClient officer)
    {
        var request = new
        {
            requestId = Guid.NewGuid(),
            studentNumber = FakeBusinessCentralClient.StudentA,
            invoiceNumber = FakeBusinessCentralClient.InvoiceOfA,
            amount = 125,
            paymentDate = DateOnly.FromDateTime(DateTime.UtcNow),
            method = "Cash",
            reference = "TEST-RECEIPT"
        };
        using var response = await officer.PostAsJsonAsync("/api/officer/finance/payments", request);
        response.EnsureSuccessStatusCode();
        return await ReadBodyAsync(response);
    }

    private static async Task<JsonDocument> ReadBodyAsync(HttpResponseMessage response) =>
        await response.Content.ReadFromJsonAsync<JsonDocument>()
        ?? throw new InvalidOperationException("The receipt endpoint returned an empty response.");
}
