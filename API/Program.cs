using Libs.Data.Context;
using Libs.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
//public void ConfigureServices(IServiceCollection services)
//{
//    services.AddSingleton<FileSystemWatcherService>();
//    services.AddControllers();
//}

builder.Services.AddDbContext<ScanLabContext>(options =>
            options.UseSqlite($"Data Source=.\\..\\DB\\ScanLab.db"));
builder.Services.AddSingleton<FileSystemWatcherService>();
builder.Services.AddScoped<OrderRepository>();
builder.Services.AddScoped<ScannerRepository>();
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

// app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
