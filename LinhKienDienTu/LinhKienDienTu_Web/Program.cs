using Microsoft.EntityFrameworkCore;
using LinhKienDienTu_Web.Models;
using LinhKienDienTu_Web.Services;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using LinhKienDienTu_Web.Hubs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<ComboService>();
builder.Services.AddScoped<PasswordSecurityService>();
builder.Services.AddScoped<ShippingService>();
builder.Services.AddScoped<PaymentService>();
builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddHttpContextAccessor();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("LinhKienDB")
    ));

builder.Services.AddHostedService<NewsletterBackgroundService>();

builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = "CookieAuth";
    options.DefaultChallengeScheme = "CookieAuth";
})
.AddCookie("CookieAuth", options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.Name = "UserSession";
})
.AddCookie("ExternalCookie", options =>
{
    options.Cookie.Name = "ExternalSession";
    options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
})
.AddGoogle(googleOptions =>
{
    googleOptions.SignInScheme = "ExternalCookie";
    googleOptions.ClientId = builder.Configuration["Authentication:Google:ClientId"] ?? "placeholder";
    googleOptions.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"] ?? "placeholder";
})
.AddFacebook(facebookOptions =>
{
    facebookOptions.SignInScheme = "ExternalCookie";
    facebookOptions.AppId = builder.Configuration["Authentication:Facebook:AppId"] ?? "placeholder";
    facebookOptions.AppSecret = builder.Configuration["Authentication:Facebook:AppSecret"] ?? "placeholder";
})
.AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["JwtOptions:Issuer"],
        ValidAudience = builder.Configuration["JwtOptions:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["JwtOptions:SecretKey"]))
    };
});

builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    //dbContext.Database.Migrate();
}
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

    if (!context.Users.Any(u => u.Email == "admin@gmail.com"))
    {
        var hasher = new Microsoft.AspNetCore.Identity.PasswordHasher<User>();

        var admin = new User
        {
            Ho_Ten = "Admin",
            Email = "admin@gmail.com",
            So_Dien_Thoai = "0903724639",
            Mat_Khau = hasher.HashPassword(null, "123456"),
            Role = "Admin",
            Created_At = DateTime.Now
        };

        context.Users.Add(admin);
        context.SaveChanges();
    }
}



// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseSession();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.MapHub<ChatHub>("/chathub");

app.Run();

