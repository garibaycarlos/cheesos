using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cheesos.Data;
using Cheesos.Models;
using Cheesos.Models.ViewModels;
using Cheesos.Utility;
using Stripe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace Cheesos.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IEmailSender _emailSender;

        [BindProperty]
        public OrderDetailsCartViewModel OrderDetailsCartVM { get; set; }

        public CartController(ApplicationDbContext db, IEmailSender emailSender)
        {
            _db = db;

            _emailSender = emailSender;
        }

        public async Task<IActionResult> Index()
        {
            OrderDetailsCartVM = new OrderDetailsCartViewModel
            {
                OrderHeader = new OrderHeader()
            };

            OrderDetailsCartVM.OrderHeader.OrderTotal = 0;

            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier); // get the logged in user
            var cart = _db.ShoppingCart.Where(s => s.ApplicationUserId == claim.Value); // get the shopping cart items for the user

            if (cart != null)
            {
                OrderDetailsCartVM.ListCart = cart.ToList();
            }

            // calculate order total
            foreach (ShoppingCart shoppingCart in OrderDetailsCartVM.ListCart)
            {
                shoppingCart.MenuItem = await _db.MenuItem.FirstOrDefaultAsync(m => m.Id == shoppingCart.MenuItemId);

                OrderDetailsCartVM.OrderHeader.OrderTotal += (shoppingCart.MenuItem.Price * shoppingCart.Count);

                shoppingCart.MenuItem.Description = SD.ConvertToRawHtml(shoppingCart.MenuItem.Description);

                if (shoppingCart.MenuItem.Description.Length > 100)
                {
                    shoppingCart.MenuItem.Description = shoppingCart.MenuItem.Description.Substring(0, 99) + "...";
                }
            }

            OrderDetailsCartVM.OrderHeader.OrderTotalOriginal = OrderDetailsCartVM.OrderHeader.OrderTotal;

            if (HttpContext.Session.GetString(SD.ssCouponCode) != null)
            {
                OrderDetailsCartVM.OrderHeader.CouponCode = HttpContext.Session.GetString(SD.ssCouponCode);

                var couponFromDb = await _db.Coupon.Where(c => c.Name.ToLower() == OrderDetailsCartVM.OrderHeader.CouponCode.ToLower()).FirstOrDefaultAsync();

                OrderDetailsCartVM.OrderHeader.OrderTotal = SD.DiscountedPrice(couponFromDb, OrderDetailsCartVM.OrderHeader.OrderTotalOriginal);
            }

            return View(OrderDetailsCartVM);
        }

        public async Task<IActionResult> AddCoupon()
        {
            if (OrderDetailsCartVM.OrderHeader.CouponCode == null)
            {
                OrderDetailsCartVM.OrderHeader.CouponCode = string.Empty;
            }

            var couponFromDb = await _db.Coupon.Where(c => c.Name.ToLower() == OrderDetailsCartVM.OrderHeader.CouponCode.ToLower()).FirstOrDefaultAsync();

            if (couponFromDb != null)
            {
                HttpContext.Session.SetString(SD.ssCouponCode, OrderDetailsCartVM.OrderHeader.CouponCode);
            }

            return RedirectToAction(nameof(Index));
        }

        public IActionResult RemoveCoupon()
        {
            HttpContext.Session.SetString(SD.ssCouponCode, string.Empty);

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Plus(int cartId)
        {
            var cart = await _db.ShoppingCart.FindAsync(cartId);

            cart.Count++;

            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Minus(int cartId)
        {
            var cart = await _db.ShoppingCart.FindAsync(cartId);

            if (cart.Count == 1)
            {
                _db.ShoppingCart.Remove(cart);

                await _db.SaveChangesAsync();

                int cnt = _db.ShoppingCart.Where(u => u.ApplicationUserId == cart.ApplicationUserId).ToList().Count;

                HttpContext.Session.SetInt32(SD.ssShoppingCartCount, cnt);
            }
            else
            {
                cart.Count--;

                await _db.SaveChangesAsync();
            }

            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Remove(int cartId)
        {
            var cart = await _db.ShoppingCart.FindAsync(cartId);

            _db.ShoppingCart.Remove(cart);

            await _db.SaveChangesAsync();

            int cnt = _db.ShoppingCart.Where(u => u.ApplicationUserId == cart.ApplicationUserId).ToList().Count;

            HttpContext.Session.SetInt32(SD.ssShoppingCartCount, cnt);

            await _db.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Summary()
        {
            OrderDetailsCartVM = new OrderDetailsCartViewModel
            {
                OrderHeader = new OrderHeader()
            };

            OrderDetailsCartVM.OrderHeader.OrderTotal = 0;

            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier); // get the logged in user
            ApplicationUser applicationUser = await _db.ApplicationUser.FindAsync(claim.Value);
            var cart = _db.ShoppingCart.Where(s => s.ApplicationUserId == claim.Value); // get the shopping cart items for the user

            if (cart != null)
            {
                OrderDetailsCartVM.ListCart = cart.ToList();
            }

            // calculate order total
            foreach (ShoppingCart shoppingCart in OrderDetailsCartVM.ListCart)
            {
                shoppingCart.MenuItem = await _db.MenuItem.FirstOrDefaultAsync(m => m.Id == shoppingCart.MenuItemId);

                OrderDetailsCartVM.OrderHeader.OrderTotal += (shoppingCart.MenuItem.Price * shoppingCart.Count);
            }

            OrderDetailsCartVM.OrderHeader.OrderTotalOriginal = OrderDetailsCartVM.OrderHeader.OrderTotal;
            OrderDetailsCartVM.OrderHeader.PickupName = applicationUser.Name;
            OrderDetailsCartVM.OrderHeader.PhoneNumber = applicationUser.PhoneNumber;
            OrderDetailsCartVM.OrderHeader.PickupTime = DateTime.Now;

            if (HttpContext.Session.GetString(SD.ssCouponCode) != null)
            {
                OrderDetailsCartVM.OrderHeader.CouponCode = HttpContext.Session.GetString(SD.ssCouponCode);

                var couponFromDb = await _db.Coupon.Where(c => c.Name.ToLower() == OrderDetailsCartVM.OrderHeader.CouponCode.ToLower()).FirstOrDefaultAsync();

                OrderDetailsCartVM.OrderHeader.OrderTotal = SD.DiscountedPrice(couponFromDb, OrderDetailsCartVM.OrderHeader.OrderTotalOriginal);
            }

            return View(OrderDetailsCartVM);
        }

        [HttpPost]
        [ActionName("Summary")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SummaryPost(string stripeToken)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier); // get the logged in user

            OrderDetailsCartVM.ListCart = await _db.ShoppingCart.Where(s => s.ApplicationUserId == claim.Value).ToListAsync();

            OrderDetailsCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
            OrderDetailsCartVM.OrderHeader.OrderDate = DateTime.Now;
            OrderDetailsCartVM.OrderHeader.UserId = claim.Value;
            OrderDetailsCartVM.OrderHeader.Status = SD.PaymentStatusPending;
            OrderDetailsCartVM.OrderHeader.PickupTime = Convert.ToDateTime(OrderDetailsCartVM.OrderHeader.PickupDate.ToShortDateString() + ' ' + OrderDetailsCartVM.OrderHeader.PickupTime.ToShortTimeString());

            _db.OrderHeader.Add(OrderDetailsCartVM.OrderHeader);

            await _db.SaveChangesAsync();

            OrderDetailsCartVM.OrderHeader.OrderTotalOriginal = 0;

            var orderDetailsList = new List<OrderDetails>();


            // calculate order total
            foreach (ShoppingCart item in OrderDetailsCartVM.ListCart)
            {
                item.MenuItem = await _db.MenuItem.FirstOrDefaultAsync(m => m.Id == item.MenuItemId);

                var orderDetails = new OrderDetails
                {
                    MenuItemId = item.MenuItemId,
                    OrderId = OrderDetailsCartVM.OrderHeader.Id,
                    Name = item.MenuItem.Name,
                    Description = item.MenuItem.Description,
                    Price = item.MenuItem.Price,
                    Count = item.Count
                };

                OrderDetailsCartVM.OrderHeader.OrderTotalOriginal += (orderDetails.Count * orderDetails.Price);

                _db.OrderDetails.Add(orderDetails);
            }

            if (HttpContext.Session.GetString(SD.ssCouponCode) != null)
            {
                OrderDetailsCartVM.OrderHeader.CouponCode = HttpContext.Session.GetString(SD.ssCouponCode);

                var couponFromDb = await _db.Coupon.Where(c => c.Name.ToLower() == OrderDetailsCartVM.OrderHeader.CouponCode.ToLower()).FirstOrDefaultAsync();

                OrderDetailsCartVM.OrderHeader.OrderTotal = SD.DiscountedPrice(couponFromDb, OrderDetailsCartVM.OrderHeader.OrderTotalOriginal);
            }
            else // no coupon was used
            {
                OrderDetailsCartVM.OrderHeader.OrderTotal = OrderDetailsCartVM.OrderHeader.OrderTotalOriginal;
            }

            OrderDetailsCartVM.OrderHeader.CouponCodeDiscount = OrderDetailsCartVM.OrderHeader.OrderTotalOriginal - OrderDetailsCartVM.OrderHeader.OrderTotal;

            _db.ShoppingCart.RemoveRange(OrderDetailsCartVM.ListCart); // remove the cart from the db

            HttpContext.Session.SetInt32(SD.ssShoppingCartCount, 0); // remove the shoppng cart value from the session

            await _db.SaveChangesAsync();

            var options = new ChargeCreateOptions
            {
                Amount = Convert.ToInt32(OrderDetailsCartVM.OrderHeader.OrderTotal * 100), // we need to pass it in cents
                Currency = "usd",
                Description = "Order ID : " + OrderDetailsCartVM.OrderHeader.Id,
                Source = stripeToken
            };

            var service = new ChargeService();
            Charge charge = service.Create(options);

            if (charge.BalanceTransactionId == null)
            {
                OrderDetailsCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusRejected;
            }
            else
            {
                OrderDetailsCartVM.OrderHeader.TransactionId = charge.BalanceTransactionId;
            }

            if (charge.Status.ToLower() == "succeeded")
            {
                OrderDetailsCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusApproved;
                OrderDetailsCartVM.OrderHeader.Status = SD.StatusSubmitted;

                // email for successful order
                await _emailSender.SendEmailAsync(_db.Users.FirstOrDefault(u => u.Id == claim.Value).Email, "Cheesos - Order Created " + OrderDetailsCartVM.OrderHeader.Id.ToString(), "Order has been submitted successfully");
            }
            else
            {
                OrderDetailsCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusRejected;
            }

            await _db.SaveChangesAsync();

            return RedirectToAction("Confirm", "Order", new { id = OrderDetailsCartVM.OrderHeader.Id });
        }
    }
}