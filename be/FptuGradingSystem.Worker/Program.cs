using FptuGradingSystem.Infrastructure;
using FptuGradingSystem.Worker;

var builder = Host.CreateApplicationBuilder(args);

// Add Infrastructure services (DbContext, Redis, etc.)
builder.Services.AddInfrastructureServices(builder.Configuration);

// Register background workers
builder.Services.AddHostedService<GradeNotificationConsumer>();
builder.Services.AddHostedService<GradeReportWorker>();

var host = builder.Build();
host.Run();
