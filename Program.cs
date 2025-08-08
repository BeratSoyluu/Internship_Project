using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Staj_Proje_1.Data;
using System.Text;
using System.Net.Http.Headers;
using Staj_Proje_1.Services;
using System.Text.Json.Serialization;
using Staj_Proje_1.Services;


// Bu program bir Web API projesinin en temel ayarlarını—veritabanı, kullanıcı sistemi, güvenlik (JWT),
// otomatik dokümantasyon ve VakıfBank entegrasyonunu—hemen çalışır hale getiriyor.

var builder = WebApplication.CreateBuilder(args);

// 1) DbContext kaydı (MySQL)
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))
    ));

// 2) Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // parola kuralları vb.
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// 3) JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme    = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var keyBytes = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!);
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey         = new SymmetricSecurityKey(keyBytes),
        ValidateIssuer           = true,
        ValidateAudience         = true,
        ValidIssuer              = builder.Configuration["Jwt:Issuer"],
        ValidAudience            = builder.Configuration["Jwt:Audience"],
        ValidateLifetime         = true
    };
});

// 4) Authorization
builder.Services.AddAuthorization();

// 5) Controllers
builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        
        opts.JsonSerializerOptions.NumberHandling =
            JsonNumberHandling.AllowReadingFromString;
    });


// 6) Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// <<< VakıfBank Integration Start >>>
// VakıfBank API’sine istek yapacak HttpClient + BankService kaydı
builder.Services.AddHttpClient<IBankService, BankService>(client =>
{
    var baseUrl = builder.Configuration["VakifBank:BaseUrl"];
    if (string.IsNullOrWhiteSpace(baseUrl))
        throw new InvalidOperationException("VakifBank:BaseUrl appsettings içinde tanımlı değil.");

    client.BaseAddress = new Uri(baseUrl);
});
// <<< VakıfBank Integration End >>>

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();  // ← Bu satırı ekledik
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// 8) Attribute-routed controller’ları eşle
app.MapControllers();

app.Run();
