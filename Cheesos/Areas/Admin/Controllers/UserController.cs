using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cheesos.Data;
using Cheesos.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Cheesos.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.ManagerUser)]
    public class UserController : Controller
    {
        private readonly ApplicationDbContext _db;

        public UserController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var claimsIdentity = (ClaimsIdentity)this.User.Identity;
            Claim claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            // we fetch the id of the user that has logged in, then we display all users except the logged in user
            return View(await _db.ApplicationUser.Where(u => u.Id != claim.Value).ToListAsync());
        }

        public async Task<IActionResult> LockUnlock(string id, bool lockUser)
        {
            if (id == null) return NotFound();

            var applicationUser = await _db.ApplicationUser.FirstOrDefaultAsync(a => a.Id == id);

            if (applicationUser == null) return NotFound();

            DateTime lockTime = (lockUser ? DateTime.Now.AddYears(1000) : DateTime.Now);

            applicationUser.LockoutEnd = lockTime;

            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}