using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using site.Data;
using site.Models;

var builder = WebApplication.CreateBuilder(args);

// 🔥 DB
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
});

// 🔥 Identity
builder.Services.AddDefaultIdentity<User>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<AppDbContext>();
// 🔥 MVC + Razor
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

var app = builder.Build();

// 🔥 Middleware
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// 🔥 ВАЖНЫЙ ПОРЯДОК
app.UseAuthentication();

app.UseAuthorization();

// 🔥 Маршруты
app.MapRazorPages();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Article}/{action=Index}/{id?}");

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();

    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

    string[] roles = { "Admin", "Author", "User" };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }
    }

    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

    var email = "admin@mail.com";
    var password = "Admin123!";

    var user = await userManager.FindByEmailAsync(email);

    if (user == null)
    {
        user = new User { UserName = email, Email = email };

        var result = await userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            throw new Exception("User creation failed: " + string.Join(", ", result.Errors.Select(e => e.Description)));
        }

        // 🔥 ПЕРЕЗАГРУЖАЕМ пользователя
        user = await userManager.FindByEmailAsync(email);
    }

    // 🔥 ТОЛЬКО ПОСЛЕ ПРОВЕРКИ
    if (!await userManager.IsInRoleAsync(user, "Admin"))
    {
        var roleResult = await userManager.AddToRoleAsync(user, "Admin");

        if (!roleResult.Succeeded)
        {
            throw new Exception("Role assignment failed: " + string.Join(", ", roleResult.Errors.Select(e => e.Description)));
        }
    }
}

app.Run();