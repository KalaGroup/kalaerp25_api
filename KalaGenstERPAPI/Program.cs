using System.Text;
using KalaGenset.ERP.Core.Interface;
using KalaGenset.ERP.Core.MappingProfiles;
using KalaGenset.ERP.Core.Services;
using KalaGenset.ERP.Data.DbContexts;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;


var builder = WebApplication.CreateBuilder(args);

// ✅ Standard .NET config loading order
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

//// Configure Kestrel server limits (100MB)
//builder.WebHost.ConfigureKestrel(serverOptions =>
//{
//    serverOptions.Limits.MaxRequestBodySize = 104857600; // 100MB in bytes
//});

//// Configure Form Options (100MB)
//builder.Services.Configure<FormOptions>(options =>
//{
//    options.MultipartBodyLengthLimit = 104857600;
//    options.ValueLengthLimit = 104857600;
//    options.MultipartHeadersLengthLimit = 104857600;
//});

// Configure Kestrel server limits (500MB)
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 524288000; // 500MB in bytes
});

// Configure Form Options (500MB)
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 524288000;
    options.ValueLengthLimit = 524288000;
    options.MultipartHeadersLengthLimit = 524288000;
});

// ✅ FIX 1: Persist Data Protection keys to disk so auth cookies survive restarts
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(@"C:\Publish\ERPAngularApiNew26\DataProtectionKeys"))
    .SetApplicationName("KalaGensetERP");

// ✅ JWT Authentication — with null guard to prevent crash on missing config
var jwtSettings = builder.Configuration.GetSection("JwtSettings");

var secretKey = jwtSettings["SecretKey"]
    ?? throw new InvalidOperationException("❌ JwtSettings:SecretKey is missing from appsettings.json");
var issuer = jwtSettings["Issuer"]
    ?? throw new InvalidOperationException("❌ JwtSettings:Issuer is missing from appsettings.json");
var audience = jwtSettings["Audience"]
    ?? throw new InvalidOperationException("❌ JwtSettings:Audience is missing from appsettings.json");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
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
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };
});

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "My API", Version = "v1" });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your valid token in the text input below.\n\nExample: 'Bearer eyJhbGciOiJIUzI1NiIsInR...' ",
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// Configure the DbContext with the connection string from appsettings.json
builder.Services.AddDbContext<KalaDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("KalaDbContext")),
    ServiceLifetime.Scoped);

// Register AutoMapper and MappingProfiles
builder.Services.AddAutoMapper(typeof(EmployeeMappingProfile));

// Register Services
builder.Services.AddScoped<IEmployeeService, EmployeeService>();
builder.Services.AddScoped<IAdminService, AdminService>();
builder.Services.AddScoped<IEngineDGAssembly, EngineDGAssemblyService>();
builder.Services.AddScoped<IMarketing, MarketingService>();
builder.Services.AddScoped<ILogistic, LogisticService>();
builder.Services.AddScoped<IKalaService, KalaService>();
builder.Services.AddScoped<IDgStageChecker, DgStageCheckerService>();
builder.Services.AddScoped<IQuality, QualityService>();
builder.Services.AddScoped<ICanopy, CanopyService>();
builder.Services.AddScoped<IJobcard, JobcardService>();
builder.Services.AddScoped<I_invoiceScan, InvoiceScanService>();

// ✅ CORS Policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp", policy =>
    {
        policy.WithOrigins(
                "https://localhost",
                "http://localhost",
                "https://localhost:4200",
                "http://localhost:4200",
                "https://www.kalapms.com"
              )
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

app.UseHttpsRedirection();
// ✅ FIX 2: Removed app.UseStaticFiles() — no wwwroot folder exists, this is a pure API
app.UseRouting();
app.UseCors("AllowAngularApp");
app.UseAuthentication();
app.UseAuthorization();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("../swagger/v1/swagger.json", "My API V1");
});

app.MapControllers();

// ✅ FIX 3: Wrap Run() in try/catch to log startup crashes clearly
try
{
    app.Run();
}
catch (Exception ex)
{
    Console.WriteLine($"💥 Application crashed on startup: {ex.Message}");
    Console.WriteLine(ex.ToString());
    Console.ReadLine(); // Keep console open to read the error
}