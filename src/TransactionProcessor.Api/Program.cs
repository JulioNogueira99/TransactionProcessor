using FluentValidation.AspNetCore;
using Microsoft.EntityFrameworkCore;
using TransactionProcessor.Application.Interfaces;
using TransactionProcessor.Application.Services;
using TransactionProcessor.Infrastructure.Context;
using TransactionProcessor.Infrastructure.Locking;
using TransactionProcessor.Infrastructure.Outbox;
using TransactionProcessor.Infrastructure.Repositories;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddFluentValidationClientsideAdapters();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

if (string.IsNullOrWhiteSpace(connectionString) && !builder.Environment.IsEnvironment("Testing"))
    throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

if (!string.IsNullOrWhiteSpace(connectionString))
{
    builder.Services.AddDbContext<AppDbContext>(options =>
        options.UseSqlServer(connectionString, sql =>
        {
            sql.CommandTimeout(10);
            sql.EnableRetryOnFailure();
        }));
}

builder.Services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());

builder.Services.AddHealthChecks()
    .AddDbContextCheck<AppDbContext>("db");

builder.Services.AddScoped<IOutboxStore, OutboxStore>();
builder.Services.AddScoped<ITransactionRepository, TransactionRepository>();
builder.Services.AddScoped<IAccountRepository, AccountRepository>();
builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();

builder.Services.AddScoped<IAccountLock, SqlServerAccountLock>();

builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<IAccountService, AccountService>();

builder.Services.AddSingleton<IEventPublisher, LoggingEventPublisher>();
builder.Services.AddHostedService<OutboxPublisherWorker>();

var app = builder.Build();

if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetService<AppDbContext>();
    if (db is not null)
        db.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Testing"))
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler();
}

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

public partial class Program { }
