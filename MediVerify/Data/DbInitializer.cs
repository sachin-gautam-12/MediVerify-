using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MediVerify.Models;
using System;
using System.Threading.Tasks;

namespace MediVerify.Data
{
    public class DbInitializer
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public DbInitializer(ApplicationDbContext context, // ConnectionString not initialized


                             UserManager<ApplicationUser> userManager,
                             RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task InitializeAsync()
        {
            // Apply pending migrations
            await _context.Database.MigrateAsync();

            // Seed roles
            string[] roleNames = { "Administrator", "Doctor", "Pharmacist", "Patient" };
            foreach (var roleName in roleNames)
            {
                if (!await _roleManager.RoleExistsAsync(roleName))
                {
                    await _roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }

            // Seed demo admin (only if not exists)
            var adminEmail = "admin@mediverify.com";
            if (await _userManager.FindByEmailAsync(adminEmail) == null)
            {
                var admin = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "System Administrator",
                    EmailConfirmed = true,
                    IsActive = true
                };
                var result = await _userManager.CreateAsync(admin, "Admin#123");
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(admin, "Administrator");
                }
            }

            // Demo Doctor
            var docEmail = "doctor@mediverify.com";
            if (await _userManager.FindByEmailAsync(docEmail) == null)
            {
                var doctor = new ApplicationUser
                {
                    UserName = docEmail,
                    Email = docEmail,
                    FullName = "Dr. John Doe",
                    EmailConfirmed = true,
                    IsActive = true
                };
                var res = await _userManager.CreateAsync(doctor, "Doctor#123");
                if (res.Succeeded)
                {
                    await _userManager.AddToRoleAsync(doctor, "Doctor");
                }
            }

            // Demo Pharmacist
            var pharmEmail = "pharma@mediverify.com";
            if (await _userManager.FindByEmailAsync(pharmEmail) == null)
            {
                var pharmacist = new ApplicationUser
                {
                    UserName = pharmEmail,
                    Email = pharmEmail,
                    FullName = "Pharmacist Jane Smith",
                    EmailConfirmed = true,
                    IsActive = true
                };
                var res = await _userManager.CreateAsync(pharmacist, "Pharma#123");
                if (res.Succeeded)
                {
                    await _userManager.AddToRoleAsync(pharmacist, "Pharmacist");
                }
            }

            // Demo Patient (linked to ApplicationUser and Patient entity)
            var patEmail = "patient@mediverify.com";
            if (await _userManager.FindByEmailAsync(patEmail) == null)
            {
                var patientUser = new ApplicationUser
                {
                    UserName = patEmail,
                    Email = patEmail,
                    FullName = "Patient Alice",
                    EmailConfirmed = true,
                    IsActive = true
                };
                var res = await _userManager.CreateAsync(patientUser, "Patient#123");
                if (res.Succeeded)
                {
                    await _userManager.AddToRoleAsync(patientUser, "Patient");
                    var patient = new Patient
                    {
                        ApplicationUserId = patientUser.Id,
                        DateOfBirth = new DateTime(1990, 1, 1),
                        Gender = "Female",
                        Address = "123 Main St",
                        EmergencyContact = "555-1234"
                    };
                    _context.Patients.Add(patient);
                    await _context.SaveChangesAsync();
                }
            }
        }
    }
}
