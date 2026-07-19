using DarkStar.Application;
using DarkStar.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddDarkStarApplication()
    .AddDarkStarInfrastructure(builder.Configuration);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();

public partial class Program;
