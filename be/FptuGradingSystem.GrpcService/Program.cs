using FptuGradingSystem.GrpcService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddGrpc();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.MapGrpcService<GradingCalcService>();
app.MapGet("/", () => "gRPC GradingCalculator service is running. Use a gRPC client to communicate.");

app.Run();
