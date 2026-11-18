using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MediVerify.Models;
using MediVerify.Services;

namespace MediVerify.Data
{
    /// <summary>
    /// Idempotent demo data. Safe to run on every start: existing rows are detected and reused.
    /// </summary>
    public class DbInitializer
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ISafetyService _safety;

        public DbInitializer(ApplicationDbContext context,
                             UserManager<ApplicationUser> userManager,
                             RoleManager<IdentityRole> roleManager,
                             ISafetyService safety)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _safety = safety;
        }

        public async Task InitializeAsync()
        {
            _context.Database.SetCommandTimeout(180);
            await _context.Database.MigrateAsync();

            await SeedRolesAsync();
            var admin = await EnsureUserAsync("admin@mediverify.com", "System Administrator", "Admin#123", "Administrator");
            var demoDoctor = await EnsureUserAsync("doctor@mediverify.com", "Dr. John Doe", "Doctor#123", "Doctor");
            var demoPharmacist = await EnsureUserAsync("pharma@mediverify.com", "Jane Smith", "Pharma#123", "Pharmacist");
            var demoPatientUser = await EnsureUserAsync("patient@mediverify.com", "Alice Fernandes", "Patient#123", "Patient");

            var demoPatient = await EnsurePatientAsync(demoPatientUser, new DateTime(1991, 4, 12), "Female",
                "12 Marine Drive, Mumbai, Maharashtra", "+91 98200 11223");

            var doctors = new List<ApplicationUser> { demoDoctor };
            doctors.Add(await EnsureUserAsync("priya.sharma@mediverify.com", "Dr. Priya Sharma", "Doctor#123", "Doctor"));
            doctors.Add(await EnsureUserAsync("arjun.mehta@mediverify.com", "Dr. Arjun Mehta", "Doctor#123", "Doctor"));
            doctors.Add(await EnsureUserAsync("neha.iyer@mediverify.com", "Dr. Neha Iyer", "Doctor#123", "Doctor"));
            doctors.Add(await EnsureUserAsync("rakesh.verma@mediverify.com", "Dr. Rakesh Verma", "Doctor#123", "Doctor"));

            var pharmacists = new List<ApplicationUser> { demoPharmacist };
            pharmacists.Add(await EnsureUserAsync("rahul.nair@mediverify.com", "Rahul Nair", "Pharma#123", "Pharmacist"));
            pharmacists.Add(await EnsureUserAsync("sneha.kulkarni@mediverify.com", "Sneha Kulkarni", "Pharma#123", "Pharmacist"));
            pharmacists.Add(await EnsureUserAsync("imran.qureshi@mediverify.com", "Imran Qureshi", "Pharma#123", "Pharmacist"));

            var patientSeeds = new (string Email, string Name, DateTime Dob, string Gender, string City, string Phone)[]
            {
                ("patient1@mediverify.com", "Aarav Gupta", new DateTime(1985, 2, 18), "Male", "New Delhi", "+91 98110 22001"),
                ("patient2@mediverify.com", "Ananya Reddy", new DateTime(1998, 7, 5), "Female", "Hyderabad", "+91 98490 22002"),
                ("patient3@mediverify.com", "Vikram Singh", new DateTime(1972, 11, 23), "Male", "Jaipur", "+91 98290 22003"),
                ("patient4@mediverify.com", "Meera Nair", new DateTime(1990, 3, 14), "Female", "Kochi", "+91 98950 22004"),
                ("patient5@mediverify.com", "Rohan Desai", new DateTime(2001, 9, 30), "Male", "Ahmedabad", "+91 98790 22005"),
                ("patient6@mediverify.com", "Kavya Menon", new DateTime(1995, 5, 9), "Female", "Chennai", "+91 98400 22006"),
                ("patient7@mediverify.com", "Aditya Joshi", new DateTime(1968, 1, 27), "Male", "Pune", "+91 98220 22007"),
                ("patient8@mediverify.com", "Pooja Bhatt", new DateTime(1988, 6, 21), "Female", "Surat", "+91 98250 22008"),
                ("patient9@mediverify.com", "Sanjay Patil", new DateTime(1979, 12, 3), "Male", "Nagpur", "+91 98230 22009"),
                ("patient10@mediverify.com", "Divya Rao", new DateTime(1993, 8, 17), "Female", "Bengaluru", "+91 98860 22010"),
                ("patient11@mediverify.com", "Karan Malhotra", new DateTime(1983, 4, 8), "Male", "Chandigarh", "+91 98150 22011"),
                ("patient12@mediverify.com", "Ishita Sen", new DateTime(2004, 10, 11), "Female", "Kolkata", "+91 98300 22012"),
                ("patient13@mediverify.com", "Manish Yadav", new DateTime(1975, 7, 24), "Male", "Lucknow", "+91 94150 22013"),
                ("patient14@mediverify.com", "Ritu Agarwal", new DateTime(1986, 2, 2), "Female", "Indore", "+91 98260 22014"),
                ("patient15@mediverify.com", "Nikhil Chavan", new DateTime(1997, 11, 29), "Male", "Nashik", "+91 98220 22015"),
            };

            var patients = new List<Patient> { demoPatient };
            var patientsByEmail = new Dictionary<string, Patient>(StringComparer.OrdinalIgnoreCase)
            {
                [demoPatientUser.Email] = demoPatient
            };
            foreach (var seed in patientSeeds)
            {
                var user = await EnsureUserAsync(seed.Email, seed.Name, "Patient#123", "Patient");
                var patient = await EnsurePatientAsync(user, seed.Dob, seed.Gender,
                    $"{seed.City}, India", seed.Phone);
                patients.Add(patient);
                patientsByEmail[seed.Email] = patient;
            }

            var medicines = await SeedMedicinesAsync();
            await SeedInventoryAsync(medicines);
            await SeedDrugInteractionsAsync(medicines);

            if (!await _context.Prescriptions.AnyAsync())
            {
                await SeedPrescriptionsAsync(doctors, pharmacists, patients, patientsByEmail, demoPatient, demoDoctor, medicines);
            }

            if (!await _context.Notifications.AnyAsync())
            {
                await SeedNotificationsAsync(demoDoctor, demoPharmacist, demoPatientUser);
            }

            if (!await _context.AuditLogs.AnyAsync())
            {
                await SeedAuditAsync();
            }
        }

        // ---------------------------------------------------------------- users

        private async Task SeedRolesAsync()
        {
            foreach (var roleName in new[] { "Administrator", "Doctor", "Pharmacist", "Patient" })
            {
                if (!await _roleManager.RoleExistsAsync(roleName))
                    await _roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        private async Task<ApplicationUser> EnsureUserAsync(string email, string fullName, string password, string role)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    FullName = fullName,
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddMonths(-6)
                };
                var result = await _userManager.CreateAsync(user, password);
                if (!result.Succeeded)
                {
                    throw new InvalidOperationException(
                        $"Could not seed demo user {email}: {string.Join("; ", result.Errors.Select(e => e.Description))}");
                }
            }

            if (!await _userManager.IsInRoleAsync(user, role))
                await _userManager.AddToRoleAsync(user, role);

            // Keep demo accounts looking right even if they were registered earlier
            // with a placeholder display name.
            if (!string.Equals(user.FullName, fullName, StringComparison.Ordinal) || !user.IsActive)
            {
                user.FullName = fullName;
                user.IsActive = true;
                await _userManager.UpdateAsync(user);
            }

            return user;
        }

        private async Task<Patient> EnsurePatientAsync(ApplicationUser user, DateTime dob, string gender, string address, string emergency)
        {
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.ApplicationUserId == user.Id);
            if (patient == null)
            {
                patient = new Patient
                {
                    ApplicationUserId = user.Id,
                    DateOfBirth = dob,
                    Gender = gender,
                    Address = address,
                    EmergencyContact = emergency,
                    CreatedAt = DateTime.UtcNow.AddMonths(-6)
                };
                _context.Patients.Add(patient);
                await _context.SaveChangesAsync();
            }
            return patient;
        }

        // ------------------------------------------------------------ medicines

        private async Task<List<Medicine>> SeedMedicinesAsync()
        {
            var existing = await _context.Medicines.ToListAsync();
            if (existing.Count >= 60) return existing;

            var catalogue = new (string Name, string Generic, string Manufacturer, string Category, string Description)[]
            {
                // Analgesic / antipyretic
                ("Crocin Advance 500 mg", "Paracetamol", "GSK", "Analgesic", "Fever and mild to moderate pain relief."),
                ("Combiflam", "Ibuprofen + Paracetamol", "Sanofi", "Analgesic", "Anti-inflammatory combination for pain and fever."),
                ("Brufen 400 mg", "Ibuprofen", "Abbott", "Analgesic", "NSAID for pain and inflammation."),
                ("Disprin", "Aspirin", "Reckitt", "Analgesic", "Fast relief of headache and fever."),
                ("Nimulid 100 mg", "Nimesulide", "Panacea", "Analgesic", "Short-term relief of acute pain."),
                ("Zerodol-SP", "Aceclofenac + Paracetamol + Serratiopeptidase", "Ipca", "Analgesic", "Pain and swelling after injury or surgery."),
                // Antibiotic
                ("Augmentin 625 Duo", "Amoxicillin + Clavulanic acid", "GSK", "Antibiotic", "Broad-spectrum antibiotic for respiratory and skin infections."),
                ("Azithral 500", "Azithromycin", "Alembic", "Antibiotic", "Macrolide antibiotic, 3-5 day course."),
                ("Taxim-O 200", "Cefixime", "Alkem", "Antibiotic", "Third-generation cephalosporin for bacterial infections."),
                ("Moxikind-CV 625", "Amoxicillin + Clavulanic acid", "Mankind", "Antibiotic", "Broad-spectrum antibiotic."),
                ("Cifran 500", "Ciprofloxacin", "Sun Pharma", "Antibiotic", "Fluoroquinolone for urinary and GI infections."),
                ("Doxy-1 L-DR", "Doxycycline", "USV", "Antibiotic", "Tetracycline antibiotic for acne and atypical infections."),
                // Antidiabetic
                ("Glycomet 500", "Metformin", "USV", "Antidiabetic", "First-line therapy for type 2 diabetes."),
                ("Januvia 100", "Sitagliptin", "MSD", "Antidiabetic", "DPP-4 inhibitor for glycaemic control."),
                ("Galvus 50", "Vildagliptin", "Novartis", "Antidiabetic", "DPP-4 inhibitor used with diet and exercise."),
                ("Glimestar-M1", "Glimepiride + Metformin", "Mankind", "Antidiabetic", "Combination therapy for type 2 diabetes."),
                ("Janumet 50/500", "Sitagliptin + Metformin", "MSD", "Antidiabetic", "Dual therapy for type 2 diabetes."),
                ("Amaryl 2 mg", "Glimepiride", "Sanofi", "Antidiabetic", "Sulfonylurea to stimulate insulin release."),
                // Antihypertensive / cardiovascular
                ("Telma 40", "Telmisartan", "Glenmark", "Antihypertensive", "Angiotensin receptor blocker for hypertension."),
                ("Amlopres 5", "Amlodipine", "Cipla", "Antihypertensive", "Calcium channel blocker for hypertension."),
                ("Metolar XR 50", "Metoprolol", "Cipla", "Antihypertensive", "Beta blocker for hypertension and angina."),
                ("Cardace 5", "Ramipril", "Sanofi", "Antihypertensive", "ACE inhibitor for hypertension and heart failure."),
                ("Ecosprin 75", "Aspirin", "USV", "Cardiovascular", "Low-dose antiplatelet for cardiovascular protection."),
                ("Atorva 10", "Atorvastatin", "Zydus", "Cardiovascular", "Statin for cholesterol management."),
                ("Rosuvas 10", "Rosuvastatin", "Sun Pharma", "Cardiovascular", "Statin for dyslipidaemia."),
                ("Clopilet 75", "Clopidogrel", "Sun Pharma", "Cardiovascular", "Antiplatelet after stenting or stroke."),
                ("Lasix 40", "Furosemide", "Sanofi", "Cardiovascular", "Loop diuretic for oedema and heart failure."),
                ("Aldactone 25", "Spironolactone", "RPG", "Cardiovascular", "Potassium-sparing diuretic."),
                ("Warfarin 5 mg", "Warfarin", "Cipla", "Cardiovascular", "Oral anticoagulant requiring INR monitoring."),
                // Antihistamine
                ("Allegra 120", "Fexofenadine", "Sanofi", "Antihistamine", "Non-sedating antihistamine for allergies."),
                ("Avil 25", "Pheniramine", "Sanofi", "Antihistamine", "Antihistamine for allergic reactions."),
                ("Levocet 5", "Levocetirizine", "Cipla", "Antihistamine", "Antihistamine for allergic rhinitis and urticaria."),
                ("Montair-LC", "Montelukast + Levocetirizine", "Cipla", "Antihistamine", "Allergic rhinitis with mild asthma."),
                ("Cetirizine 10 mg", "Cetirizine", "Dr Reddy's", "Antihistamine", "Antihistamine for hay fever and hives."),
                ("Alerid 10", "Cetirizine", "Cipla", "Antihistamine", "Antihistamine for allergic symptoms."),
                // Antacid / GI
                ("Pan 40", "Pantoprazole", "Alkem", "Antacid", "Proton pump inhibitor for acidity and reflux."),
                ("Rantac 150", "Ranitidine", "JB Chemicals", "Antacid", "H2 blocker for acid-related disorders."),
                ("Digene Gel", "Antacid combination", "Abbott", "Antacid", "Antacid for heartburn and indigestion."),
                ("Cremaffin Plus", "Lactulose + Milk of magnesia", "Abbott", "Antacid", "Laxative for constipation."),
                ("Ondem MD 4", "Ondansetron", "Alkem", "Antacid", "Anti-emetic for nausea and vomiting."),
                ("Domstal 10", "Domperidone", "Torrent", "Antacid", "Prokinetic for nausea and bloating."),
                // Antifungal
                ("Forcan 150", "Fluconazole", "Cipla", "Antifungal", "Systemic antifungal for candidiasis."),
                ("Itraconazole 100", "Itraconazole", "Janssen", "Antifungal", "Antifungal for dermatophyte infections."),
                ("Terbinafine 250", "Terbinafine", "Sun Pharma", "Antifungal", "Antifungal for nail and skin infections."),
                ("Clotrimazole 1% Cream", "Clotrimazole", "Bayer", "Antifungal", "Topical antifungal for skin infections."),
                ("Candid-B Cream", "Clotrimazole + Beclometasone", "Glenmark", "Antifungal", "Topical antifungal with mild steroid."),
                // Antiviral
                ("Acyclovir 400", "Acyclovir", "Cipla", "Antiviral", "Antiviral for herpes infections."),
                ("Oseltamivir 75", "Oseltamivir", "Roche", "Antiviral", "Antiviral for influenza."),
                ("Valacyclovir 500", "Valacyclovir", "GSK", "Antiviral", "Antiviral for herpes zoster and HSV."),
                ("Favipiravir 200", "Favipiravir", "Glenmark", "Antiviral", "Antiviral used in viral infections."),
                // Respiratory
                ("Asthalin Inhaler", "Salbutamol", "Cipla", "Respiratory", "Bronchodilator for asthma relief."),
                ("Budecort 100 Inhaler", "Budesonide", "Cipla", "Respiratory", "Inhaled steroid for asthma control."),
                ("Montair 10", "Montelukast", "Cipla", "Respiratory", "Leukotriene antagonist for asthma and rhinitis."),
                ("Ascoril LS", "Bromhexine + Ambroxol + Levosalbutamol", "Glenmark", "Respiratory", "Cough syrup for productive cough."),
                ("Deriphyllin Retard", "Etophylline + Theophylline", "Zydus", "Respiratory", "Bronchodilator for bronchospasm."),
                ("Foracort 200 Rotacap", "Formoterol + Budesonide", "Cipla", "Respiratory", "Maintenance therapy for asthma."),
                // Vitamin / supplement
                ("Shelcal 500", "Calcium + Vitamin D3", "Torrent", "Vitamin", "Calcium and vitamin D supplementation."),
                ("Becosules", "B-complex + Vitamin C", "Pfizer", "Vitamin", "B-vitamin supplement."),
                ("Zincovit", "Multivitamin + Zinc", "Apex", "Vitamin", "Daily multivitamin and mineral supplement."),
                ("Livogen", "Ferrous fumarate + Folic acid", "Merck", "Vitamin", "Iron and folate for anaemia."),
                ("Uprise-D3 60K", "Cholecalciferol", "Alkem", "Vitamin", "Weekly vitamin D3 sachet."),
                ("Neurobion Forte", "Vitamin B complex", "Merck", "Vitamin", "B-vitamins for neuropathy support."),
                // Dermatology
                ("Soframycin Cream", "Framycetin", "Sanofi", "Dermatology", "Topical antibiotic for minor skin infections."),
                ("Betnovate-C", "Betamethasone + Clioquinol", "GSK", "Dermatology", "Topical steroid for inflamed skin conditions."),
                ("Tretinoin 0.05% Cream", "Tretinoin", "Janssen", "Dermatology", "Topical retinoid for acne."),
                ("Moisturex Cream", "Urea + Lactic acid", "Glenmark", "Dermatology", "Moisturiser for dry and scaly skin."),
                ("Caladryl Lotion", "Calamine + Pramoxine", "Piramal", "Dermatology", "Soothing lotion for itching and rashes."),
            };

            var toAdd = new List<Medicine>();
            var known = existing.Select(m => m.MedicineName).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var c in catalogue)
            {
                if (known.Contains(c.Name)) continue;
                toAdd.Add(new Medicine
                {
                    MedicineName = c.Name,
                    GenericName = c.Generic,
                    Manufacturer = c.Manufacturer,
                    Category = c.Category,
                    Description = c.Description,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow.AddMonths(-5)
                });
            }
            if (toAdd.Count > 0)
            {
                _context.Medicines.AddRange(toAdd);
                await _context.SaveChangesAsync();
            }

            return await _context.Medicines.ToListAsync();
        }

        // ------------------------------------------------------------ inventory

        private async Task SeedInventoryAsync(List<Medicine> medicines)
        {
            if (await _context.Inventories.AnyAsync()) return;
            if (medicines.Count == 0) return;

            var rnd = new Random(20261118);
            var today = DateTime.UtcNow.Date;
            var rows = new List<Inventory>();

            for (var i = 0; i < medicines.Count; i++)
            {
                var medicine = medicines[i];
                // Primary batch: deliberate mix of healthy, low, out of stock and expired.
                decimal qty;
                DateTime expiry;
                if (i % 13 == 0)
                {
                    qty = 0m;                                            // out of stock
                    expiry = today.AddDays(rnd.Next(120, 500));
                }
                else if (i % 11 == 0)
                {
                    qty = rnd.Next(5, 40);                               // expired but still on shelf
                    expiry = today.AddDays(-rnd.Next(10, 90));
                }
                else if (i % 7 == 0)
                {
                    qty = rnd.Next(8, 45);                               // low stock
                    expiry = today.AddDays(rnd.Next(20, 240));
                }
                else
                {
                    qty = rnd.Next(120, 900);                            // healthy
                    expiry = today.AddDays(rnd.Next(180, 900));
                }

                rows.Add(new Inventory
                {
                    MedicineId = medicine.MedicineId,
                    AvailableQuantity = qty,
                    ReorderLevel = 50m,
                    BatchNumber = $"B{2026}{(i + 1):D4}",
                    ExpiryDate = expiry,
                    LastUpdated = DateTime.UtcNow.AddDays(-rnd.Next(1, 20))
                });

                // Secondary batch for roughly half the catalogue.
                if (i % 2 == 0)
                {
                    rows.Add(new Inventory
                    {
                        MedicineId = medicine.MedicineId,
                        AvailableQuantity = rnd.Next(60, 600),
                        ReorderLevel = 50m,
                        BatchNumber = $"B{2026}{(i + 1):D4}A",
                        ExpiryDate = today.AddDays(rnd.Next(240, 1000)),
                        LastUpdated = DateTime.UtcNow.AddDays(-rnd.Next(1, 30))
                    });
                }
            }

            _context.Inventories.AddRange(rows);
            await _context.SaveChangesAsync();
        }

        // ------------------------------------------------------ drug interactions

        private async Task SeedDrugInteractionsAsync(List<Medicine> medicines)
        {
            if (await _context.DrugInteractions.AnyAsync()) return;

            var byName = medicines.ToDictionary(m => m.MedicineName, m => m.MedicineId, StringComparer.OrdinalIgnoreCase);
            var pairs = new (string A, string B, string Severity, string Description)[]
            {
                ("Warfarin 5 mg", "Ecosprin 75", "High", "Additive bleeding risk: anticoagulant plus antiplatelet therapy."),
                ("Warfarin 5 mg", "Brufen 400 mg", "High", "NSAIDs increase bleeding risk and GI ulceration with warfarin."),
                ("Warfarin 5 mg", "Forcan 150", "High", "Fluconazole inhibits warfarin metabolism and raises INR."),
                ("Warfarin 5 mg", "Augmentin 625 Duo", "Moderate", "Antibiotic disruption of gut flora can increase INR."),
                ("Warfarin 5 mg", "Disprin", "High", "Avoid concurrent aspirin and warfarin without specialist review."),
                ("Ecosprin 75", "Brufen 400 mg", "High", "Combined antiplatelet and NSAID therapy increases GI bleeding risk."),
                ("Ecosprin 75", "Nimulid 100 mg", "High", "NSAID plus aspirin raises the risk of peptic ulcer bleeding."),
                ("Clopilet 75", "Pan 40", "Moderate", "Proton pump inhibitors may reduce clopidogrel activation."),
                ("Atorva 10", "Forcan 150", "Moderate", "Azole antifungals raise statin exposure and myopathy risk."),
                ("Rosuvas 10", "Forcan 150", "Moderate", "Monitor for muscle pain when combining statin and azole."),
                ("Azithral 500", "Montair-LC", "Moderate", "Both agents can prolong the QT interval."),
                ("Azithral 500", "Forcan 150", "High", "Additive QT prolongation; consider an alternative antibiotic."),
                ("Cifran 500", "Shelcal 500", "Moderate", "Fluoroquinolones chelate with calcium and lose absorption."),
                ("Cifran 500", "Zincovit", "Low", "Zinc salts reduce ciprofloxacin absorption; separate doses."),
                ("Doxy-1 L-DR", "Shelcal 500", "Moderate", "Tetracyclines bind calcium and become less effective."),
                ("Doxy-1 L-DR", "Zincovit", "Low", "Divalent cations reduce doxycycline absorption."),
                ("Rantac 150", "Forcan 150", "Low", "Raised gastric pH may alter azole absorption."),
                ("Allegra 120", "Forcan 150", "Moderate", "Possible additive QT prolongation."),
                ("Asthalin Inhaler", "Metolar XR 50", "Moderate", "Beta blockers can antagonise bronchodilator effect in asthma."),
                ("Deriphyllin Retard", "Asthalin Inhaler", "Low", "Additive cardiac stimulation; monitor heart rate."),
                ("Glycomet 500", "Lasix 40", "Moderate", "Diuretic-induced dehydration increases metformin-related risk."),
                ("Glycomet 500", "Januvia 100", "Low", "Additive glucose lowering; monitor for hypoglycaemia."),
                ("Amaryl 2 mg", "Metolar XR 50", "Moderate", "Beta blockers may mask hypoglycaemia symptoms."),
                ("Oseltamivir 75", "Pan 40", "Low", "Reduced gastric acidity may slightly lower oseltamivir absorption."),
                ("Valacyclovir 500", "Cifran 500", "Moderate", "Both can affect renal handling; monitor renal function."),
                ("Tretinoin 0.05% Cream", "Betnovate-C", "Low", "Combined topical retinoid and steroid can irritate skin."),
                ("Amlopres 5", "Atorva 10", "Low", "Widely used combination; monitor blood pressure and lipids."),
                ("Cardace 5", "Aldactone 25", "High", "ACE inhibitor plus potassium-sparing diuretic can cause hyperkalaemia."),
                ("Lasix 40", "Shelcal 500", "Low", "Loop diuretics increase calcium excretion."),
            };

            var list = new List<DrugInteraction>();
            foreach (var p in pairs)
            {
                if (!byName.TryGetValue(p.A, out var a) || !byName.TryGetValue(p.B, out var b)) continue;
                list.Add(new DrugInteraction
                {
                    MedicineAId = a,
                    MedicineBId = b,
                    Severity = p.Severity,
                    Description = p.Description
                });
            }

            if (list.Count > 0)
            {
                _context.DrugInteractions.AddRange(list);
                await _context.SaveChangesAsync();
            }
        }

        // ---------------------------------------------------------- prescriptions

        private async Task SeedPrescriptionsAsync(List<ApplicationUser> doctors, List<ApplicationUser> pharmacists,
            List<Patient> patients, Dictionary<string, Patient> patientsByEmail, Patient demoPatient,
            ApplicationUser demoDoctor, List<Medicine> medicines)
        {
            var rnd = new Random(424242);
            var today = DateTime.UtcNow.Date;
            var statusPool = new[]
            {
                "Draft", "Draft",
                "Submitted", "Submitted", "Submitted",
                "Verified", "Verified",
                "Dispensed", "Dispensed", "Dispensed", "Dispensed",
                "Rejected"
            };

            var activeMedicines = medicines.Where(m => m.IsActive).ToList();
            if (activeMedicines.Count == 0) return;

            var created = new List<Prescription>();
            var itemsByPrescription = new Dictionary<Prescription, List<PrescriptionItem>>();

            for (var i = 0; i < 48; i++)
            {
                var doctor = i % 4 == 0 ? demoDoctor : doctors[rnd.Next(doctors.Count)];
                var patient = i % 3 == 0 ? demoPatient : patients[rnd.Next(patients.Count)];
                var createdDate = today.AddDays(-rnd.Next(0, 30)).AddHours(rnd.Next(8, 20)).AddMinutes(rnd.Next(0, 60));
                var status = statusPool[rnd.Next(statusPool.Length)];

                // Guarantee demo doctor has drafts and the demo patient has dispensed history.
                if (i == 0) { doctor = demoDoctor; status = "Draft"; patient = demoPatient; }
                if (i == 1) { doctor = demoDoctor; status = "Submitted"; patient = demoPatient; }
                if (i == 2) { doctor = demoDoctor; status = "Dispensed"; patient = demoPatient; }

                var itemCount = rnd.Next(1, 4);
                var picks = activeMedicines.OrderBy(_ => rnd.Next()).Take(itemCount).ToList();

                var prescription = new Prescription
                {
                    PatientId = patient.PatientId,
                    DoctorId = doctor.Id,
                    PrescriptionNumber = $"RX{createdDate:yyyyMMdd}-{(i + 1):D4}",
                    PrescriptionDate = createdDate,
                    ValidUntil = createdDate.AddDays(30),
                    Notes = rnd.Next(0, 4) == 0 ? "Review after one week if symptoms persist." : null,
                    Status = status,
                    CreatedAt = createdDate,
                    UpdatedAt = status == "Draft" ? null : createdDate.AddHours(rnd.Next(1, 6))
                };

                var items = new List<PrescriptionItem>();
                foreach (var medicine in picks)
                {
                    items.Add(new PrescriptionItem
                    {
                        MedicineId = medicine.MedicineId,
                        Quantity = rnd.Next(1, 4) * 10,
                        Duration = rnd.Next(3, 15),
                        PrescribedInstructions = new[]
                        {
                            "After meals", "Before breakfast", "At bedtime", "Twice daily", "With warm water"
                        }[rnd.Next(5)],
                        Notes = null
                    });
                }
                prescription.Items = items;

                var pharmacist = pharmacists[rnd.Next(pharmacists.Count)];
                switch (status)
                {
                    case "Verified":
                        prescription.IsVerified = true;
                        prescription.ClinicalReviewStatus = "Approved";
                        prescription.VerifiedBy = pharmacist.Id;
                        prescription.VerifiedAt = createdDate.AddHours(2);
                        break;
                    case "Rejected":
                        prescription.IsVerified = false;
                        prescription.ClinicalReviewStatus = "Rejected";
                        prescription.VerifiedBy = pharmacist.Id;
                        prescription.VerifiedAt = createdDate.AddHours(2);
                        prescription.RejectionReason = "Dose exceeds the recommended range for this patient. Please review and resubmit.";
                        break;
                    case "Dispensed":
                        prescription.IsVerified = true;
                        prescription.ClinicalReviewStatus = "Approved";
                        prescription.VerifiedBy = pharmacist.Id;
                        prescription.VerifiedAt = createdDate.AddHours(2);
                        prescription.DispensedAt = createdDate.AddHours(3);
                        break;
                }

                // Expired prescription sample so the "Expired Prescription" alert can fire.
                if (i == 5)
                {
                    prescription.ValidUntil = today.AddDays(-7);
                    prescription.Status = "Submitted";
                    prescription.IsVerified = false;
                    prescription.VerifiedBy = null;
                    prescription.VerifiedAt = null;
                    prescription.DispensedAt = null;
                }

                created.Add(prescription);
                itemsByPrescription[prescription] = items;
            }

            _context.Prescriptions.AddRange(created);
            await _context.SaveChangesAsync();

            // Dispensing records for dispensed prescriptions.
            var records = new List<DispensingRecord>();
            var dispensed = created.Where(p => p.Status == "Dispensed").ToList();
            var rnd2 = new Random(777);
            foreach (var rx in dispensed)
            {
                var pharmacist = pharmacists[rnd2.Next(pharmacists.Count)];
                var record = new DispensingRecord
                {
                    PrescriptionId = rx.PrescriptionId,
                    PharmacistId = pharmacist.Id,
                    DispensedAt = rx.DispensedAt ?? rx.CreatedAt.AddHours(3),
                    Items = itemsByPrescription[rx].Select(i => new DispensedItem
                    {
                        MedicineId = i.MedicineId,
                        Quantity = i.Quantity
                    }).ToList()
                };
                records.Add(record);
            }
            if (records.Count > 0)
            {
                _context.DispensingRecords.AddRange(records);
                await _context.SaveChangesAsync();
            }

            // Curated interaction prescriptions so the safety dashboard has real content.
            var byName = medicines.ToDictionary(m => m.MedicineName, m => m, StringComparer.OrdinalIgnoreCase);
            var curated = new (string[] Medicines, string Doctor, string Patient)[]
            {
                (new[] { "Warfarin 5 mg", "Ecosprin 75" }, "doctor@mediverify.com", "patient@mediverify.com"),
                (new[] { "Azithral 500", "Forcan 150" }, "priya.sharma@mediverify.com", "patient2@mediverify.com"),
                (new[] { "Cardace 5", "Aldactone 25" }, "arjun.mehta@mediverify.com", "patient7@mediverify.com")
            };

            var extra = new List<Prescription>();
            foreach (var c in curated)
            {
                var meds = c.Medicines.Where(n => byName.ContainsKey(n)).Select(n => byName[n]).ToList();
                if (meds.Count < 2) continue;
                var doctor = doctors.FirstOrDefault(d => d.Email.Equals(c.Doctor, StringComparison.OrdinalIgnoreCase)) ?? demoDoctor;
                var patient = patientsByEmail.TryGetValue(c.Patient, out var seededPatient) ? seededPatient : demoPatient;
                var createdDate = today.AddHours(-rnd.Next(2, 40));
                extra.Add(new Prescription
                {
                    PatientId = patient.PatientId,
                    DoctorId = doctor.Id,
                    PrescriptionNumber = $"RX{createdDate:yyyyMMdd}-C{extra.Count + 1:D2}",
                    PrescriptionDate = createdDate,
                    ValidUntil = createdDate.AddDays(30),
                    Status = "Submitted",
                    CreatedAt = createdDate,
                    UpdatedAt = createdDate.AddMinutes(30),
                    Notes = "Please review the flagged interaction before dispensing.",
                    Items = meds.Select(m => new PrescriptionItem
                    {
                        MedicineId = m.MedicineId,
                        Quantity = 20,
                        Duration = 7,
                        PrescribedInstructions = "After meals"
                    }).ToList()
                });
            }
            if (extra.Count > 0)
            {
                _context.Prescriptions.AddRange(extra);
                await _context.SaveChangesAsync();
            }

            // Run the real safety engine so alerts reflect production logic.
            var toReview = created.Concat(extra)
                .Where(p => p.Status == "Submitted" || p.Status == "Verified")
                .ToList();
            var reviewer = pharmacists.First().Id;
            foreach (var rx in toReview)
            {
                await _safety.EvaluateAsync(rx.PrescriptionId, reviewer);
            }

            // Seed a couple of resolved alerts so filters and badges are exercised.
            var pending = await _context.SafetyAlerts
                .Where(a => a.ReviewStatus == "Pending")
                .OrderBy(a => a.SafetyAlertId)
                .Take(3)
                .ToListAsync();
            if (pending.Count > 0)
            {
                pending[0].ReviewStatus = "Acknowledged";
                pending[0].ReviewedBy = reviewer;
                pending[0].ReviewedAt = DateTime.UtcNow.AddDays(-1);
                if (pending.Count > 1)
                {
                    pending[1].ReviewStatus = "Resolved";
                    pending[1].ReviewedBy = reviewer;
                    pending[1].ReviewedAt = DateTime.UtcNow.AddDays(-2);
                }
                await _context.SaveChangesAsync();
            }

            // Audit entries for the seeded prescriptions.
            var logs = new List<AuditLog>();
            foreach (var rx in created.OrderByDescending(p => p.CreatedAt).Take(16))
            {
                logs.Add(new AuditLog
                {
                    UserId = rx.DoctorId,
                    Action = rx.Status == "Draft" ? "CreatePrescription" : "SubmitPrescription",
                    EntityName = "Prescription",
                    EntityId = rx.PrescriptionId,
                    Timestamp = rx.CreatedAt,
                    IPAddress = "127.0.0.1",
                    Details = rx.PrescriptionNumber
                });
            }
            foreach (var rx in created.Where(p => p.Status == "Rejected").Take(3))
            {
                logs.Add(new AuditLog
                {
                    UserId = rx.VerifiedBy ?? demoDoctor.Id,
                    Action = "RejectPrescription",
                    EntityName = "Prescription",
                    EntityId = rx.PrescriptionId,
                    Timestamp = rx.VerifiedAt ?? rx.UpdatedAt ?? rx.CreatedAt,
                    IPAddress = "127.0.0.1",
                    Details = rx.RejectionReason
                });
            }
            _context.AuditLogs.AddRange(logs);
            await _context.SaveChangesAsync();
        }

        // --------------------------------------------------------- notifications

        private async Task SeedNotificationsAsync(ApplicationUser doctor, ApplicationUser pharmacist, ApplicationUser patient)
        {
            var now = DateTime.UtcNow;
            _context.Notifications.AddRange(
                new Notification
                {
                    UserId = pharmacist.Id,
                    Title = "Verification queue",
                    Message = "New prescriptions are waiting for a pharmacist review.",
                    Link = "/Prescription/Queue",
                    IsRead = false,
                    CreatedAt = now.AddMinutes(-25)
                },
                new Notification
                {
                    UserId = pharmacist.Id,
                    Title = "Low stock warning",
                    Message = "Several batches are below their reorder level.",
                    Link = "/Inventory?status=low",
                    IsRead = false,
                    CreatedAt = now.AddHours(-3)
                },
                new Notification
                {
                    UserId = doctor.Id,
                    Title = "Prescription verified",
                    Message = "A prescription you submitted has been verified and is ready to dispense.",
                    Link = "/Prescription",
                    IsRead = false,
                    CreatedAt = now.AddHours(-1)
                },
                new Notification
                {
                    UserId = patient.Id,
                    Title = "Your medicine is ready",
                    Message = "A prescription has been dispensed. Open it to view your receipt.",
                    Link = "/Prescription",
                    IsRead = false,
                    CreatedAt = now.AddMinutes(-50)
                },
                new Notification
                {
                    UserId = patient.Id,
                    Title = "Welcome to MediVerify",
                    Message = "You can track every prescription and download receipts here.",
                    Link = "/Dashboard",
                    IsRead = true,
                    CreatedAt = now.AddDays(-5)
                });
            await _context.SaveChangesAsync();
        }

        private async Task SeedAuditAsync()
        {
            var admin = await _userManager.FindByEmailAsync("admin@mediverify.com");
            if (admin == null) return;
            var now = DateTime.UtcNow;
            _context.AuditLogs.AddRange(
                new AuditLog { UserId = admin.Id, Action = "SeedDemoData", EntityName = "System", Timestamp = now.AddMinutes(-40), IPAddress = "127.0.0.1", Details = "Demo catalogue and workflow data initialised." },
                new AuditLog { UserId = admin.Id, Action = "ReviewInventory", EntityName = "Inventory", Timestamp = now.AddHours(-5), IPAddress = "127.0.0.1", Details = "Reviewed low stock batches." });
            await _context.SaveChangesAsync();
        }
    }
}
