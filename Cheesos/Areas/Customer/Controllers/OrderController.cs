using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cheesos.Data;
using Cheesos.Models;
using Cheesos.Models.ViewModels;
using Cheesos.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Cheesos.Areas.Customer.Controllers
{
    [Area("Customer")]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IEmailSender _emailSender;
        private int PageSize = 2;

        public OrderController(ApplicationDbContext db, IEmailSender emailSender)
        {
            _db = db;

            _emailSender = emailSender;
        }

        [Authorize]
        public async Task<IActionResult> Confirm(int id)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
            var orderDetailsVM = new OrderDetailsViewModel
            {
                OrderHeader = await _db.OrderHeader.Include(o => o.ApplicationUser).FirstOrDefaultAsync(o => o.Id == id && o.UserId == claim.Value),
                OrderDetails = await _db.OrderDetails.Where(o => o.OrderId == id).ToListAsync()
            };

            return View(orderDetailsVM);
        }

        public IActionResult Index()
        {
            return View();
        }

        [Authorize]
        public async Task<IActionResult> OrderHistory(int productPage = 1)
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var claim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier); // get the logged in user

            var orderListVM = new OrderListViewModel
            {
                Orders = new List<OrderDetailsViewModel>()
            };

            List<OrderHeader> orderHeaderList = await _db.OrderHeader.Include(o => o.ApplicationUser)
                                                                     .Where(u => u.UserId == claim.Value)
                                                                     .ToListAsync();

            foreach (OrderHeader orderHeader in orderHeaderList)
            {
                var individual = new OrderDetailsViewModel
                {
                    OrderHeader = orderHeader,
                    OrderDetails = await _db.OrderDetails.Where(o => o.OrderId == orderHeader.Id).ToListAsync()
                };

                orderListVM.Orders.Add(individual);
            }

            int count = orderListVM.Orders.Count;

            orderListVM.Orders = orderListVM.Orders.OrderByDescending(p => p.OrderHeader.Id)
                                                   .Skip((productPage - 1) * PageSize)
                                                   .Take(PageSize)
                                                   .ToList();

            orderListVM.PagingInfo = new PagingInfo
            {
                CurrentPage = productPage,
                ItemsPerPage = PageSize,
                TotalItem = count,
                URLParam = "/Customer/Order/OrderHistory?productPage=:" // the : is replaced in the PageLinkTagHelper class
            };

            return View(orderListVM);
        }

        [Authorize(Roles = SD.KitchenUser + "," + SD.ManagerUser)]
        public async Task<IActionResult> ManageOrder()
        {
            var orderDetailsVM = new List<OrderDetailsViewModel>();
            List<OrderHeader> orderHeaderList = await _db.OrderHeader.Where(o => o.Status == SD.StatusSubmitted ||
                                                                            o.Status == SD.StatusInProcess)
                                                                     .OrderByDescending(o => o.PickupTime)
                                                                     .ToListAsync();

            foreach (OrderHeader orderHeader in orderHeaderList)
            {
                var individual = new OrderDetailsViewModel
                {
                    OrderHeader = orderHeader,
                    OrderDetails = await _db.OrderDetails.Where(o => o.OrderId == orderHeader.Id).ToListAsync()
                };

                orderDetailsVM.Add(individual);
            }

            return View(orderDetailsVM.OrderBy(o => o.OrderHeader.PickupTime).ToList());
        }

        public async Task<IActionResult> GetOrderDetails(int id)
        {
            var orderDetailsVM = new OrderDetailsViewModel
            {
                OrderHeader = await _db.OrderHeader.FindAsync(id),
                OrderDetails = await _db.OrderDetails.Where(m => m.OrderId == id).ToListAsync()
            };

            orderDetailsVM.OrderHeader.ApplicationUser = await _db.ApplicationUser.FindAsync(orderDetailsVM.OrderHeader.UserId);

            return PartialView("_IndividualOrderDetailsPartial", orderDetailsVM);
        }

        public IActionResult GetOrderStatus(int id)
        {
            return PartialView("_OrderStatusPartial", _db.OrderHeader.Find(id).Status);
        }

        [Authorize(Roles = SD.KitchenUser + "," + SD.ManagerUser)]
        public async Task<IActionResult> OrderPrepare(int orderId)
        {
            OrderHeader orderHeader = await _db.OrderHeader.FindAsync(orderId);

            orderHeader.Status = SD.StatusInProcess;

            await _db.SaveChangesAsync();

            return RedirectToAction("ManageOrder", "Order");
        }

        [Authorize(Roles = SD.KitchenUser + "," + SD.ManagerUser)]
        public async Task<IActionResult> OrderReady(int orderId)
        {
            OrderHeader orderHeader = await _db.OrderHeader.FindAsync(orderId);

            orderHeader.Status = SD.StatusReady;

            await _db.SaveChangesAsync();

            await _emailSender.SendEmailAsync(_db.Users.FirstOrDefault(u => u.Id == orderHeader.UserId).Email, "Cheesos - Order Ready for Pickup " + orderHeader.Id.ToString(), "Order is ready for pickup successfully");

            return RedirectToAction("ManageOrder", "Order");
        }

        [Authorize(Roles = SD.KitchenUser + "," + SD.ManagerUser)]
        public async Task<IActionResult> OrderCancel(int orderId)
        {
            OrderHeader orderHeader = await _db.OrderHeader.FindAsync(orderId);

            orderHeader.Status = SD.StatusCanceled;

            await _db.SaveChangesAsync();

            await _emailSender.SendEmailAsync(_db.Users.FirstOrDefault(u => u.Id == orderHeader.UserId).Email, "Cheesos - Order Canceled " + orderHeader.Id.ToString(), "Order has been canceled successfully");

            return RedirectToAction("ManageOrder", "Order");
        }

        [Authorize]
        public async Task<IActionResult> OrderPickup(int productPage = 1, string searchName = null,
                                                     string searchPhone = null, string searchEmail = null)
        {
            var orderListVM = new OrderListViewModel
            {
                Orders = new List<OrderDetailsViewModel>()
            };

            var param = new StringBuilder();

            param.Append("/Customer/Order/OrderPickup?productPage=:"); // the : is replaced in the PageLinkTagHelper class
            param.Append("&searchName=");

            if (searchName != null)
            {
                param.Append(searchName);
            }

            param.Append("&searchPhone=");

            if (searchPhone != null)
            {
                param.Append(searchPhone);
            }

            param.Append("&searchEmail=");

            if (searchEmail != null)
            {
                param.Append(searchEmail);
            }

            var orderHeaderList = new List<OrderHeader>();

            if (searchName != null || searchPhone != null || searchEmail != null)
            {
                var user = new ApplicationUser();

                if (searchName != null)
                {
                    orderHeaderList = await _db.OrderHeader.Include(o => o.ApplicationUser)
                                                           .Where(u => u.PickupName.ToLower().Contains(searchName.ToLower()))
                                                           .OrderByDescending(o => o.OrderDate)
                                                           .ToListAsync();
                }
                else
                {
                    if (searchPhone != null)
                    {
                        orderHeaderList = await _db.OrderHeader.Include(o => o.ApplicationUser)
                                                               .Where(u => u.PhoneNumber.Contains(searchPhone))
                                                               .OrderByDescending(o => o.OrderDate)
                                                               .ToListAsync();
                    }
                    else
                    {
                        if (searchEmail != null)
                        {
                            user = await _db.ApplicationUser.FirstOrDefaultAsync(u => u.Email.ToLower().Contains(searchEmail.ToLower()));

                            orderHeaderList = await _db.OrderHeader.Include(o => o.ApplicationUser)
                                                                   .Where(u => u.UserId == user.Id)
                                                                   .OrderByDescending(o => o.OrderDate)
                                                                   .ToListAsync();
                        }
                    }
                }
            }
            else
            {
                orderHeaderList = await _db.OrderHeader.Include(o => o.ApplicationUser)
                                                       .Where(u => u.Status == SD.StatusReady)
                                                       .ToListAsync();
            }

            foreach (OrderHeader orderHeader in orderHeaderList)
            {
                var individual = new OrderDetailsViewModel
                {
                    OrderHeader = orderHeader,
                    OrderDetails = await _db.OrderDetails.Where(o => o.OrderId == orderHeader.Id).ToListAsync()
                };

                orderListVM.Orders.Add(individual);
            }


            int count = orderListVM.Orders.Count;

            orderListVM.Orders = orderListVM.Orders.OrderByDescending(p => p.OrderHeader.Id)
                                                   .Skip((productPage - 1) * PageSize)
                                                   .Take(PageSize)
                                                   .ToList();

            orderListVM.PagingInfo = new PagingInfo
            {
                CurrentPage = productPage,
                ItemsPerPage = PageSize,
                TotalItem = count,
                URLParam = param.ToString()
            };

            return View(orderListVM);
        }

        [HttpPost]
        [ActionName("OrderPickup")]
        [Authorize(Roles = SD.FrontDeskUser + "," + SD.ManagerUser)]
        public async Task<IActionResult> OrderPickupPost(int orderId)
        {
            OrderHeader orderHeader = await _db.OrderHeader.FindAsync(orderId);

            orderHeader.Status = SD.StatusCompleted;

            await _db.SaveChangesAsync();

            await _emailSender.SendEmailAsync(_db.Users.FirstOrDefault(u => u.Id == orderHeader.UserId).Email, "Cheesos - Order Completed " + orderHeader.Id.ToString(), "Order has been completed successfully");

            return RedirectToAction("OrderPickup", "Order");
        }
    }
}