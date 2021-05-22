using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Cheesos.Data;
using Cheesos.Models;
using Cheesos.Models.ViewModels;
using Cheesos.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Cheesos.Controllers
{
    [Area("Customer")]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _db;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext db)
        {
            _logger = logger;

            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            var indexVM = new IndexViewModel
            {
                MenuItem = await _db.MenuItem.Include(c => c.Category)
                                             .Include(s => s.SubCategory)
                                             .ToListAsync(),

                Category = await _db.Category.ToListAsync(),
                Coupon = await _db.Coupon.Where(c => c.IsActive).ToListAsync()
            };

            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

            if (claim != null)
            {
                int count = _db.ShoppingCart.Count(s => s.ApplicationUserId == claim.Value);

                HttpContext.Session.SetInt32(SD.ssShoppingCartCount, count);
            }

            return View(indexVM);
        }

        [Authorize]
        public async Task<IActionResult> Details(int id)
        {
            MenuItem getMenuItem = await _db.MenuItem.Include(c => c.Category)
                                                     .Include(s => s.SubCategory)
                                                     .FirstOrDefaultAsync(m => m.Id == id);

            var cartObj = new ShoppingCart
            {
                MenuItem = getMenuItem,
                MenuItemId = id
            };

            return View(cartObj);
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Details(ShoppingCart cartObj)
        {
            cartObj.Id = 0;

            if (ModelState.IsValid)
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);

                cartObj.ApplicationUserId = claim.Value;

                ShoppingCart getShoppingCart = await _db.ShoppingCart.FirstOrDefaultAsync(a => a.ApplicationUserId == cartObj.ApplicationUserId &&
                                                                                               a.MenuItemId == cartObj.MenuItemId);

                // we set this object to null to prevent errors when EF tries to insert category and subcategory
                // along with the cartObj since those objects carry values from the MenuItem property. Other way
                // to prevent such insert operation is to set the MenuItem field of the ShoppingCart to [NotMapped]
                cartObj.MenuItem = null;

                if (getShoppingCart == null) // item does not exist in the shopping cart
                {
                    await _db.ShoppingCart.AddAsync(cartObj);
                }
                else
                {
                    getShoppingCart.Count += cartObj.Count;
                }

                await _db.SaveChangesAsync();

                int countCartItems = _db.ShoppingCart.Where(a => a.ApplicationUserId == cartObj.ApplicationUserId).Count();

                HttpContext.Session.SetInt32("ssCount", countCartItems);

                return RedirectToAction(nameof(Index));
            }

            MenuItem getMenuItem = await _db.MenuItem.Include(c => c.Category)
                                         .Include(s => s.SubCategory)
                                         .FirstOrDefaultAsync(m => m.Id == cartObj.MenuItemId);

            var shoppingCart = new ShoppingCart
            {
                MenuItem = getMenuItem,
                MenuItemId = getMenuItem.Id
            };

            return View(shoppingCart);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}