using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json.Serialization;

using Staj_Proje_1.Data;
using Staj_Proje_1.Models;    // ApplicationUser
using Staj_Proje_1.Services;  // IBankService, BankService

var builder = WebApplication.CreateBuilder(args);

// -------------------------------------------------------
// 0) Config
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);
if (builder.Environment.IsDevelopment())
    builder.Configuration.AddUserSecrets<Program>(optional: true);

// -------------------------------------------------------
// 1) DbContext (MySQL / Pomelo)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    var cs = builder.Configuration.GetConnectionString("DefaultConnection")
             ?? throw new InvalidOperationException("ConnectionString 'DefaultConnection' yok.");
    options.UseMySql(cs, ServerVersion.AutoDetect(cs));
});

// -------------------------------------------------------
// 2) Identity (şifre kurallarını burada yumuşatabilirsiniz)
builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(opt =>
    {
        opt.User.RequireUniqueEmail = true;
        opt.Password.RequireDigit = false;
        opt.Password.RequireLowercase = false;
        opt.Password.RequireUppercase = false;
        opt.Password.RequireNonAlphanumeric = false;
        opt.Password.RequiredLength = 6;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders(); // şifre sıfırlama vb. için gerekli

// -------------------------------------------------------
// 3) JWT + Cookie (hibrit) kimlik doğrulama
var jwtKey = builder.Configuration["Jwt:Key"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];
if (string.IsNullOrWhiteSpace(jwtKey))
    throw new InvalidOperationException("Jwt:Key appsettings.json içinde tanımlı değil.");

builder.Services
    .AddAuthentication(options =>
    {
        // "Akıllı" şema: Authorization header varsa JWT, yoksa Cookie
        options.DefaultAuthenticateScheme = "Smart";
        options.DefaultChallengeScheme = "Smart";
    })
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, opt =>
    {
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer           = !string.IsNullOrWhiteSpace(jwtIssuer),
            ValidateAudience         = !string.IsNullOrWhiteSpace(jwtAudience),
            ValidIssuer              = jwtIssuer,
            ValidAudience            = jwtAudience,
            ValidateLifetime         = true,
            ClockSkew                = TimeSpan.FromMinutes(2)
        };
    })
    .AddPolicyScheme("Smart", "JWT or Cookie", opt =>
    {
        opt.ForwardDefaultSelector = ctx =>
        {
            var hasBearer = ctx.Request.Headers["Authorization"].ToString()
                .StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase);
            return hasBearer ? JwtBearerDefaults.AuthenticationScheme
                             : IdentityConstants.ApplicationScheme; // Identity'nin cookie şeması
        };
    });

// -------------------------------------------------------
// 4) Authorization
builder.Services.AddAuthorization();

// -------------------------------------------------------
// 5) Controllers + JSON
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        // "123" veya "123.45" string'lerini sayıya parse edebilir
        opts.JsonSerializerOptions.NumberHandling = JsonNumberHandling.AllowReadingFromString;
        // opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()); // isterseniz
        opts.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });

// -------------------------------------------------------
// 6) Swagger (+ JWT destekli)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Staj_Proje_1 API", Version = "v1" });

    var jwtScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Bearer token"
    };
    c.AddSecurityDefinition("Bearer", jwtScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { jwtScheme, Array.Empty<string>() }
    });
});

// -------------------------------------------------------
// 7) CORS (Angular dev için)
const string ClientCors = "client";
builder.Services.AddCors(opts =>
{
    opts.AddPolicy(ClientCors, p =>
        p.WithOrigins("http://localhost:4200", "https://localhost:4200")
         .AllowAnyHeader()
         .AllowAnyMethod()
         .AllowCredentials());
});

// -------------------------------------------------------
// 8) VakıfBank Integration – HttpClient + Service
builder.Services.AddHttpClient<IBankService, BankService>(client =>
{
    var baseUrl = builder.Configuration["VakifBank:BaseUrl"]
                  ?? throw new InvalidOperationException("VakifBank:BaseUrl eksik.");
    client.BaseAddress = new Uri(baseUrl);
});

// -------------------------------------------------------
var app = builder.Build();

// Dev ortamında Swagger + ayrıntılı hata sayfası
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors(ClientCors);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
