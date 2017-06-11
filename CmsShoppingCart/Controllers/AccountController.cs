using CmsShoppingCart.Models.Data;
using CmsShoppingCart.Models.ViewModels.Account;
using CmsShoppingCart.Models.ViewModels.Cart;
using CmsShoppingCart.Models.ViewModels.Shop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;

namespace CmsShoppingCart.Controllers
{
    public class AccountController : Controller
    {
        // GET: Account
        public ActionResult Index()
        {
            return Redirect("~/account/login");
        }

        //GET: /account/login
        [HttpGet]
        public ActionResult Login()
        {
            //Confirm user is not logged in
            string username = User.Identity.Name;

            if (!string.IsNullOrEmpty(username))
                return RedirectToAction("user-profile");

            //return view
            return View();
        }

        //POST: /account/login
        [HttpPost]
        public ActionResult Login(LoginUserVM model)
        {
            //Check model state
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            //Check if user is valid
            bool isValid = false;
            using (Db db = new Db())
            {
                if (db.Users.Any(x => x.Username.Equals(model.Username) && x.Password.Equals(model.Password)))
                {
                    isValid = true;
                }
            }

            if (!isValid)
            {
                ModelState.AddModelError("", "Invalid username or password.");
                return View(model);
            }
            else
            {
                FormsAuthentication.SetAuthCookie(model.Username, model.RememberMe);
                return Redirect(FormsAuthentication.GetRedirectUrl(model.Username, model.RememberMe));
            }
        }

        // GET: /account/create-account
        [ActionName("create-account")]
        [HttpGet]
        public ActionResult CreateAccount()
        {
            return View("CreateAccount");
        }

        // POST: /account/create-account
        [ActionName("create-account")]
        [HttpPost]
        public ActionResult CreateAccount(UserVM model)
        {
            //Check model state
            if (!ModelState.IsValid)
            {
                return View("CreateAccount", model);
            }

            //Check if passwords match
            if (!model.Password.Equals(model.ConfirmPassword))
            {
                ModelState.AddModelError("", "Passwords do not match.");
                return View("CreateAccount", model);
            }
            using (Db db = new Db())
            {
                //Make sure username is unique
                if (db.Users.Any(x => x.Username.Equals(model.Username)))
                {
                    ModelState.AddModelError("", "Username " + model.Username + " is already taken.");
                    model.Username = "";
                    return View("CreateAccount", model);
                }

                //create userDTO
                UserDTO userDTO = new UserDTO()
                {
                    Username = model.Username,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    EmailAddress = model.EmailAddress,
                    Password = model.Password
                };

                //add the userDTO
                db.Users.Add(userDTO);
                db.SaveChanges();

                //get userId
                int userId = userDTO.Id;

                //create userRolesDTO
                UserRoleDTO userRoleDTO = new UserRoleDTO()
                {
                    UserId = userId,
                    RoleId = 2
                };

                //add userRoleDTO
                db.UserRoles.Add(userRoleDTO);
                db.SaveChanges();
            }

            //create TempData message
            TempData["SM"] = "You are now registered and can login";

            return Redirect("~/account/login");
        }

        //GET: /account/Logout
        [HttpGet]
        [Authorize]
        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            return Redirect("~/account/login");
        }

        [Authorize]
        public ActionResult UserNavPartial()
        {
            //Get the username
            string userName = User.Identity.Name;

            //Declare model
            UserNavPartialVM model;
            using (Db db = new Db())
            {
                //Get the user
                UserDTO dto = db.Users.FirstOrDefault(x => x.Username.Equals(userName));

                //Build the model
                model = new UserNavPartialVM()
                {
                    FirstName = dto.FirstName,
                    LastName = dto.LastName
                };
            }

            //Return partialview with the model
            return PartialView(model);
        }

        //GET: /account/user-profile
        [HttpGet]
        [Authorize]
        [ActionName("user-profile")]
        public ActionResult UserProfile()
        {
            //Get username
            string userName = User.Identity.Name;

            //Declare model
            UserProfileVM model;

            using (Db db = new Db())
            {
                //Get user
                UserDTO dto = db.Users.FirstOrDefault(x => x.Username.Equals(userName));

                //Build model
                model = new UserProfileVM(dto);

            }

            //Return view with model
            return View("UserProfile", model);
        }

        //POST: /account/user-profile
        [HttpPost]
        [ActionName("user-profile")]
        public ActionResult UserProfile(UserProfileVM model)
        {
            //Check model state
            if (!ModelState.IsValid)
            {
                return View("UserProfile", model);
            }

            //Check if password match if need be
            if (!string.IsNullOrWhiteSpace(model.Password))
            {
                if (!model.Password.Equals(model.ConfirmPassword))
                {
                    ModelState.AddModelError("", "Passwords do not match.");
                    return View("UserProfile", model);
                }
            }
            using (Db db = new Db())
            {
                //Get username
                string userName = User.Identity.Name;

                //Make sure username is unique
                if (db.Users.Where(x => x.Id != model.Id).Any(x => x.Username.Equals(userName)))
                {
                    ModelState.AddModelError("", "Username " + model.Username + " already exist.");
                    model.Username = "";
                    return View("UserProfile", model);
                }

                //Edit DTO
                UserDTO dto = db.Users.Find(model.Id);
                dto.FirstName = model.FirstName;
                dto.LastName = model.LastName;
                dto.EmailAddress = model.EmailAddress;
                dto.Username = model.Username;

                if (!string.IsNullOrWhiteSpace(model.Password))
                {
                    dto.Password = model.Password;
                }

                db.SaveChanges();
            }

            //Set TempData Message
            TempData["SM"] = "You have edited your profile!";

            return Redirect("~/account/user-profile");
        }

        //GET: /account/Orders
        [Authorize(Roles="User")]
        public ActionResult Orders()
        {
            //Init list of OrdersForUserVM
            List<OrdersForUserVM> orderForUser = new List<OrdersForUserVM>();
            using (Db db=new Db())
            {
                //Get user id
                UserDTO user = db.Users.Where(x => x.Username == User.Identity.Name).FirstOrDefault();
                int userId = user.Id;
                //Init list of OrderVM
                List<OrderVM> orders = db.Orders.Where(x => x.UserId == userId).ToArray().Select(x => new OrderVM(x)).ToList();
                //loop through list of orderVM
                foreach (var order in orders)
                {
                    //Init product dict
                    Dictionary<string, int> productsAndQty = new Dictionary<string, int>();
                    //Declare total
                    decimal total=0m;

                    //Init list of orderDetailsDTO
                    List<OrderDetailsDTO> orderDetailsDTO = db.OrderDetails.Where(x => x.OrderId == order.OrderId).ToList();

                    //loop through list of orderDetailsDTO
                    foreach (var orderDetails in orderDetailsDTO)
                    {
                        //Get product
                        ProductDTO product = db.Products.Where(x => x.Id == orderDetails.ProductId).FirstOrDefault();

                        //Get product price
                        decimal price=product.Price;

                        //Get product name
                        string productName = product.Name;

                        //Add to products dict
                        productsAndQty.Add(productName, orderDetails.Quantity);

                        //Get total
                        total += orderDetails.Quantity * price;                       
                    }
                    //Add to OrdersForUserVM list
                    orderForUser.Add(new OrdersForUserVM()
                    {
                        OrderNumber = order.OrderId,
                        Total = total,
                        ProductsAndQty = productsAndQty,
                        CreatedAt = order.CreatedAt
                    });
                }
            }
            return View(orderForUser);
        }
    }
}