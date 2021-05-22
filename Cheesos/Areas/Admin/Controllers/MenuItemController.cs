using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cheesos.Data;
using Cheesos.Models;
using Cheesos.Models.ViewModels;
using Cheesos.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Cheesos.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.ManagerUser)]
    public class MenuItemController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IWebHostEnvironment _hostEnvironment;

        [BindProperty]
        public MenuItemViewModel MenuItemVM { get; set; }

        public MenuItemController(ApplicationDbContext db, IWebHostEnvironment hostEnvironment)
        {
            _db = db;

            _hostEnvironment = hostEnvironment;

            MenuItemVM = new MenuItemViewModel
            {
                Category = _db.Category, // assign all the categories from the database
                MenuItem = new MenuItem()
            };
        }

        public async Task<IActionResult> Index()
        {
            var menuItems = await _db.MenuItem.Include(c => c.Category)
                                              .Include(s => s.SubCategory)
                                              .ToListAsync();

            return View(menuItems);
        }

        // GET - Create
        public IActionResult Create()
        {
            return View(MenuItemVM);
        }

        [HttpPost, ActionName("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePost()
        {
            MenuItemVM.MenuItem.SubCategoryId = Convert.ToInt32(Request.Form["SubCategoryId"].ToString());

            if (!ModelState.IsValid) return View(MenuItemVM);

            _db.MenuItem.Add(MenuItemVM.MenuItem);

            await _db.SaveChangesAsync();

            string webRootPath = _hostEnvironment.WebRootPath;
            var files = HttpContext.Request.Form.Files;

            var getMenuItem = await _db.MenuItem.FindAsync(MenuItemVM.MenuItem.Id);

            if (files.Count > 0)
            {
                // file has been uploaded
                string uploads = Path.Combine(webRootPath, "images");
                string extension = Path.GetExtension(files[0].FileName);

                using (var fileStream = new FileStream(Path.Combine(uploads, "menu-item-" + MenuItemVM.MenuItem.Id + extension), FileMode.Create))
                {
                    files[0].CopyTo(fileStream);
                }

                getMenuItem.Image = string.Concat(@"\images\menu-item-", MenuItemVM.MenuItem.Id, extension);
            }
            else
            {
                // no file was uploaded, so use default
                string uploads = Path.Combine(webRootPath, @"images\" + SD.DefaultFoodImage);

                System.IO.File.Copy(uploads, webRootPath + @"\images\menu-item-" + MenuItemVM.MenuItem.Id + ".png");

                getMenuItem.Image = string.Concat(@"\images\menu-item-", MenuItemVM.MenuItem.Id + ".png");
            }

            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET - Edit
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            MenuItemVM.MenuItem = await _db.MenuItem.Include(c => c.Category)
                                                    .Include(s => s.SubCategory)
                                                    .SingleOrDefaultAsync(m => m.Id == id);

            //MenuItemVM.SubCategory = await _db.SubCategory.Where(s => s.CategoryId == MenuItemVM.MenuItem.CategoryId).ToListAsync();

            if (MenuItemVM.MenuItem == null) return NotFound();

            return View(MenuItemVM);
        }

        [HttpPost, ActionName("Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPost(int? id)
        {
            if (id == null) return NotFound();

            MenuItemVM.MenuItem.SubCategoryId = Convert.ToInt32(Request.Form["SubCategoryId"].ToString());

            if (!ModelState.IsValid)
            {
                MenuItemVM.SubCategory = await _db.SubCategory.Where(s => s.CategoryId == MenuItemVM.MenuItem.CategoryId).ToListAsync();

                return View(MenuItemVM);
            }

            string webRootPath = _hostEnvironment.WebRootPath;
            var files = HttpContext.Request.Form.Files;

            var getMenuItem = await _db.MenuItem.FindAsync(MenuItemVM.MenuItem.Id);

            if (files.Count > 0)
            {
                // new image file has been uploaded
                string uploads = Path.Combine(webRootPath, "images");
                string extension_new = Path.GetExtension(files[0].FileName);

                // delete the original file
                var imagePath = Path.Combine(webRootPath, getMenuItem.Image.TrimStart('\\'));

                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }

                // we upload the new file
                using (var fileStream = new FileStream(Path.Combine(uploads, "menu-item-" + MenuItemVM.MenuItem.Id + extension_new), FileMode.Create))
                {
                    files[0].CopyTo(fileStream);
                }

                getMenuItem.Image = string.Concat(@"\images\menu-item-", MenuItemVM.MenuItem.Id, extension_new);
            }

            getMenuItem.Name = MenuItemVM.MenuItem.Name;
            getMenuItem.Description = MenuItemVM.MenuItem.Description;
            getMenuItem.Price = MenuItemVM.MenuItem.Price;
            getMenuItem.Spicyness = MenuItemVM.MenuItem.Spicyness;
            getMenuItem.CategoryId = MenuItemVM.MenuItem.CategoryId;
            getMenuItem.SubCategoryId = MenuItemVM.MenuItem.SubCategoryId;

            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET - Details
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            MenuItemVM.MenuItem = await _db.MenuItem.Include(c => c.Category)
                                                    .Include(s => s.SubCategory)
                                                    .SingleOrDefaultAsync(m => m.Id == id);

            if (MenuItemVM.MenuItem == null) return NotFound();

            return View(MenuItemVM);
        }

        // GET - Delete
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            MenuItemVM.MenuItem = await _db.MenuItem.Include(c => c.Category)
                                                    .Include(s => s.SubCategory)
                                                    .SingleOrDefaultAsync(m => m.Id == id);

            if (MenuItemVM.MenuItem == null) return NotFound();

            return View(MenuItemVM);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            MenuItem getMenuItem = await _db.MenuItem.FindAsync(id);

            if (getMenuItem == null) return NotFound();

            _db.Remove(getMenuItem);

            await _db.SaveChangesAsync();

            string webRootPath = _hostEnvironment.WebRootPath;
            var imagePath = Path.Combine(webRootPath, getMenuItem.Image.TrimStart('\\'));

            if (System.IO.File.Exists(imagePath))
            {
                System.IO.File.Delete(imagePath);
            }

            return RedirectToAction(nameof(Index));
        }
    }
}