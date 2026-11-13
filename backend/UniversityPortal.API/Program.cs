using System.Text.Json.Serialization;
using UniversityPortal.API.Common.Exceptions;
using UniversityPortal.API.Common.Security;
using UniversityPortal.API.Infrastructure.Persistence;
using UniversityPortal.API.Integrations.BusinessCentral.Configuration;
using UniversityPortal.API.Modules.Administration;
using UniversityPortal.API.Modules.Administration.Services;
using UniversityPortal.API.Modules.Finance;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddPortalPersistence(builder.Configuration);
builder.Services.AddAdministrationModule();

if (builder.Environment.IsDevelopment())
    builder.Services.AddScoped<ICurrentUser, DevCurrentUser>();
else
    builder.Services.AddScoped<ICurrentUser, AnonymousCurrentUser>();

builder.Services.AddBusinessCentralIntegration(builder.Configuration);
builder.Services.AddFinanceModule();

var app = builder.Build();
await app.SeedIdentityAsync();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();
app.Run();