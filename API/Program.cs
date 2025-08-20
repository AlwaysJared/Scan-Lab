using Libs.Data.Context;
using Libs.Services;
using Microsoft.EntityFrameworkCore;
using Libs.Repositories;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Json;
using Serilog.Sinks.PostgreSQL;
using Microsoft.AspNetCore.Identity;
using Libs.Data.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authorization;



var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:3624");

// Add services to the container.
builder.Services.AddDbContext<ScanLabContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("ScanLabDBConnection")));

builder.Services.AddDbContext<ScanLab_LogContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("ScanLab_LogsDBConnection")));

builder.Services
    .AddIdentity<Staff, IdentityRole<Guid>>(options =>
    {
        options.User.RequireUniqueEmail = true;
        options.Password.RequireDigit = true;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 8;
    })
    .AddEntityFrameworkStores<ScanLabContext>()
    .AddDefaultTokenProviders();

// JWT Config
var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];

builder.Services
    .AddAuthentication(options =>
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
            ValidIssuer = jwtIssuer,
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };

        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"JWT auth failed: {context.Exception}");
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();


builder.Host.UseSerilog((context, services, configuration) =>
{
    var columnWriters = new Dictionary<string, ColumnWriterBase>
    {
        { "timestamp", new TimestampColumnWriter() },
        { "level", new LevelColumnWriter(true, NpgsqlTypes.NpgsqlDbType.Text) },
        { "message", new RenderedMessageColumnWriter() },
        { "message_template", new MessageTemplateColumnWriter() },
        { "exception", new ExceptionColumnWriter() },
        { "properties", new PropertiesColumnWriter(NpgsqlTypes.NpgsqlDbType.Jsonb) },
        { "log_event", new LogEventSerializedColumnWriter(NpgsqlTypes.NpgsqlDbType.Jsonb) },
        { "area", new SinglePropertyColumnWriter("Area", PropertyWriteMethod.Raw) }
    };

    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .Enrich.WithEnvironmentName()
        .Enrich.WithThreadId()
        .Enrich.WithProcessId()
        .Enrich.WithProperty("Area", "System")
        .WriteTo.PostgreSQL(
            builder.Configuration.GetConnectionString("ScanLab_LogsDBConnection"),
            tableName: "logs",
            columnOptions: columnWriters,
            needAutoCreateTable: true  // Set true if you want Serilog to create the table
        );
});

builder.Services.AddSingleton<FileSystemWatcherService>();
builder.Services.AddScoped<OrderRepository>();
builder.Services.AddScoped<ScannerRepository>();
builder.Services.AddScoped<RollRepository>();
builder.Services.AddScoped<LogRepository>();
builder.Services.AddScoped<AuthRepository>();
builder.Services.AddScoped<StaffRepository>();
builder.Services.AddScoped<GmailService>();
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSerilogRequestLogging(); // Optional: logs HTTP request info

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
// app.MapGet("/ping", () => "pong from root");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
