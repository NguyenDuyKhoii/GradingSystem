using FptuGradingSystem.GrpcService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddGrpc();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<GradingCalcService>();
app.MapGrpcService<DocumentReaderService>();
app.MapGet("/", () => "gRPC GradingCalculator & DocumentReader service is running.");

app.Run();
