using CMCS_ST10026321.Helpers;
using CMCS_ST10026321.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// EF Core + SQL Server
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity + Roles
builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    // Adjust password rules if needed
    // options.Password.RequireNonAlphanumeric = false;
})
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>();

// Add this line with your other service registrations
builder.Services.AddScoped<IClaimAutomationService, ClaimAutomationService>();

// Razor Pages (for /Identity/Account/Login etc.)
builder.Services.AddRazorPages();

// MVC
builder.Services.AddControllersWithViews();

var app = builder.Build();

// ------------ Apply migrations + Seed Roles & Test Users ------------
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;

    // Ensure database and all migrations are applied
    var dbContext = services.GetRequiredService<AppDbContext>();
    await dbContext.Database.MigrateAsync();

    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

    string[] roles = { Roles.Admin, Roles.HR, Roles.Employee };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    // Admin user
    var adminEmail = "admin@cmcs.local";
    var adminUser = await userManager.FindByEmailAsync(adminEmail);
    if (adminUser == null)
    {
        adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true
        };

        await userManager.CreateAsync(adminUser, "Admin123!");
        await userManager.AddToRoleAsync(adminUser, Roles.Admin);
    }

    // HR user (optional)
    var hrEmail = "hr@cmcs.local";
    var hrUser = await userManager.FindByEmailAsync(hrEmail);
    if (hrUser == null)
    {
        hrUser = new ApplicationUser
        {
            UserName = hrEmail,
            Email = hrEmail,
            EmailConfirmed = true
        };

        await userManager.CreateAsync(hrUser, "Hr123!");
        await userManager.AddToRoleAsync(hrUser, Roles.HR);
    }

    // Employee user (optional)
    var employeeEmail = "employee@cmcs.local";
    var employeeUser = await userManager.FindByEmailAsync(employeeEmail);
    if (employeeUser == null)
    {
        employeeUser = new ApplicationUser
        {
            UserName = employeeEmail,
            Email = employeeEmail,
            EmailConfirmed = true
        };

        await userManager.CreateAsync(employeeUser, "Employee123!");
        await userManager.AddToRoleAsync(employeeUser, Roles.Employee);
    }
}
// ------------ End seeding ------------

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// MVC routes
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// Razor Pages routes (for Identity UI)
app.MapRazorPages();

app.Run();
