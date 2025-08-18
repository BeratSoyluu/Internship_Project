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
    .AddDefaultTokenProviders();

// Identity uygulama çerezi (SPA için pratik ayarlar)
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;        // SPA için Lax idealdir
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

// -------------------------------------------------------
// 3) JWT + PolicyScheme (JWT varsa onu, yoksa Identity Cookie’yi kullan)
var jwtKey      = builder.Configuration["Jwt:Key"];
var jwtIssuer   = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];
if (string.IsNullOrWhiteSpace(jwtKey))
    throw new InvalidOperationException("Jwt:Key appsettings.json içinde tanımlı değil.");

var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

var auth = builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = "Smart";
    options.DefaultChallengeScheme    = "Smart";
});

// JWT
auth.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, opt =>
{
    opt.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
    opt.SaveToken = true;
    opt.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey         = signingKey,
        ValidateIssuer           = !string.IsNullOrWhiteSpace(jwtIssuer),
        ValidateAudience         = !string.IsNullOrWhiteSpace(jwtAudience),
        ValidIssuer              = jwtIssuer,
        ValidAudience            = jwtAudience,
        ValidateLifetime         = true,
        ClockSkew                = TimeSpan.FromMinutes(2)
    };
});

// PolicyScheme → Authorization header "Bearer " ise JWT; değilse Identity çerezi
auth.AddPolicyScheme("Smart", "JWT or Cookie", opt =>
{
    opt.ForwardDefaultSelector = ctx =>
    {
        var hasBearer = ctx.Request.Headers["Authorization"]
            .ToString()
            .StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase);
        return hasBearer ? JwtBearerDefaults.AuthenticationScheme
                         : IdentityConstants.ApplicationScheme;
    };
});

// -------------------------------------------------------
// 4) Authorization
builder.Services.AddAuthorization();

// -------------------------------------------------------
// 5) Controllers + JSON
builder.Services.AddControllers()
    .AddJsonOptions(o =>
    {
        o.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
        o.JsonSerializerOptions.NumberHandling = JsonNumberHandling.AllowReadingFromString;
        // o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()); // istersen
    });

// -------------------------------------------------------
// 6) Swagger (+ JWT destekli)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Staj_Proje_1 API", Version = "v1" });

    // 🔧 Şema adı çakışmalarını çöz: schemaId = FullName
    c.CustomSchemaIds(t => t.FullName?.Replace('+', '.'));

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

// 8.1) Open Banking (aggregator) service kayıt
builder.Services.AddScoped<IOpenBankingService, OpenBankingService>();

// -------------------------------------------------------
var app = builder.Build();

// Dev ortamında Swagger + ayrıntılı hata sayfası
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Eğer yalnızca HTTP (5047) kullanıyorsan ve HTTPS dev sertifikan yoksa,
// UseHttpsRedirection 307 ile https’e zorlayabilir. Gerekirse yoruma al.
app.UseHttpsRedirection();

app.UseCors(ClientCors);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
