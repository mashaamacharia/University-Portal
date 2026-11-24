using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using UniversityPortal.API.Integrations.BusinessCentral.Clients;
using UniversityPortal.API.Integrations.BusinessCentral.Configuration;
using UniversityPortal.API.Integrations.BusinessCentral.DTOs;
using UniversityPortal.API.Modules.Finance.DTOs;

namespace UniversityPortal.API.Tests;

public class PaymentRecordingUnitTests
{
    [Fact]
    public async Task Business_central_payment_lines_use_a_negative_amount_and_omit_invoice_when_unapplied()
    {
        var handler = new PaymentRequestHandler(
            """
            {
              "id": "11111111-1111-1111-1111-111111111111",
              "lineNumber": 10000,
              "customerNumber": "T-1001",
              "postingDate": "2026-11-24",
              "externalDocumentNumber": "request-reference",
              "amount": -125,
              "description": "Cash payment"
            }
            """);
        var options = new BusinessCentralOptions
        {
            BaseUrl = "https://businesscentral.example.test/api/v2.0/",
            AuthMode = "Windows",
            CompanyId = Guid.NewGuid()
        };
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri(options.BaseUrl)
        };
        var client = new BusinessCentralClient(httpClient, Options.Create(options));

        var line = await client.CreatePaymentLineAsync(
            Guid.NewGuid(),
            new BcPaymentLineRequest(
                "T-1001",
                new DateOnly(2026, 11, 24),
                125,
                null,
                "request-reference",
                "Cash payment"));

        using var body = JsonDocument.Parse(handler.RequestBody!);
        Assert.Equal(HttpMethod.Post, handler.RequestMethod);
        Assert.Equal(-125m, body.RootElement.GetProperty("amount").GetDecimal());
        Assert.False(body.RootElement.TryGetProperty("appliesToInvoiceNumber", out _));
        Assert.Equal("request-reference", line.ExternalDocumentNumber);
    }

    [Fact]
    public void Payment_request_validation_rejects_whitespace_student_numbers()
    {
        var request = new RecordPaymentRequest
        {
            RequestId = Guid.NewGuid(),
            StudentNumber = "   ",
            Amount = 125,
            PaymentDate = new DateOnly(2026, 11, 24),
            Method = "Cash"
        };
        var validationResults = new List<ValidationResult>();

        var valid = Validator.TryValidateObject(
            request,
            new ValidationContext(request),
            validationResults,
            validateAllProperties: true);

        Assert.False(valid);
        Assert.Contains(
            validationResults,
            result => result.MemberNames.Contains(nameof(RecordPaymentRequest.StudentNumber)));
    }

    private sealed class PaymentRequestHandler(string responseBody) : HttpMessageHandler
    {
        public HttpMethod? RequestMethod { get; private set; }
        public string? RequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestMethod = request.Method;
            RequestBody = request.Content is null
                ? null
                : await request.Content.ReadAsStringAsync(cancellationToken);

            return new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent(responseBody, Encoding.UTF8, "application/json")
            };
        }
    }
}
