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
// 0) Config kaynaklarını netleştir (opsiyonel ama faydalı)
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true);

if (builder.Environment.IsDevelopment())
{
    builder.Configuration.AddUserSecrets<Program>(optional: true);
}

// -------------------------------------------------------
// 1) DbContext (MySQL / Pomelo)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))
    ));

// -------------------------------------------------------
// 2) Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Parola kuralları vs.
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// -------------------------------------------------------
// 3) JWT Authentication
builder.Services.AddAuthentication(opt =>
{
    opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    opt.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(opt =>
{
    var key = builder.Configuration["Jwt:Key"];
    if (string.IsNullOrWhiteSpace(key))
        throw new InvalidOperationException("Jwt:Key appsettings.json içinde tanımlı değil.");

    opt.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey         = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
        ValidateIssuer           = true,
        ValidateAudience         = true,
        ValidIssuer              = builder.Configuration["Jwt:Issuer"],
        ValidAudience            = builder.Configuration["Jwt:Audience"],
        ValidateLifetime         = true
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
        // "123" veya 123 ikisini de sayıya parse edebilmek için:
        opts.JsonSerializerOptions.NumberHandling = JsonNumberHandling.AllowReadingFromString;
        // Enum'ları string olarak yazmak istersen (opsiyonel):
        // opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

// -------------------------------------------------------
// 6) Swagger / OpenAPI (+ JWT destekli)
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Staj_Proje_1 API", Version = "v1" });

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "JWT Bearer token giriniz."
    };
    c.AddSecurityDefinition("Bearer", securityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            securityScheme, Array.Empty<string>()
        }
    });
});

// -------------------------------------------------------
// 7) CORS (Angular için örnek)
const string ClientCors = "client";
builder.Services.AddCors(opts =>
{
    opts.AddPolicy(ClientCors, policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:4200",
                "https://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// -------------------------------------------------------
// 8) VakıfBank Integration – HttpClient + Service
builder.Services.AddHttpClient<IBankService, BankService>(client =>
{
    var baseUrl = builder.Configuration["VakifBank:BaseUrl"];
    if (string.IsNullOrWhiteSpace(baseUrl))
        throw new InvalidOperationException("VakifBank:BaseUrl appsettings.json içinde tanımlı değil.");

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
