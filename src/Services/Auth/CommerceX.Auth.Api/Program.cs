using CommerceX.Auth.Application;
using CommerceX.Auth.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Register Auth Service application layer.
builder.Services.AddAuthApplication();

// Register Auth Service infrastructure.
builder.Services.AddAuthInfrastructure(builder.Configuration);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.Run();