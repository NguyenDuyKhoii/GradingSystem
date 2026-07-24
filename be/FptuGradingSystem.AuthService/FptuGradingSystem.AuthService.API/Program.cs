using FptuGradingSystem.AuthService.Application;
using FptuGradingSystem.AuthService.Infrastructure;
using FptuGradingSystem.AuthService.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(8080, o => o.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http1);
    options.ListenAnyIP(8081, o => o.Protocols = Microsoft.AspNetCore.Server.Kestrel.Core.HttpProtocols.Http2);
});

// Add Application & Infrastructure Layers
builder.Services.AddAuthApplicationServices();
builder.Services.AddAuthInfrastructureServices(builder.Configuration);

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "FPTU Auth Service API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using Bearer scheme.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// Configure JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secret = jwtSettings.GetValue<string>("Secret");
var issuer = jwtSettings.GetValue<string>("Issuer");
var audience = jwtSettings.GetValue<string>("Audience");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret!))
    };
});

builder.Services.AddGrpc();
builder.Services.AddAuthorization();

var app = builder.Build();

// Ensure DB is created & migrated on startup
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    var hasher = scope.ServiceProvider.GetRequiredService<FptuGradingSystem.AuthService.Application.Common.Interfaces.IPasswordHasher>();

    dbContext.Database.EnsureCreated();

    if (!dbContext.Users.Any())
    {
        dbContext.Users.AddRange(
            new FptuGradingSystem.AuthService.Domain.Entities.User
            {
                Username = "academic",
                PasswordHash = hasher.HashPassword("12345"),
                FullName = "Academic Staff",
                Role = "AcademicStaff",
                Email = "academic@fpt.edu.vn"
            },
            new FptuGradingSystem.AuthService.Domain.Entities.User
            {
                Username = "lecturer1",
                PasswordHash = hasher.HashPassword("12345"),
                FullName = "Dr. Nguyen Van A",
                Role = "Lecturer",
                Email = "lecturer1@fpt.edu.vn"
            },
            new FptuGradingSystem.AuthService.Domain.Entities.User
            {
                Username = "lecturer2",
                PasswordHash = hasher.HashPassword("12345"),
                FullName = "Dr. Tran Thi B",
                Role = "Lecturer",
                Email = "lecturer2@fpt.edu.vn"
            }
        );
        dbContext.SaveChanges();
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapGrpcService<FptuGradingSystem.AuthService.API.Services.UserSyncServiceImpl>();

app.Run();
