using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using users_api.Domain.Entities;
using users_api.Identity;
using users_api.Infra.Ef;
using users_api.Interface.Configuracao;
using users_api.Messaging;

var builder = WebApplication.CreateBuilder(args);



builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerDocumentation();

string? connFromSection = builder.Configuration["ConnectionStrings:Default"];
string? connFromGet = builder.Configuration.GetConnectionString("Default");
string? connectionString = connFromSection ?? connFromGet;

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("Connection string 'ConnectionStrings:Default' não encontrada. Garanta que ConnectionStrings__Default esteja definida.");
}

builder.Services.AddDbContext<UsersDbContext>(options =>
{
    options.UseSqlServer(connectionString, sql =>
    {
        sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null);
    });
});

builder.Services
    .AddIdentity<Usuario, IdentityRole>()
    .AddEntityFrameworkStores<UsersDbContext>()
    .AddDefaultTokenProviders();

string? jwtKeyRaw =
    builder.Configuration["Jwt:SymmetricSecurityKey"] ??
    builder.Configuration["Jwt__SymmetricSecurityKey"] ??
    builder.Configuration["Jwt:SymmetricKey"] ??
    builder.Configuration["Jwt__SymmetricKey"] ??
    builder.Configuration["JWT:Secret"] ??
    builder.Configuration["Jwt:Key"];

if (string.IsNullOrWhiteSpace(jwtKeyRaw))
{
    throw new InvalidOperationException("A chave JWT não foi informada. Defina 'Jwt:SymmetricSecurityKey' (ou Jwt__SymmetricSecurityKey).");
}

byte[] jwtKeyBytes = IsBase64(jwtKeyRaw) ? Convert.FromBase64String(jwtKeyRaw.Trim()) : Encoding.UTF8.GetBytes(jwtKeyRaw);

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
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(jwtKeyBytes),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero,
            RoleClaimType = ClaimTypes.Role
        };
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
    });

builder.Services.AddAuthorization();

builder.Services.AddBus(builder.Configuration);

builder.Services.AddScoped<TokenService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<UsersDbContext>();
    var pending = await db.Database.GetPendingMigrationsAsync();
    if (pending.Any())
        await db.Database.MigrateAsync();
    else
        await db.Database.EnsureCreatedAsync();
}

app.UseSwagger();
app.UseSwaggerUI();

if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

await app.RunAsync();

static bool IsBase64(string value)
{
    if (string.IsNullOrWhiteSpace(value)) return false;
    value = value.Trim();
    if (value.Length % 4 != 0) return false;
    foreach (var c in value)
    {
        bool ok = (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '+' || c == '/' || c == '=';
        if (!ok) return false;
    }
    return true;
}
