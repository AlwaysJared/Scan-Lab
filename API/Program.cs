using Libs.Data.Context;
using Libs.Services;
using Microsoft.EntityFrameworkCore;
using Libs.Repositories;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls("http://0.0.0.0:3624");

// Add services to the container.
builder.Services.AddDbContext<ScanLabContext>(options =>
            options.UseSqlite($"Data Source=.\\..\\DB\\ScanLab.db"));
builder.Services.AddSingleton<FileSystemWatcherService>();
builder.Services.AddScoped<OrderRepository>();
builder.Services.AddScoped<ScannerRepository>();
builder.Services.AddScoped<RollRepository>();
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
// app.MapGet("/ping", () => "pong from root");

app.UseAuthorization();

app.MapControllers();

app.Run();
