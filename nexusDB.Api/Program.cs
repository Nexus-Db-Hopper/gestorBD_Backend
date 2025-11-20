using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using nexusDB.Application.Configuration;
using nexusDB.Application.Interfaces.Instances;
using nexusDB.Application.Interfaces.Providers;
using nexusDB.Application.Interfaces.Repositories;
using nexusDB.Application.Interfaces.Security;
using nexusDB.Application.Services;
using nexusDB.Domain.Docker;
using nexusDB.Domain.Docker.Providers;
using nexusDB.Infrastructure.Extensions;
using nexusDB.Infrastructure.Repositories;
using nexusDB.Infrastructure.Services.Security;
using Swashbuckle.AspNetCore.Filters;

var builder = WebApplication.CreateBuilder(args);

// 1. Configura JwtSettings
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

// 2. Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// 3. Configuración de Swagger con soporte para JWT
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Description = "Standard Authorization header using the Bearer scheme (\"bearer {token}\")",
        In = ParameterLocation.Header,
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey
    });
    options.OperationFilter<SecurityRequirementsOperationFilter>();
});
// Application
builder.Services.AddScoped<IInstanceService, InstanceService>();

// Domain/Infrastructure
builder.Services.AddScoped<IInstanceRepository, InstanceRepository>();
builder.Services.AddScoped<IDatabaseProviderFactory, DatabaseProviderFactory>();
builder.Services.AddScoped<IAesEncryptionService, AesEncryptionService>();

// Providers
builder.Services.AddScoped<IDatabaseProvider, MySqlProvider>();
builder.Services.AddScoped<IDatabaseProvider, MongoDbProvider>();
builder.Services.AddScoped<IDatabaseProvider, SqlServerProvider>();


// 4. Database and other services from Infrastructure layer
builder.Services.AddInfrastructure(builder.Configuration); // Esta línea registra todos los servicios necesarios

// 5. Configura el middleware de autenticación JWT
builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>()!;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey))
        };
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// El orden es CRÍTICO aquí.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
