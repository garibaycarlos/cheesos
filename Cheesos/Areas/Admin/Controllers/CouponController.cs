using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cheesos.Data;
using Cheesos.Models;
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
    public class CouponController : Controller
    {
        private readonly ApplicationDbContext _db;
        private const string StatusMessage = "Error: Coupon {0} already exists. Please use another name.";

        [BindProperty]
        public Coupon Coupon { get; set; }

        public CouponController(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IActionResult> Index()
        {
            return View(await _db.Coupon.ToListAsync());
        }

        // GET - Create
        public IActionResult Create()
        {
            return View();
        }

        // GET - Post
        [HttpPost, ActionName("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreatePost()
        {
            if (ModelState.IsValid)
            {
                var getCoupon = _db.Coupon.Where(c => c.Name == Coupon.Name);

                if (getCoupon.Count() > 0)
                {
                    // display error
                    TempData["StatusMessage"] = string.Format(StatusMessage, Coupon.Name);
                }
                else
                {
                    var files = HttpContext.Request.Form.Files;

                    if (files.Count > 0)
                    {
                        byte[] picture = null;

                        using (Stream fs = files[0].OpenReadStream())
                        {
                            using (var ms = new MemoryStream())
                            {
                                fs.CopyTo(ms);

                                picture = ms.ToArray();
                            }
                        }

                        Coupon.Picture = picture;
                    }

                    _db.Coupon.Add(Coupon);

                    await _db.SaveChangesAsync();

                    return RedirectToAction(nameof(Index));
                }
            }

            return View();
        }

        // GET - Edit
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            Coupon getCoupon = await _db.Coupon.FindAsync(id);

            if (getCoupon == null) return NotFound();

            return View(getCoupon);
        }

        // POST - Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit()
        {
            if (Coupon.Id == 0) return NotFound();

            if (ModelState.IsValid)
            {
                Coupon getCoupon = await _db.Coupon.AsNoTracking().FirstOrDefaultAsync(c => c.Name == Coupon.Name);

                if (getCoupon != null)
                {
                    if (getCoupon.Id != Coupon.Id)
                    {
                        // display error
                        TempData["StatusMessage"] = string.Format(StatusMessage, Coupon.Name);
                    }
                    else
                    {
                        var files = HttpContext.Request.Form.Files;

                        if (files.Count > 0)
                        {
                            byte[] picture = null;

                            using (Stream fs = files[0].OpenReadStream())
                            {
                                using (var ms = new MemoryStream())
                                {
                                    fs.CopyTo(ms);

                                    picture = ms.ToArray();
                                }
                            }

                            Coupon.Picture = picture;
                        }
                        else
                        {
                            Coupon.Picture = getCoupon.Picture;
                        }

                        _db.Update(Coupon); // we use this method because we do not need to update several fields (just one here)

                        await _db.SaveChangesAsync();

                        // when we return to any view, we actually return to an Action method which will then call the view
                        return RedirectToAction(nameof(Index));
                    }
                }
            }

            return View(Coupon);
        }

        // GET - Details
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            Coupon getCoupon = await _db.Coupon.FindAsync(id);

            if (getCoupon == null) return NotFound();

            return View(getCoupon);
        }

        // GET - Delete
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            Coupon getCoupon = await _db.Coupon.FindAsync(id);

            if (getCoupon == null) return NotFound();

            return View(getCoupon);
        }

        // POST - Delete
        [HttpPost, ActionName("Delete")] // we specify the action name to make sure it matches the asp-action="Delete" called from the view,
                                         // just in case we need to rename the action method due to existing action method names
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            Coupon getCoupon = await _db.Coupon.FindAsync(id);

            if (getCoupon == null) return NotFound();

            _db.Remove(getCoupon);

            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}