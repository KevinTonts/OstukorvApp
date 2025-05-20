using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OstukorvApp.Data;
using OstukorvApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Ainult vajalikud teenused
builder.Services.AddControllers();
builder.Services.AddScoped<MealPlannerService>();
builder.Services.AddScoped<SelverScraperService>();
builder.Services.AddScoped<CoopScraperService>();
builder.Services.AddScoped<CartComparisonService>();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();

app.MapControllers();
app.Run();
