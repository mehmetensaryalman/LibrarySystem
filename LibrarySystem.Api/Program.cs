using System.Text;
using LibrarySystem.Api.Hubs;
using LibrarySystem.Api.OpenApi;
using LibrarySystem.Api.Realtime;
using LibrarySystem.Application.Common.Constants;
using LibrarySystem.Application.Interfaces.Auth;
using LibrarySystem.Application.Interfaces.Books;
using LibrarySystem.Application.Interfaces.Borrow;
using LibrarySystem.Application.Interfaces.Realtime;
using LibrarySystem.Application.Interfaces.Repositories;
using LibrarySystem.Application.Services.Books;
using LibrarySystem.Application.Services.Borrow;
using LibrarySystem.Infrastructure.Identity;
using LibrarySystem.Infrastructure.Persistence;
using LibrarySystem.Infrastructure.Repositories;
using LibrarySystem.Infrastructure.Seed;
using LibrarySystem.Infrastructure.Services.Auth;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder =
    WebApplication.CreateBuilder(args);

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "Frontend",
        policy =>
        {
            policy
                .WithOrigins(
                    "http://localhost:4200")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});

builder.Services.AddDbContext<
    LibraryDbContext>(
    options =>
        options.UseSqlServer(
            builder.Configuration
                .GetConnectionString(
                    "DefaultConnection")));

builder.Services
    .AddIdentity<
        ApplicationUser,
        IdentityRole>(options =>
        {
            options.User.RequireUniqueEmail =
                true;

            options.Password.RequiredLength =
                6;

            options.Password.RequireDigit =
                true;

            options.Password.RequireLowercase =
                true;

            options.Password.RequireUppercase =
                true;

            options.Password
                .RequireNonAlphanumeric =
                false;
        })
    .AddEntityFrameworkStores<
        LibraryDbContext>()
    .AddDefaultTokenProviders();

var jwtKey =
    builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException(
        "Jwt:Key configuration is missing.");

var jwtIssuer =
    builder.Configuration["Jwt:Issuer"]
    ?? throw new InvalidOperationException(
        "Jwt:Issuer configuration is missing.");

var jwtAudience =
    builder.Configuration["Jwt:Audience"]
    ?? throw new InvalidOperationException(
        "Jwt:Audience configuration is missing.");

var expirationMinutes =
    builder.Configuration
        .GetValue<int>(
            "Jwt:ExpirationMinutes");

builder.Services.AddScoped<
    IJwtTokenService>(
    _ =>
        new JwtTokenService(
            jwtKey,
            jwtIssuer,
            jwtAudience,
            expirationMinutes));

builder.Services.AddScoped<
    IAuthService,
    AuthService>();

builder.Services.AddScoped<
    IBookRepository,
    BookRepository>();

builder.Services.AddScoped<
    IBookService,
    BookService>();

builder.Services.AddScoped<
    IBorrowRepository,
    BorrowRepository>();

builder.Services.AddScoped<
    IBorrowService,
    BorrowService>();

builder.Services.AddScoped<
    IRealtimeNotifier,
    SignalRRealtimeNotifier>();

builder.Services.AddSignalR();

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults
                .AuthenticationScheme;

        options.DefaultChallengeScheme =
            JwtBearerDefaults
                .AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey =
                    true,

                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8
                            .GetBytes(
                                jwtKey)),

                ClockSkew = TimeSpan.Zero
            };

        options.Events =
            new JwtBearerEvents
            {
                OnMessageReceived =
                    context =>
                    {
                        var accessToken =
                            context.Request
                                .Query[
                                    "access_token"]
                                .ToString();

                        var path =
                            context.HttpContext
                                .Request
                                .Path;

                        if (
                            !string.IsNullOrWhiteSpace(
                                accessToken) &&
                            path.StartsWithSegments(
                                "/hubs/library"))
                        {
                            context.Token =
                                accessToken;
                        }

                        return Task.CompletedTask;
                    }
            };
    });

builder.Services.AddAuthorization(
    options =>
    {
        options.AddPolicy(
            "BorrowerOnly",
            policy =>
            {
                policy
                    .RequireAuthenticatedUser();

                policy.RequireRole(
                    RoleNames.User);

                policy.RequireAssertion(
                    context =>
                        !context.User
                            .IsInRole(
                                RoleNames.Admin));
            });
    });

builder.Services.AddControllers();

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer<
        BearerSecuritySchemeTransformer>();
});

var app = builder.Build();

using (var scope =
       app.Services.CreateScope())
{
    var roleManager =
        scope.ServiceProvider
            .GetRequiredService<
                RoleManager<
                    IdentityRole>>();

    var userManager =
        scope.ServiceProvider
            .GetRequiredService<
                UserManager<
                    ApplicationUser>>();

    await IdentitySeeder
        .SeedRolesAsync(
            roleManager);

    await IdentitySeeder
        .SeedAdminAsync(
            userManager,
            app.Configuration[
                "AdminSeed:Email"],
            app.Configuration[
                "AdminSeed:Password"]);

    await IdentitySeeder
        .SeedExistingUserRolesAsync(
            userManager);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint(
            "/openapi/v1.json",
            "LibrarySystem API v1");
    });
}

app.UseHttpsRedirection();

app.UseCors("Frontend");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.MapHub<LibraryHub>(
        "/hubs/library")
    .RequireCors("Frontend");

app.Run();