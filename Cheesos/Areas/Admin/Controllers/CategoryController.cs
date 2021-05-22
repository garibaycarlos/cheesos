using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cheesos.Data;
using Cheesos.Models;
using Cheesos.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cheesos.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.ManagerUser)]
    public class CategoryController : Controller
    {
        private readonly ApplicationDbContext _db;
        private const string StatusMessage = "Error: Category {0} already exists. Please use another name.";

        public CategoryController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET
        public async Task<IActionResult> Index()
        {
            return View(await _db.Category.ToListAsync());
        }

        // GET - Create
        public IActionResult Create()
        {
            return View();
        }

        // POST - Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Category category)
        {
            if (ModelState.IsValid)
            {
                var getCategories = _db.Category.Where(c => c.Name == category.Name);

                if (getCategories.Count() > 0)
                {
                    // display error
                    TempData["StatusMessage"] = string.Format(StatusMessage, category.Name);
                }
                else
                {
                    await _db.Category.AddAsync(category);

                    await _db.SaveChangesAsync();

                    // when we return to any view, we actually return to an Action method which will then call the view
                    return RedirectToAction(nameof(Index));
                }
            }

            return View();
        }

        // GET - Edit
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            Category getCategory = await _db.Category.FindAsync(id);

            if (getCategory == null) return NotFound();

            return View(getCategory);
        }

        // POST - Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Category category)
        {
            if (ModelState.IsValid)
            {
                var getCategories = _db.Category.Where(c => c.Name == category.Name && c.Id != category.Id);

                if (getCategories.Count() > 0)
                {
                    // display error
                    TempData["StatusMessage"] = string.Format(StatusMessage, category.Name);
                }
                else
                {
                    _db.Update(category); // we use this method because we do not need to update several fields (just one here)

                    await _db.SaveChangesAsync();

                    // when we return to any view, we actually return to an Action method which will then call the view
                    return RedirectToAction(nameof(Index));
                }
            }

            return View(category);
        }

        // GET - Details
        public async Task<IActionResult> Details(int id)
        {
            Category getCategory = await _db.Category.FindAsync(id);

            if (getCategory == null) return NotFound();

            return View(getCategory);
        }

        // GET - Delete
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            Category getCategory = await _db.Category.FindAsync(id);

            if (getCategory == null) return NotFound();

            return View(getCategory);
        }

        // POST - Delete
        [HttpPost, ActionName("Delete")] // we specify the action name to make sure it matches the asp-action="Delete" called from the view,
                                         // just in case we need to rename the action method due to existing action method names
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            Category getCategory = await _db.Category.FindAsync(id);

            if (getCategory == null) return NotFound();

            _db.Remove(getCategory);

            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}