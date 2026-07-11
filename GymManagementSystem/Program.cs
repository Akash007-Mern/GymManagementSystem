using GymManagementSystem.Data;
using GymManagementSystem.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);


// 1. Connect to database
builder.Services.AddDbContext<GymDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("GymConnection")));

// 2. Add Identity with Roles support
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password rules — keep simple for now
    options.Password.RequireDigit = true;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;

    // Login settings
    options.SignIn.RequireConfirmedAccount = false;
})
.AddEntityFrameworkStores<GymDbContext>()
.AddDefaultTokenProviders();

// 3. Where to redirect if not logged in
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// 4. Add MVC
builder.Services.AddControllersWithViews();

// Register Email Service
builder.Services.AddScoped<GymManagementSystem.Services.EmailService>();

var app = builder.Build();

// 5. Seed Admin user on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    await SeedData.InitializeAsync(services);
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// IMPORTANT ORDER: Authentication before Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();