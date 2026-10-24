using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TasteTogether.Core.Interfaces;
using TasteTogether.Data;
using TasteTogether.Service;
using TasteTogether.Service.Mapping;
using TasteTogether.Core.Settings;
using Microsoft.OpenApi.Models;
using TasteTogether.API.Swagger;
using TasteTogether.API.Middleware;
using NLog.Web;

// שם מדיניות ה-CORS. הכתובות המורשות מחושבות למטה
const string ClientCorsPolicy = "Client";
const string LocalClientOrigin = "http://localhost:5173";

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Host.UseNLog();

builder.Services.AddDbContext<TasteTogetherDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddScoped<IWorkshopRepository, WorkshopRepository>();
builder.Services.AddScoped<IRegistrationRepository, RegistrationRepository>();
builder.Services.AddScoped<IRecipeRepository, RecipeRepository>();
builder.Services.AddAutoMapper(typeof(MappingProfile));

builder.Services.AddScoped<IWorkshopService, WorkshopService>();
builder.Services.AddScoped<IRegistrationService, RegistrationService>();
builder.Services.AddScoped<IRecipeService, RecipeService>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>();
builder.Services.AddScoped<ITokenService, TokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();

// יוצר נתוני דמו בהפעלה (ב-Development בלבד, ראו למטה)
builder.Services.AddScoped<DbSeeder>();

// CORS: מרשה רק את הכתובות של הלקוח.
// בפיתוח: שרת Vite המקומי. בענן: הכתובת שמוגדרת בהגדרה Cors:ClientOrigin
// (משתנה סביבה Cors__ClientOrigin). הכתובת חייבת להיות בלי / בסוף.
// X-Correlation-Id חשוף כדי שהלקוח יוכל להציג אותו בהודעת שגיאה.
var allowedOrigins = new List<string>();
if (builder.Environment.IsDevelopment())
{
    allowedOrigins.Add(LocalClientOrigin);
}

var configuredOrigin = builder.Configuration["Cors:ClientOrigin"]?.Trim().TrimEnd('/');
if (!string.IsNullOrWhiteSpace(configuredOrigin))
{
    allowedOrigins.Add(configuredOrigin);
}

builder.Services.AddCors(options =>
{
    options.AddPolicy(ClientCorsPolicy, policy =>
        policy.WithOrigins(allowedOrigins.ToArray())
            .AllowAnyHeader()
            .AllowAnyMethod()
            .WithExposedHeaders(RequestLoggingMiddleware.CorrelationIdHeader));
});

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the token only, without the word Bearer."
    });

    options.OperationFilter<AuthorizeOperationFilter>();
});




var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()
    ?? throw new InvalidOperationException("Jwt section is missing in configuration.");

if (string.IsNullOrWhiteSpace(jwtSettings.Key))
{
    throw new InvalidOperationException("Jwt:Key is missing. Set it in User Secrets.");
}

builder.Services.AddSingleton(jwtSettings);

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
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
            ClockSkew = TimeSpan.Zero
        };
    });

var app = builder.Build();

// ב-Development בלבד, יוצרים את המסד ומריצים את ה-Migrations אם צריך, ואז יוצרים נתוני דמו
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();

    var dbContext = scope.ServiceProvider.GetRequiredService<TasteTogetherDbContext>();
    await dbContext.Database.MigrateAsync();

    await scope.ServiceProvider.GetRequiredService<DbSeeder>().SeedAsync(CancellationToken.None);
}

// CORS ראשון בצנרת, כדי שגם תשובות שגיאה (400, 404, 409) יכללו את ה-headers
// והדפדפן יאפשר ללקוח לקרוא את גוף השגיאה.
app.UseCors(ClientCorsPolicy);

app.UseMiddleware<ErrorHandlingMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// בענן (Production) ה-HTTPS מטופל על ידי Render מחוץ לשרת, והשרת עצמו מקבל HTTP
if (!app.Environment.IsProduction())
{
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();