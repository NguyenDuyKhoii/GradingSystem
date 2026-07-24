using FptuGradingSystem.Application.Common.Interfaces;
using FptuGradingSystem.Infrastructure;
using FptuGradingSystem.Worker;
using FptuGradingSystem.Worker.Services;
using Microsoft.Extensions.DependencyInjection;

var builder = Host.CreateApplicationBuilder(args);

// Add Infrastructure services (DbContext, Redis, etc.)
builder.Services.AddInfrastructureServices(builder.Configuration);

// Register Email Service
builder.Services.AddSingleton<IEmailService, MailtrapEmailService>();

// Register background workers
builder.Services.AddHostedService<Worker>();
builder.Services.AddHostedService<GradeNotificationConsumer>();
builder.Services.AddHostedService<GradeReportWorker>();

var host = builder.Build();
host.Run();
