using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MediVerify.Data;
using MediVerify.Models;
using MediVerify.Services;
using AutoMapper;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddTransient<DbInitializer>();

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 6;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddControllersWithViews()
    .AddRazorRuntimeCompilation();
builder.Services.AddRazorPages();

// Register application services
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IAuditService, AuditService>();

// AutoMapper
builder.Services.AddAutoMapper(typeof(Program));

// Authorization policies
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminPolicy", policy => policy.RequireRole("Administrator"));
    options.AddPolicy("DoctorPolicy", policy => policy.RequireRole("Doctor"));
    options.AddPolicy("PharmacistPolicy", policy => policy.RequireRole("Pharmacist"));
    options.AddPolicy("PatientPolicy", policy => policy.RequireRole("Patient"));
});

var app = builder.Build();

// Seed data
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var dbInitializer = services.GetRequiredService<DbInitializer>();
    dbInitializer.InitializeAsync().GetAwaiter().GetResult();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();