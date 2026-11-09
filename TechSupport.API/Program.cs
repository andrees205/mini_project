using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using TechSupport.API.Data;
using TechSupport.API.Middleware;
using TechSupport.API.Services;

var builder = WebApplication.CreateBuilder(args);

var jwtSettings = builder.Configuration.GetSection(JwtSettings.Seccion).Get<JwtSettings>() ?? new JwtSettings();
if (string.IsNullOrWhiteSpace(jwtSettings.Key) || Encoding.UTF8.GetByteCount(jwtSettings.Key) < 32)
    throw new InvalidOperationException(
        "Falta la clave JWT (Jwt:Key) o tiene menos de 32 bytes. Configúrala con user-secrets o la variable de entorno Jwt__Key.");

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.Seccion));

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IIncidenteService, IncidenteService>();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role
        };

        options.Events = new JwtBearerEvents
        {
            OnChallenge = async context =>
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new
                {
                    success = false,
                    message = "No autenticado: falta el token o no es válido."
                });
            },
            OnForbidden = async context =>
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new
                {
                    success = false,
                    message = "No tienes permiso para realizar esta operación."
                });
            }
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddControllers(o =>
    {
        o.ModelBindingMessageProvider.SetValueIsInvalidAccessor(_ => "Valor no válido.");
        o.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
    })
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        o.AllowInputFormatterExceptionMessages = false;
    })
    .ConfigureApiBehaviorOptions(o =>
    {
        o.InvalidModelStateResponseFactory = context =>
        {
            var errores = context.ModelState
                .Where(e => e.Value?.Errors.Count > 0)
                .ToDictionary(
                    e => string.IsNullOrEmpty(e.Key) ? "body" : e.Key.TrimStart('$', '.'),
                    e => e.Value!.Errors.Select(x => string.IsNullOrEmpty(x.ErrorMessage) ? "Valor no válido." : x.ErrorMessage).ToArray());

            return new BadRequestObjectResult(new
            {
                success = false,
                message = "Los datos enviados no son válidos.",
                errors = errores
            });
        };
    });

builder.Services.AddOpenApi(o => o.AddDocumentTransformer((document, _, _) =>
{
    document.Components ??= new OpenApiComponents();
    document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
    document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Pega el token devuelto por POST /api/auth/login"
    };
    document.Security ??= [];
    document.Security.Add(new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
    return Task.CompletedTask;
}));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    DbSeeder.Seed(db);
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwaggerUI(o => o.SwaggerEndpoint("/openapi/v1.json", "TechSupport API v1"));
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
