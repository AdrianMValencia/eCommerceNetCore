using Carter;
using eCommerce.Api;
using eCommerce.Api.Database;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDependencies(builder.Configuration);

builder.Services.AddCarter();

builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularClient", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200")
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials();
    });
});

var app = builder.Build();

await app.InitializeAsync();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors("AllowAngularClient");
app.UseHttpsRedirection();

// Se habilita la autenticación JWT antes de mapear los endpoints.
app.UseAuthentication();
app.UseAuthorization();

app.MapCarter();

app.Run();
