
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VERSUM.KitchenWareERP.Data;

var builder = WebApplication.CreateBuilder(args);

// ==========================================
// DATABASE CONNECTION
// ==========================================

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// ==========================================
// ASP.NET CORE IDENTITY CONFIGURATION
// ==========================================

builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;

    // User settings
    options.User.RequireUniqueEmail = true;

    // Account lockout settings
    options.Lockout.DefaultLockoutTimeSpan =
        TimeSpan.FromMinutes(5);

    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // Email confirmation configuration
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// ==========================================
// AUTHENTICATION COOKIE CONFIGURATION
// ==========================================

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";

    options.LogoutPath = "/Account/Logout";

    options.AccessDeniedPath = "/Account/AccessDenied";

    options.Cookie.HttpOnly = true;

    options.Cookie.SecurePolicy =
        CookieSecurePolicy.Always;

    options.Cookie.Name = "VERSUM.Authentication";

    options.ExpireTimeSpan = TimeSpan.FromHours(8);

    options.SlidingExpiration = true;
});

// ==========================================
// AUTHORIZATION CONFIGURATION
// ==========================================

builder.Services.AddAuthorization(options =>
{
    var erpPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .RequireRole("Admin", "Staff")
        .Build();

    options.DefaultPolicy = erpPolicy;
    options.FallbackPolicy = erpPolicy;
});

// ==========================================
// MVC SERVICES AND FORM SECURITY
// ==========================================

builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(
        new AutoValidateAntiforgeryTokenAttribute());
});

// ==========================================
// BUILD APPLICATION
// ==========================================

var app = builder.Build();

// ==========================================
// INITIALIZE ADMIN AND STAFF ROLES
// ==========================================

if (app.Configuration.GetValue<bool>("SeedIdentity"))
{
    using var scope = app.Services.CreateScope();

    await IdentitySeeder.SeedAsync(
        scope.ServiceProvider);
}

// ==========================================
// HTTP REQUEST PIPELINE
// ==========================================

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");

    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

app.UseRouting();

// Authentication must come before Authorization

app.UseAuthentication();

app.UseAuthorization();

// ==========================================
// STATIC ASSETS
// ==========================================

app.MapStaticAssets()
    .AllowAnonymous();

// ==========================================
// MVC ROUTING
// ==========================================

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

// ==========================================
// RUN APPLICATION
// ==========================================

app.Run();
