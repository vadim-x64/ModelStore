using System.Diagnostics;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ModelStore.Data;
using ModelStore.Models;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;

namespace ModelStore.Controllers
{
    public class HomeController : Controller
    {
        private readonly DB _db;

        public HomeController(DB db)
        {
            _db = db;
        }

        [HttpGet]
        public IActionResult Register(string returnUrl = null)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["ReturnUrl"] = returnUrl ?? Url.Action("Index", "Home");
            return View();
        }

        private string GetPasswordStrength(string password)
        {
            bool hasUpperCase = password.Any(char.IsUpper);
            bool hasLowerCase = password.Any(char.IsLower);
            bool hasDigit = password.Any(char.IsDigit);
            bool hasSpecialChar = password.Any(c => !char.IsLetterOrDigit(c));
            bool isLongEnough = password.Length >= 8;
            if (isLongEnough && hasUpperCase && hasLowerCase && hasDigit && hasSpecialChar)
            {
                return "strong";
            }
            return "weak";
        }

        [HttpPost]
        public async Task<IActionResult> Register(Registration model, IFormFile profilePicture, Login user, string returnUrl = null)
        {
            try
            {
                var passwordStrength = GetPasswordStrength(user.Password);
                if (passwordStrength == "weak")
                {
                    return Json(new { success = false, message = "Введено некоректний пароль." });
                }
                var existingUser = await _db.User.FirstOrDefaultAsync(u => u.Username == user.Username);
                if (existingUser != null)
                {
                    return Json(new { success = false, message = "Ім'я користувача вже зайняте." });
                }
                if (profilePicture != null)
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await profilePicture.CopyToAsync(memoryStream);
                        model.ProfilePicture = memoryStream.ToArray();
                    }
                }
                else
                {
                    model.ProfilePicture = GetDefaultProfilePicture();
                }
                user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
                user.Role = 1;
                _db.User.Add(user);
                await _db.SaveChangesAsync();
                model.Id = user.Id;
                _db.Users.Add(model);
                await _db.SaveChangesAsync();
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, user.Role.ToString())
                };
                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));
                return Json(new { success = true, redirectUrl = returnUrl ?? Url.Action("Index") });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Помилка реєстрації." });
            }
        }

        [Authorize(Roles = "1")]
        public async Task<IActionResult> Profile()
        {
            if (User.Identity.IsAuthenticated)
            {
                var user = await _db.User.FirstOrDefaultAsync(u => u.Username == User.Identity.Name);
                if (user == null)
                {
                    return NotFound();
                }
                var registration = await _db.Users.FirstOrDefaultAsync(r => r.Id == user.Id);
                if (registration == null)
                {
                    return NotFound();
                }
                var orders = await _db.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Where(o => o.UserId == user.Id)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
                var comments = await _db.Comments
                .Where(c => c.UserId == user.Id)
                .Include(c => c.Product)
                .OrderByDescending(c => c.DatePosted)
                .ToListAsync();
                if (registration?.BirthDate != null)
                {
                    var birthDate = registration.BirthDate;
                    var today = DateOnly.FromDateTime(DateTime.Today);
                    var age = today.Year - birthDate.Year;
                    if (birthDate > today.AddYears(-age))
                        age--;

                    ViewData["Age"] = age;
                    ViewData["BirthDate"] = birthDate.ToDateTime(TimeOnly.MinValue).ToString("dd MMMM yyyy");
                }
                else
                {
                    ViewData["Age"] = "N/A";
                    ViewData["BirthDate"] = "N/A";
                }
                ViewData["Orders"] = orders;
                ViewData["Comments"] = comments;
                ViewData["Username"] = user.Username;
                ViewData["ProfilePicture"] = registration.ProfilePicture;
                ViewData["FirstName"] = registration.FirstName;
                ViewData["LastName"] = registration.LastName;
                ViewData["MiddleName"] = registration.MiddleName;
                ViewData["BirthDate"] = registration.BirthDate;
                ViewData["Phone"] = registration.Phone;
                ViewData["Email"] = registration.Email;
                ViewData["Address"] = registration.Address;
                ViewData["Username"] = user.Username;
                ViewData["Password"] = user.Password;
                return View(orders);
            }
            return RedirectToAction("Login");
        }

        [HttpPost]
        public async Task<IActionResult> EditProfile(Registration model, Login login, string Password, string ConfirmPassword)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login");
            }
            var user = await _db.User.FirstOrDefaultAsync(u => u.Username == User.Identity.Name);
            if (user == null)
            {
                return NotFound();
            }
            var registration = await _db.Users.FirstOrDefaultAsync(r => r.Id == user.Id);
            if (registration == null)
            {
                return NotFound();
            }
            bool credentialsChanged = false;
            registration.FirstName = model.FirstName;
            registration.LastName = model.LastName;
            registration.MiddleName = model.MiddleName;
            registration.Email = model.Email;
            registration.Phone = model.Phone;
            registration.Address = model.Address;
            if (!string.IsNullOrEmpty(login.Username) && login.Username != user.Username)
            {
                if (await _db.User.AnyAsync(u => u.Username == login.Username && u.Id != user.Id))
                {
                    return RedirectToAction("Profile");
                }
                var cartItems = await _db.CartItems.Where(c => c.UserId == user.Username).ToListAsync();
                foreach (var item in cartItems)
                {
                    item.UserId = login.Username;
                }
                user.Username = login.Username;
                credentialsChanged = true;
            }
            if (!string.IsNullOrEmpty(Password))
            {
                if (Password != ConfirmPassword)
                {
                    return RedirectToAction("Profile");
                }
                if (!IsPasswordStrongOrMedium(Password))
                {

                    return RedirectToAction("Profile");
                }
                user.Password = BCrypt.Net.BCrypt.HashPassword(Password);
                credentialsChanged = true;
            }
            await _db.SaveChangesAsync();
            if (credentialsChanged)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Login", new { message = "Please login with your new credentials" });
            }
            return RedirectToAction("Profile");
        }

        private bool IsPasswordStrongOrMedium(string password)
        {
            return password.Length >= 8 && password.Any(char.IsLower) && password.Any(char.IsUpper) && password.Any(char.IsDigit);
        }

        [HttpGet]
        public async Task<IActionResult> CheckUsernameAvailability(string username)
        {
            if (await _db.User.AnyAsync(u => u.Username == username))
            {
                return Json(new { isAvailable = false });
            }
            return Json(new { isAvailable = true });
        }

        public IActionResult Contacts()
        {
            if (User.Identity.IsAuthenticated)
            {
                var user = _db.User.FirstOrDefault(u => u.Username == User.Identity.Name);
                if (user != null)
                {
                    var profile = _db.Users.FirstOrDefault(r => r.Id == user.Id);
                    if (profile != null)
                    {
                        ViewData["ProfilePicture"] = profile.ProfilePicture;
                    }
                }
            }
            return View();
        }

        public IActionResult Forum()
        {
            if (User.Identity.IsAuthenticated)
            {
                var user = _db.User.FirstOrDefault(u => u.Username == User.Identity.Name);
                if (user != null)
                {
                    var profile = _db.Users.FirstOrDefault(r => r.Id == user.Id);
                    if (profile != null)
                    {
                        ViewData["ProfilePicture"] = profile.ProfilePicture;
                    }
                }
            }
            return View();
        }

        [Authorize(Roles = "2")]
        public async Task<IActionResult> Customers()
        {
            if (User.Identity.IsAuthenticated)
            {
                var user = _db.User.FirstOrDefault(u => u.Username == User.Identity.Name);
                if (user != null)
                {
                    var profile = _db.Users.FirstOrDefault(r => r.Id == user.Id);
                    if (profile != null)
                    {
                        ViewData["ProfilePicture"] = profile.ProfilePicture;
                    }
                }
            }

            var customers = await (from user in _db.User
                                   join reg in _db.Users on user.Id equals reg.Id
                                   where user.Role == 1
                                   select new
                                   {
                                       user.Id,
                                       reg.ProfilePicture,
                                       reg.FirstName,
                                       reg.LastName,
                                       reg.MiddleName,
                                       reg.BirthDate,
                                       reg.Phone,
                                       reg.Email,
                                       reg.Address,
                                       user.Username,
                                       user.IsBlocked,
                                       Comments = _db.Comments
                                           .Where(c => c.UserId == user.Id)
                                           .OrderByDescending(c => c.DatePosted)
                                           .Select(c => new
                                           {
                                               c.ProductId,
                                               c.Content,
                                               c.DatePosted,
                                               ProductName = c.Product.Name
                                           })
                                           .ToList(),
                                       Orders = _db.Orders
                                           .Where(o => o.UserId == user.Id)
                                           .OrderByDescending(o => o.OrderDate)
                                           .Select(o => new
                                           {
                                               o.Id,
                                               o.OrderDate,
                                               o.Status,
                                               IsPendingOrder = o.Status == OrderStatus.Pending,
                                               IsCancellationRequestedOrder = o.Status == OrderStatus.CancellationRequested,
                                               o.PaymentMethod,
                                               o.DeliveryMethod,
                                               Total = o.OrderItems.Sum(oi => oi.Product.Price * oi.Quantity)
                                           })
                                           .ToList()
                                   }).ToListAsync();
            return View(customers);
        }

        [HttpPost]
        [Authorize(Roles = "2")]
        public async Task<IActionResult> UpdateOrderStatus(int orderId, OrderStatus status, string comment)
        {
            var order = await _db.Orders
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
                return NotFound();

            if (status == OrderStatus.ReceivedByCustomer)
                return BadRequest("This status can only be set by the customer.");

            if (order.Status == OrderStatus.ReceivedByCustomer)
                return BadRequest("Cannot modify order status after customer confirmation.");

            if (status == OrderStatus.CancellationRequested)
            {
                order.Status = OrderStatus.CancellationRequested;
                order.LastUpdated = DateTime.Now;
                await _db.SaveChangesAsync();
            }

            order.Status = status;
            order.LastUpdated = DateTime.Now;

            await _db.SaveChangesAsync();
            return RedirectToAction("Customers");
        }

        [HttpPost]
        [Authorize(Roles = "1")]
        public async Task<IActionResult> ConfirmOrderReceived(int orderId)
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectToAction("Login");

            var user = await _db.User.FirstOrDefaultAsync(u => u.Username == User.Identity.Name);
            if (user == null)
                return NotFound();

            var order = await _db.Orders
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == user.Id);

            if (order == null)
                return NotFound();

            if (order.Status == OrderStatus.Completed)
            {
                order.Status = OrderStatus.ReceivedByCustomer;
                order.LastUpdated = DateTime.Now;
                await _db.SaveChangesAsync();
            }

            return RedirectToAction("Profile");
        }

        [HttpPost]
        [Authorize(Roles = "1")]
        public async Task<IActionResult> RequestOrderCancellation(int orderId)
        {
            var order = await _db.Orders.FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null ||
                (order.Status != OrderStatus.Pending && order.Status != OrderStatus.Accepted))
                return RedirectToAction("Profile", new { errorMessage = "Скасування не дозволено." });

            order.Status = OrderStatus.CancellationRequested;
            order.LastUpdated = DateTime.Now;
            await _db.SaveChangesAsync();

            return RedirectToAction("Profile", new { successMessage = "Запит на скасування відправлено." });
        }

        [HttpPost]
        [Authorize(Roles = "2")]
        public async Task<IActionResult> ConfirmOrderCancellation(int orderId)
        {
            var order = await _db.Orders
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null || order.Status != OrderStatus.CancellationRequested)
                return RedirectToAction("Customers", new { errorMessage = "Помилка скасування." });

            _db.Orders.Remove(order);
            await _db.SaveChangesAsync();

            return RedirectToAction("Customers", new { successMessage = "Замовлення успішно скасовано." });
        }

        [Authorize(Roles = "2")]
        public async Task<IActionResult> Admin()
        {
            if (User.Identity.IsAuthenticated)
            {
                var user = await _db.User.FirstOrDefaultAsync(u => u.Username == User.Identity.Name);
                if (user != null)
                {
                    var profile = await _db.Users.FirstOrDefaultAsync(r => r.Id == user.Id);
                    if (profile != null)
                    {

                        var comments = await _db.Comments
             .Where(c => c.UserId == user.Id)
             .Include(c => c.Product)
             .OrderByDescending(c => c.DatePosted)
             .ToListAsync();
                        ViewData["Comments"] = comments;
                        ViewData["Username"] = user.Username;
                        ViewData["ProfilePicture"] = profile.ProfilePicture;
                        ViewData["FirstName"] = profile.FirstName;
                        ViewData["LastName"] = profile.LastName;
                        ViewData["MiddleName"] = profile.MiddleName;
                        ViewData["BirthDate"] = profile.BirthDate;
                        ViewData["Phone"] = profile.Phone;
                        ViewData["Email"] = profile.Email;
                        ViewData["Address"] = profile.Address;
                    }
                }
            }
            return View();
        }

        public async Task<IActionResult> Cart()
        {
            List<CartItem> cartItems;

            if (User.IsInRole("2"))
            {
                return RedirectToAction("Index");
            }

            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", new { returnUrl = Url.Action("Index") });
            }
            else
            {
                var user = _db.User.FirstOrDefault(u => u.Username == User.Identity.Name);
                if (user != null)
                {
                    var profile = _db.Users.FirstOrDefault(r => r.Id == user.Id);
                    if (profile != null)
                    {
                        ViewData["ProfilePicture"] = profile.ProfilePicture;
                    }
                }

                var userId = User.Identity.Name;
                cartItems = await _db.CartItems
                    .Include(c => c.Product)
                    .Where(c => c.UserId == userId)
                    .ToListAsync();
            }
            return View(cartItems);
        }

        [Authorize(Roles = "1")]
        public async Task<IActionResult> Checkout(string returnUrl = "/Home/Cart")
        {
            var user = await _db.User.FirstOrDefaultAsync(u => u.Username == User.Identity.Name);
            if (user == null)
            {
                return RedirectToAction("Login", new { returnUrl = Url.Action("Cart") });
            }

            if (User.Identity.IsAuthenticated)
            {
                if (user != null)
                {
                    var profile = _db.Users.FirstOrDefault(r => r.Id == user.Id);
                    if (profile != null)
                    {
                        ViewData["ProfilePicture"] = profile.ProfilePicture;
                    }
                }
                else
                {
                    return RedirectToAction("Register", new { returnUrl = Url.Action("Cart") });
                }
            }
            else
            {
                return RedirectToAction("Login", new { returnUrl = Url.Action("Cart") });
            }

            var registration = await _db.Users.FirstOrDefaultAsync(r => r.Id == user.Id);
            if (registration == null)
            {
                return NotFound();
            }

            var cartItems = await _db.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == user.Username)
                .ToListAsync();

            if (!cartItems.Any())
            {
                
                return Redirect(returnUrl);
            }

            var checkoutViewModel = new OrderViewModel
            {
                User = registration,
                CartItems = cartItems
            };

            return View(checkoutViewModel);
        }

        [HttpPost]
        [Authorize(Roles = "1")]
        public async Task<IActionResult> SubmitOrder(string paymentMethod, string deliveryMethod, string address, string firstname, string lastname, string email, string phone)
        {
            if (!User.Identity.IsAuthenticated)
                return RedirectToAction("Login");

            var user = await _db.User.FirstOrDefaultAsync(u => u.Username == User.Identity.Name);
            if (user == null)
                return NotFound();

            var registration = await _db.Users.FirstOrDefaultAsync(r => r.Id == user.Id);
            if (registration == null)
                return NotFound();

            var cartItems = await _db.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == user.Username)
                .ToListAsync();

            if (!cartItems.Any())
                return RedirectToAction("Cart");

            var order = new Order
            {
                UserId = user.Id,
                OrderDate = DateTime.Now,
                PaymentMethod = paymentMethod,
                DeliveryMethod = deliveryMethod,
                FirstName = firstname ?? registration.FirstName,
                LastName = lastname ?? registration.LastName,
                Email = email ?? registration.Email,
                Phone = phone ?? registration.Phone,
                Address = address ?? registration.Address,
                Status = OrderStatus.Pending,
                LastUpdated = DateTime.Now,
                OrderItems = cartItems.Select(ci => new OrderItem
                {
                    ProductId = ci.ProductId,
                    ProductName = ci.Product.Name,
                    ProductPrice = ci.Product.Price,
                    ProductDescription = ci.Product.Description,
                    ProductPhoto = ci.Product.Photo,
                    Quantity = ci.Quantity
                }).ToList()
            };

            _db.Orders.Add(order);
            await _db.SaveChangesAsync();
            return RedirectToAction("Profile");
        }

        [HttpPost]
        [Authorize(Roles = "1")]
        public async Task<IActionResult> RemoveFromCart(int productId)
        {
            if (!User.Identity.IsAuthenticated)
            {
                var sessionCart = HttpContext.Session.GetString("Cart") ?? "[]";
                var cartItems = JsonSerializer.Deserialize<List<CartItem>>(sessionCart);
                cartItems.RemoveAll(i => i.ProductId == productId);
                HttpContext.Session.SetString("Cart", JsonSerializer.Serialize(cartItems));
            }
            else
            {
                var userId = User.Identity.Name;
                var cartItem = await _db.CartItems
                    .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId);

                if (cartItem != null)
                {
                    _db.CartItems.Remove(cartItem);
                    await _db.SaveChangesAsync();
                }
            }

            return Json(new { success = true });
        }

        [HttpPost]
        [Authorize(Roles = "1")]
        public async Task<IActionResult> RemoveOneFromCart(int productId)
        {
            if (!User.Identity.IsAuthenticated)
            {
                var sessionCart = HttpContext.Session.GetString("Cart") ?? "[]";
                var cartItems = JsonSerializer.Deserialize<List<CartItem>>(sessionCart);

                var cartItem = cartItems.FirstOrDefault(i => i.ProductId == productId);
                if (cartItem != null)
                {
                    if (cartItem.Quantity > 1)
                    {
                        cartItem.Quantity--;
                    }
                    else
                    {
                        cartItems.Remove(cartItem);
                    }
                }

                HttpContext.Session.SetString("Cart", JsonSerializer.Serialize(cartItems));
            }
            else
            {
                var userId = User.Identity.Name;
                var cartItem = await _db.CartItems
                    .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId);

                if (cartItem != null)
                {
                    if (cartItem.Quantity > 1)
                    {
                        cartItem.Quantity--;
                    }
                    else
                    {
                        _db.CartItems.Remove(cartItem);
                    }

                    await _db.SaveChangesAsync();
                }
            }

            return Json(new { success = true });
        }

        [HttpPost]
        [Authorize(Roles = "1")]
        public async Task<IActionResult> ClearCart()
        {
            if (!User.Identity.IsAuthenticated)
            {
                HttpContext.Session.SetString("Cart", "[]");
            }
            else
            {
                var userId = User.Identity.Name;
                var userCartItems = await _db.CartItems.Where(c => c.UserId == userId).ToListAsync();

                if (userCartItems.Any())
                {
                    _db.CartItems.RemoveRange(userCartItems);
                    await _db.SaveChangesAsync();
                }
            }

            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> AddComment(int productId, string content)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Home");
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                ModelState.AddModelError("", "Comment cannot be empty.");
                return RedirectToAction("DetailItem", new { id = productId });
            }

            var user = await _db.User.FirstOrDefaultAsync(u => u.Username == User.Identity.Name);
            if (user == null)
            {
                return Unauthorized();
            }

            var comment = new Comment
            {
                Content = content,
                DatePosted = DateTime.Now,
                ProductId = productId,
                UserId = user.Id
            };

            _db.Comments.Add(comment);
            await _db.SaveChangesAsync();
            return RedirectToAction("DetailItem", new { id = productId });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCommentFromProfile(int commentId)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login", "Home");
            }

            var user = await _db.User.FirstOrDefaultAsync(u => u.Username == User.Identity.Name);
            if (user == null)
            {
                return Unauthorized();
            }

            var comment = await _db.Comments.FirstOrDefaultAsync(c => c.Id == commentId);
            if (comment == null || comment.UserId != user.Id)
            {
                return Forbid();
            }

            _db.Comments.Remove(comment);
            await _db.SaveChangesAsync();

            if (User.IsInRole("2"))
            {
                return RedirectToAction("Admin");
            }
            return RedirectToAction("Profile");
        }

        [HttpPost]
        [Authorize(Roles = "2")]
        public async Task<IActionResult> DeleteComment(int commentId)
        {
            var comment = await _db.Comments.FirstOrDefaultAsync(c => c.Id == commentId);
            if (comment == null)
            {
                return NotFound();
            }

            _db.Comments.Remove(comment);
            await _db.SaveChangesAsync();
            return RedirectToAction("DetailItem", new { id = comment.ProductId });
        }

        public async Task<IActionResult> Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                var user = await _db.User.FirstOrDefaultAsync(u => u.Username == User.Identity.Name);
                if (user != null)
                {
                    var profile = await _db.Users.FirstOrDefaultAsync(r => r.Id == user.Id);
                    if (profile != null)
                    {
                        ViewData["ProfilePicture"] = profile.ProfilePicture;
                    }
                }
            }

            var categories = await _db.Categories.ToListAsync();
            ViewData["Categories"] = categories;

            var customers = await _db.User
                .Where(u => u.Role == 1)
                .ToListAsync();
            return View(customers);
        }

        [Authorize(Roles = "2")]
        public async Task<IActionResult> Category()
        {
            var categories = await _db.Categories.ToListAsync();

            if (User.Identity.IsAuthenticated)
            {
                var user = _db.User.FirstOrDefault(u => u.Username == User.Identity.Name);
                if (user != null)
                {
                    var profile = _db.Users.FirstOrDefault(r => r.Id == user.Id);
                    if (profile != null)
                    {
                        ViewData["ProfilePicture"] = profile.ProfilePicture;
                    }
                }
            }

            return View(categories);
        }

        [HttpPost]
        [Authorize(Roles = "2")]
        public async Task<IActionResult> EditCategories(IFormCollection form)
        {
            var categoryIds = form.Keys
                .Where(k => k.StartsWith("updates[") && k.EndsWith("].Id"))
                .Select(k => int.Parse(k.Split('[', ']')[1]))
                .Distinct();

            foreach (var categoryId in categoryIds)
            {
                var category = await _db.Categories.FindAsync(categoryId);
                if (category != null)
                {
                    var file = form.Files.FirstOrDefault(f => f.Name == $"updates[{categoryId}].Photo");
                    if (file != null && file.Length > 0)
                    {
                        using var memoryStream = new MemoryStream();
                        await file.CopyToAsync(memoryStream);
                        category.Photo = memoryStream.ToArray();
                    }

                    var newName = form[$"updates[{categoryId}].Name"].ToString();
                    if (!string.IsNullOrWhiteSpace(newName))
                    {
                        category.Name = newName;
                    }

                    _db.Categories.Update(category);
                }
            }

            await _db.SaveChangesAsync();
            return RedirectToAction("Category");
        }

        [HttpPost]
        [Authorize(Roles = "2")]
        public async Task<IActionResult> AddCategory(IFormFile photo, string name)
        {
            if (photo != null && !string.IsNullOrWhiteSpace(name))
            {
                using var ms = new MemoryStream();
                await photo.CopyToAsync(ms);
                var category = new Category
                {
                    Name = name,
                    Photo = ms.ToArray()
                };
                _db.Categories.Add(category);
                await _db.SaveChangesAsync();
            }
            return RedirectToAction("Category");
        }

        [HttpPost]
        [Authorize(Roles = "2")]
        public async Task<IActionResult> DeleteCategories([FromForm] List<int> categoryIds)
        {
            if (categoryIds != null && categoryIds.Any())
            {
                var categoriesToDelete = await _db.Categories
                    .Where(c => categoryIds.Contains(c.Id))
                    .ToListAsync();

                var productsToDelete = await _db.Products
                    .Where(p => categoryIds.Contains(p.CategoryId))
                    .ToListAsync();

                _db.Products.RemoveRange(productsToDelete);
                _db.Categories.RemoveRange(categoriesToDelete);

                await _db.SaveChangesAsync();
            }

            return RedirectToAction("Category");
        }

        [Authorize(Roles = "2")]
        public async Task<IActionResult> EditProduct()
        {
            if (User.Identity.IsAuthenticated)
            {
                var user = await _db.User.FirstOrDefaultAsync(u => u.Username == User.Identity.Name);
                if (user != null)
                {
                    var profile = await _db.Users.FirstOrDefaultAsync(r => r.Id == user.Id);
                    if (profile != null)
                    {
                        ViewData["ProfilePicture"] = profile.ProfilePicture;
                    }
                }
            }

            var products = await _db.Products.Include(p => p.Category).ToListAsync();
            var categories = await _db.Categories.ToListAsync();
            ViewData["Categories"] = categories;
            return View(products);
        }

        [HttpPost]
        [Authorize(Roles = "2")]
        public async Task<IActionResult> EditProductDetails(IFormCollection form)
        {
            var productIds = form.Keys
                .Where(k => k.StartsWith("updates[") && k.EndsWith("].Id"))
                .Select(k => int.Parse(k.Split('[', ']')[1]))
                .Distinct();

            foreach (var productId in productIds)
            {
                var product = await _db.Products.FindAsync(productId);
                if (product != null)
                {
                    var file = form.Files.FirstOrDefault(f => f.Name == $"updates[{productId}].Photo");
                    if (file != null && file.Length > 0)
                    {
                        using var memoryStream = new MemoryStream();
                        await file.CopyToAsync(memoryStream);
                        product.Photo = memoryStream.ToArray();
                    }

                    var newName = form[$"updates[{productId}].Name"].ToString();
                    if (!string.IsNullOrWhiteSpace(newName))
                    {
                        product.Name = newName;
                    }

                    product.Price = float.TryParse(form[$"updates[{productId}].Price"], out var price) ? price : product.Price;
                    product.Description = form[$"updates[{productId}].Description"].ToString();
                    product.CategoryId = int.TryParse(form[$"updates[{productId}].CategoryId"], out var categoryId) ? categoryId : product.CategoryId;

                    _db.Products.Update(product);
                }
            }

            await _db.SaveChangesAsync();
            return RedirectToAction("EditProduct");
        }

        [HttpPost]
        [Authorize(Roles = "2")]
        public async Task<IActionResult> AddProduct(IFormFile photo, string name, float price, int categoryId, string description)
        {
            if (photo != null && !string.IsNullOrWhiteSpace(name))
            {
                using var ms = new MemoryStream();
                await photo.CopyToAsync(ms);
                var product = new Product
                {
                    Name = name,
                    Photo = ms.ToArray(),
                    Price = price,
                    Description = description,
                    CategoryId = categoryId
                };
                _db.Products.Add(product);
                await _db.SaveChangesAsync();
            }
            return RedirectToAction("EditProduct");
        }

        [HttpPost]
        [Authorize(Roles = "2")]
        public async Task<IActionResult> DeleteProduct([FromForm] List<int> productIds)
        {
            if (productIds != null && productIds.Any())
            {
                var productsToDelete = await _db.Products
                    .Where(p => productIds.Contains(p.Id))
                    .ToListAsync();

                _db.Products.RemoveRange(productsToDelete);
                await _db.SaveChangesAsync();
            }

            return RedirectToAction("EditProduct");
        }

        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["ReturnUrl"] = returnUrl ?? Url.Action("Index", "Home");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(Login model, string returnUrl = null)
        {
            if (ModelState.IsValid)
            {
                var user = await _db.User.FirstOrDefaultAsync(u => u.Username == model.Username);

                if (user != null && BCrypt.Net.BCrypt.Verify(model.Password, user.Password))
                {
                    if (user.IsBlocked)
                    {
                        return Json(new { success = false, message = "Вас було заблоковано. Зв'яжіться з адміністрацією." });
                    }

                    var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                    return Json(new { success = true, redirectUrl = returnUrl ?? Url.Action("Index") });
                }

                return Json(new { success = false, message = "Невірний логін або пароль." });
            }

            return Json(new { success = false, message = "Помилка введення." });
        }

        public async Task<IActionResult> Logout()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index");
        }

        [HttpPost]
        [Authorize(Roles = "2")]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            var user = await _db.User.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var registration = await _db.Users.FirstOrDefaultAsync(r => r.Id == id);
            if (registration != null)
            {
                _db.Users.Remove(registration);
            }

            _db.User.Remove(user);
            await _db.SaveChangesAsync();
            return RedirectToAction("Customers");
        }

        private byte[] GetDefaultProfilePicture()
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "def.png");
            return System.IO.File.ReadAllBytes(filePath);
        }

        [HttpPost]
        public async Task<IActionResult> ChangeProfilePicture(IFormFile profilePicture, bool? deletePicture)
        {
            if (deletePicture.HasValue && deletePicture.Value)
            {
                var user = await _db.User.FirstOrDefaultAsync(u => u.Username == User.Identity.Name);
                if (user != null)
                {
                    var registration = await _db.Users.FirstOrDefaultAsync(r => r.Id == user.Id);
                    if (registration != null)
                    {
                        registration.ProfilePicture = GetDefaultProfilePicture();
                        await _db.SaveChangesAsync();
                        ViewData["ProfilePicture"] = registration.ProfilePicture;
                    }
                }
            }
            else if (profilePicture != null && profilePicture.Length > 0)
            {
                var user = await _db.User.FirstOrDefaultAsync(u => u.Username == User.Identity.Name);
                if (user != null)
                {
                    var registration = await _db.Users.FirstOrDefaultAsync(r => r.Id == user.Id);
                    if (registration != null)
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            await profilePicture.CopyToAsync(memoryStream);
                            registration.ProfilePicture = memoryStream.ToArray();
                        }

                        await _db.SaveChangesAsync();
                        ViewData["ProfilePicture"] = registration.ProfilePicture;
                    }
                }
            }
            return RedirectToAction("Profile");
        }

        [HttpPost]
        [Authorize(Roles = "2")]
        public async Task<IActionResult> ChangeAdminProfilePicture(IFormFile profilePicture, bool? deletePicture)
        {
            var user = await _db.User.FirstOrDefaultAsync(u => u.Username == User.Identity.Name);
            if (user == null)
            {
                return NotFound();
            }

            var registration = await _db.Users.FirstOrDefaultAsync(r => r.Id == user.Id);
            if (registration == null)
            {
                return NotFound();
            }

            if (deletePicture.HasValue && deletePicture.Value)
            {
                registration.ProfilePicture = GetDefaultProfilePicture();
            }
            else if (profilePicture != null && profilePicture.Length > 0)
            {
                using (var memoryStream = new MemoryStream())
                {
                    await profilePicture.CopyToAsync(memoryStream);
                    registration.ProfilePicture = memoryStream.ToArray();
                }
            }

            await _db.SaveChangesAsync();
            return RedirectToAction("Admin");
        }

        [HttpPost]
        [Authorize(Roles = "2")]
        public IActionResult Block(int id)
        {
            var customer = _db.User.Find(id);
            if (customer == null)
            {
                return NotFound();
            }

            customer.IsBlocked = true;
            _db.SaveChanges();
            return RedirectToAction("Customers");
        }

        [HttpPost]
        [Authorize(Roles = "2")]
        public IActionResult Unblock(int id)
        {
            var customer = _db.User.Find(id);
            if (customer == null)
            {
                return NotFound();
            }

            customer.IsBlocked = false;
            _db.SaveChanges();
            return RedirectToAction("Customers");
        }

        [HttpPost]
        [Authorize(Roles = "2")]
        public async Task<IActionResult> RemoveProfilePicture(int userId)
        {
            var user = await _db.User.FindAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var registration = await _db.Users.FirstOrDefaultAsync(r => r.Id == userId);
            if (registration == null)
            {
                return NotFound();
            }

            registration.ProfilePicture = GetDefaultProfilePicture();
            await _db.SaveChangesAsync();
            return RedirectToAction("Customers");
        }

        [HttpPost]
        [Authorize(Roles = "2")]
        public async Task<IActionResult> EditAdminProfile(Registration model, Login login, string Password, string ConfirmPassword)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Login");
            }

            var user = await _db.User.FirstOrDefaultAsync(u => u.Username == User.Identity.Name);
            if (user == null)
            {
                return NotFound();
            }

            var registration = await _db.Users.FirstOrDefaultAsync(r => r.Id == user.Id);
            if (registration == null)
            {
                return NotFound();
            }

            bool credentialsChanged = false;

            registration.FirstName = model.FirstName;
            registration.LastName = model.LastName;
            registration.MiddleName = model.MiddleName;
            registration.Email = model.Email;
            registration.Phone = model.Phone;
            registration.Address = model.Address;

            if (!string.IsNullOrEmpty(login.Username) && login.Username != user.Username)
            {
                if (await _db.User.AnyAsync(u => u.Username == login.Username && u.Id != user.Id))
                {
                    ModelState.AddModelError("Username", "This username is already taken");
                    return View(model);
                }
                user.Username = login.Username;
                credentialsChanged = true;
            }

            if (!string.IsNullOrEmpty(Password))
            {
                if (Password != ConfirmPassword)
                {
                    ModelState.AddModelError("Password", "Passwords do not match");
                    return View(model);
                }
                user.Password = BCrypt.Net.BCrypt.HashPassword(Password);
                credentialsChanged = true;
            }

            await _db.SaveChangesAsync();

            if (credentialsChanged)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Login", new { message = "Please login with your new credentials" });
            }

            return RedirectToAction("Admin");
        }

        public async Task<IActionResult> AllProducts(int categoryId)
        {
            var products = await _db.Products
                .Include(p => p.Category)
                .Where(p => p.CategoryId == categoryId)
                .ToListAsync();

            ViewData["Category"] = await _db.Categories.FindAsync(categoryId);

            if (User.Identity.IsAuthenticated)
            {
                var user = _db.User.FirstOrDefault(u => u.Username == User.Identity.Name);
                if (user != null)
                {
                    var profile = _db.Users.FirstOrDefault(r => r.Id == user.Id);
                    if (profile != null)
                    {
                        ViewData["ProfilePicture"] = profile.ProfilePicture;
                    }
                }
            }
            return View(products);
        }

        public async Task<IActionResult> DetailItem(int id)
        {
            var product = await _db.Products
         .Include(p => p.Category)
         .Include(p => p.Comments)
         .ThenInclude(c => c.User)
         .FirstOrDefaultAsync(p => p.Id == id);

            if (product == null)
            {
                return NotFound();
            }

            if (User.Identity.IsAuthenticated)
            {
                var user = _db.User.FirstOrDefault(u => u.Username == User.Identity.Name);
                if (user != null)
                {
                    var profile = _db.Users.FirstOrDefault(r => r.Id == user.Id);
                    if (profile != null)
                    {
                        ViewData["ProfilePicture"] = profile.ProfilePicture;
                    }
                }
            }
            ViewBag.Comments = product.Comments
               .OrderByDescending(c => c.DatePosted)
               .Select(c => new
               {
                   c.Id,
                   c.Content,
                   c.DatePosted,
                   Username = c.User.Username
               })
               .ToList();

            return View(product);
        }

        [HttpPost]
        [Authorize(Roles = "1")]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            if (!User.Identity.IsAuthenticated)
            {
                var sessionCart = HttpContext.Session.GetString("Cart") ?? "[]";
                var cartItems = JsonSerializer.Deserialize<List<CartItem>>(sessionCart);

                var existingItem = cartItems.FirstOrDefault(i => i.ProductId == productId);
                if (existingItem != null)
                {
                    existingItem.Quantity += quantity;
                }
                else
                {
                    cartItems.Add(new CartItem
                    {
                        ProductId = productId,
                        Quantity = quantity,
                        DateAdded = DateTime.Now
                    });
                }

                HttpContext.Session.SetString("Cart", JsonSerializer.Serialize(cartItems));
            }
            else
            {
                var userId = User.Identity.Name;
                var cartItem = await _db.CartItems
                    .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId);

                if (cartItem != null)
                {
                    cartItem.Quantity += quantity;
                }
                else
                {
                    cartItem = new CartItem
                    {
                        UserId = userId,
                        ProductId = productId,
                        Quantity = quantity,
                        DateAdded = DateTime.Now
                    };
                    _db.CartItems.Add(cartItem);
                }

                await _db.SaveChangesAsync();
            }

            return Redirect(Request.Headers["Referer"].ToString());
        }

        [Authorize(Roles = "1")]
        public async Task<IActionResult> OrderInfo(int orderId)
        {
            if (User.Identity.IsAuthenticated)
            {
                var user = _db.User.FirstOrDefault(u => u.Username == User.Identity.Name);
                if (user != null)
                {
                    var profile = _db.Users.FirstOrDefault(r => r.Id == user.Id);
                    if (profile != null)
                    {
                        ViewData["ProfilePicture"] = profile.ProfilePicture;
                    }
                }
            }

            var order = await _db.Orders
            .Include(o => o.OrderItems)
            .ThenInclude(oi => oi.Product)
            .Include(o => o.User)
            .FirstOrDefaultAsync(o => o.Id == orderId);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }
    }
}