using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Text.Json.Serialization;

using Staj_Proje_1.Data;
using Staj_Proje_1.Models;    // ApplicationUser burada
using Staj_Proje_1.Services;  // Tek kez!

var builder = WebApplication.CreateBuilder(args);

// 1) DbContext (MySQL / Pomelo)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))
    ));

// 2) Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Parola kuralları vs. (istersen burada sıkılaştır)
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

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

// 4) Authorization
builder.Services.AddAuthorization();

// 5) Controllers + JSON
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        // "123" veya 123 ikisini de sayıya parse edebilmek için:
        opts.JsonSerializerOptions.NumberHandling = JsonNumberHandling.AllowReadingFromString;
    });

// 6) Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 7) VakıfBank Integration – HttpClient + Service
builder.Services.AddHttpClient<IBankService, BankService>(client =>
{
    var baseUrl = builder.Configuration["VakifBank:BaseUrl"];
    if (string.IsNullOrWhiteSpace(baseUrl))
        throw new InvalidOperationException("VakifBank:BaseUrl appsettings.json içinde tanımlı değil.");

    client.BaseAddress = new Uri(baseUrl);
});

var app = builder.Build();

// Dev ortamında Swagger’ı aç
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
