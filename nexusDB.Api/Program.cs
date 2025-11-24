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

// --- CONFIGURACIÓN DE SERVICIOS ---

// 1. Política de CORS
var MyAllowSpecificOrigins = "_myAllowSpecificOrigins";
builder.Services.AddCors(options =>
{
    options.AddPolicy(name: MyAllowSpecificOrigins, policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

// 2. Configuración de JWT y otros servicios de la capa de Aplicación/Infraestructura
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
builder.Services.AddInfrastructure(builder.Configuration);

// 3. Controladores y ProblemDetails para estandarizar errores HTTP
builder.Services.AddControllers();
builder.Services.AddProblemDetails(); // <-- HABILITA PROBLEM DETAILS

// 4. Swagger
builder.Services.AddEndpointsApiExplorer();
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




// 5. Autenticación JWT
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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
            // MEJORA: Validar el tiempo de vida del token con un margen de cero.
            ClockSkew = TimeSpan.Zero
        };
    });


// --- CONFIGURACIÓN DEL PIPELINE HTTP ---
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors(MyAllowSpecificOrigins);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
