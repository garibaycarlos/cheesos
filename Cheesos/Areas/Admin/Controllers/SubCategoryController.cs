using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Cheesos.Data;
using Cheesos.Models;
using Cheesos.Models.ViewModels;
using Cheesos.Utility;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cheesos.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = SD.ManagerUser)]
    public class SubCategoryController : Controller
    {
        private readonly ApplicationDbContext _db;

        [TempData]
        public string StatusMessage { get; set; }

        public SubCategoryController(ApplicationDbContext db)
        {
            _db = db;
        }

        // GET - Index
        public async Task<IActionResult> Index()
        {
            List<SubCategory> getSubCategories = await _db.SubCategory.Include(c => c.Category) // apply eager loading
                                                                      .ToListAsync();

            return View(getSubCategories);
        }

        // GET - Create
        public async Task<IActionResult> Create()
        {
            var getCategories = await _db.Category.ToListAsync();
            var getSubCategories = await _db.SubCategory.ToListAsync();
            var viewModel = new SubCategoryAndCategoryViewModel
            {
                CategoryList = getCategories.OrderBy(c => c.Name),
                SubCategory = new SubCategory(),
                SubCategoryList = (getSubCategories != null ? getSubCategories.Select(s => s.Name)
                                                                              .OrderBy(s => s)
                                                                              .Distinct()
                                                                              .ToList() : new List<string>())
            };

            return View(viewModel);
        }

        // POST - Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SubCategoryAndCategoryViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var getSubCategories = _db.SubCategory.Include(c => c.Category)
                                                      .Where(s => s.Name == viewModel.SubCategory.Name &&
                                                                  s.Category.Id == viewModel.SubCategory.CategoryId);

                if (getSubCategories.Count() > 0)
                {
                    // display error
                    StatusMessage = string.Concat("Error: Sub Category exists under ", getSubCategories.First().Name, ". Please use another name.");
                }
                else
                {
                    _db.SubCategory.Add(viewModel.SubCategory);

                    await _db.SaveChangesAsync();

                    // when we return to any view, we actually return to an Action method which will then call the view
                    return RedirectToAction(nameof(Index));
                }
            }

            var modelVM = new SubCategoryAndCategoryViewModel
            {
                CategoryList = await _db.Category.ToListAsync(),
                SubCategory = viewModel.SubCategory,
                SubCategoryList = await _db.SubCategory.Select(s => s.Name).Distinct().OrderBy(s => s).ToListAsync(),
                StatusMessage = StatusMessage
            };

            return View(modelVM);
        }

        [ActionName("GetSubCategory")]
        public async Task<IActionResult> GetSubCategory(int id)
        {
            List<SubCategory> subCategories = await _db.SubCategory.Where(s => s.CategoryId == id).ToListAsync();

            return Json(new SelectList(subCategories, "Id", "Name"));
        }

        // GET - Edit
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            SubCategory subCategory = await _db.SubCategory.FindAsync(id);

            if (subCategory == null) return NotFound();

            List<SubCategory> getSubCategories = await _db.SubCategory.Include(c => c.Category) // apply eager loading
                                                                      .ToListAsync();

            var viewModel = new SubCategoryAndCategoryViewModel
            {
                CategoryList = await _db.Category.ToListAsync(),
                SubCategory = subCategory,
                SubCategoryList = (getSubCategories != null ? getSubCategories.Select(s => s.Name)
                                                                              .OrderBy(s => s)
                                                                              .Distinct()
                                                                              .ToList() : new List<string>())
            };

            return View(viewModel);
        }

        // POST - Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(SubCategoryAndCategoryViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var getSubCategories = _db.SubCategory.Include(c => c.Category)
                                                      .Where(s => s.Name == viewModel.SubCategory.Name &&
                                                                  s.Category.Id == viewModel.SubCategory.CategoryId &&
                                                                  s.Id != viewModel.SubCategory.Id);

                if (getSubCategories.Count() > 0)
                {
                    // display error
                    StatusMessage = string.Concat("Error: Sub Category exists under ", getSubCategories.First().Name, ". Please use another name.");
                }
                else
                {
                    SubCategory getSubCategory = await _db.SubCategory.FindAsync(viewModel.SubCategory.Id);

                    getSubCategory.Name = viewModel.SubCategory.Name; // update just one property

                    await _db.SaveChangesAsync();

                    // when we return to any view, we actually return to an Action method which will then call the view
                    return RedirectToAction(nameof(Index));
                }
            }

            var modelVM = new SubCategoryAndCategoryViewModel
            {
                CategoryList = await _db.Category.ToListAsync(),
                SubCategory = viewModel.SubCategory,
                SubCategoryList = await _db.SubCategory.Select(s => s.Name).Distinct().OrderBy(s => s).ToListAsync(),
                StatusMessage = StatusMessage
            };

            return View(modelVM);
        }

        // GET - Details
        public async Task<IActionResult> Details(int id)
        {
            SubCategory getSubCategory = await _db.SubCategory.Include(c => c.Category).FirstOrDefaultAsync(s => s.Id == id);

            if (getSubCategory == null) return NotFound();

            return View(getSubCategory);
        }

        // GET - Delete
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            SubCategory getSubCategory = await _db.SubCategory.Include(c => c.Category).FirstOrDefaultAsync(s => s.Id == id);

            if (getSubCategory == null) return NotFound();

            return View(getSubCategory);
        }

        // POST - Delete
        [HttpPost, ActionName("Delete")] // we specify the action name to make sure it matches the asp-action="Delete" called from the view,
                                         // just in case we need to rename the action method due to existing action method names
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            SubCategory getSubCategory = await _db.SubCategory.FindAsync(id);

            if (getSubCategory == null) return NotFound();

            _db.Remove(getSubCategory);

            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}