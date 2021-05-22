using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Cheesos.Models;
using Cheesos.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cheesos.Data
{
    public class DbInitializer : IDbInitializer
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public DbInitializer(ApplicationDbContext db, UserManager<IdentityUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _db = db;

            _userManager = userManager;

            _roleManager = roleManager;
        }

        public async void Initialize()
        {
            try
            {
                if (_db.Database.GetPendingMigrations().Count() > 0)
                {
                    _db.Database.Migrate(); // apply any pending migration
                }
            }
            catch (Exception)
            {

            }

            // if there is a Manager user already, do nothing
            if (_db.Roles.Any(r => r.Name == SD.ManagerUser)) return;

            // create the roles
            _roleManager.CreateAsync(new IdentityRole(SD.ManagerUser)).GetAwaiter().GetResult();
            _roleManager.CreateAsync(new IdentityRole(SD.FrontDeskUser)).GetAwaiter().GetResult();
            _roleManager.CreateAsync(new IdentityRole(SD.KitchenUser)).GetAwaiter().GetResult();
            _roleManager.CreateAsync(new IdentityRole(SD.CustomerEndUser)).GetAwaiter().GetResult();

            string adminEmail = "admin@cheesos.com";

            // create default manager user
            _userManager.CreateAsync(new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                Name = "Administrator",
                EmailConfirmed = true,
                PhoneNumber = "1122334455"
            }, "Admin123*").GetAwaiter().GetResult();

            IdentityUser user = await _db.Users.FirstOrDefaultAsync(u => u.Email == adminEmail);

            await _userManager.AddToRoleAsync(user, SD.ManagerUser);
        }
    }
}