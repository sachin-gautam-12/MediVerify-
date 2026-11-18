using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MediVerify.Data;
using MediVerify.Models;
using MediVerify.Services;
using AutoMapper;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        sql => sql.CommandTimeout(180)));

builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddTransient<DbInitializer>();
builder.Services.AddHttpContextAccessor();

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.SignIn.RequireConfirmedAccount = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 6;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddControllersWithViews(options =>
    {
        // Optional string fields (notes, generic name, etc.) must not be treated as
        // implicitly required just because they are non-nullable reference types.
        options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
    })
    .AddRazorRuntimeCompilation();
builder.Services.AddRazorPages();

// Register application services
builder.Services.AddScoped<IReportService, ReportService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<ISafetyService, SafetyService>();

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
    app.UseDeveloperExceptionPage();
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseStatusCodePagesWithReExecute("/Error", "?code={0}");

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();