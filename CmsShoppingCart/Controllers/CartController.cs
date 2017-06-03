using CmsShoppingCart.Models.Data;
using CmsShoppingCart.Models.ViewModels.Cart;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web;
using System.Web.Mvc;

namespace CmsShoppingCart.Controllers
{
    public class CartController : Controller
    {
        // GET: Cart
        public ActionResult Index()
        {
            //Init the cart list
            var cart = Session["cart"] as List<CartVM> ?? new List<CartVM>();

            //Check if cart is empty
            if (cart.Count == 0 || Session["cart"] == null)
            {
                ViewBag.Message = "Your cart is empty.";
                return View();
            }

            //Calculate the total and save to ViewBag
            decimal total = 0m;
            foreach (var item in cart)
            {
                total += item.Total;
            }
            ViewBag.GrandTotal = total;

            //return view with list
            return View(cart);
        }
        public ActionResult CartPartial()
        {
            //Init CartVM
            CartVM model = new CartVM();

            //Init Quantity
            int qty = 0;

            //Init Price
            decimal price = 0m;

            //Check for cart session
            if (Session["cart"] != null)
            {
                //Get quantity and price
                var list = (List<CartVM>)Session["cart"];
                foreach (var item in list)
                {
                    qty += item.Quantity;
                    price += item.Quantity * item.Price;
                }
                model.Quantity = qty;
                model.Price = price;
            }
            else
            {
                model.Quantity = 0;
                model.Price = 0m;
            }
            //Return partial view with model
            return PartialView(model);
        }

        public ActionResult AddToCartPartial(int id)
        {
            //Init CartVM list
            List<CartVM> cart = Session["cart"] as List<CartVM> ?? new List<CartVM>();

            //Int CartVM
            CartVM model = new CartVM();
            using (Db db = new Db())
            {
                //Get the product
                ProductDTO product = db.Products.Find(id);

                //Check if the product is already in cart
                var productInCart = cart.FirstOrDefault(x => x.ProductId == id);

                //If not, add new
                if (productInCart == null)
                {
                    cart.Add(new CartVM()
                    {
                        ProductId = product.Id,
                        ProductName = product.Name,
                        Quantity = 1,
                        Price = product.Price,
                        Image = product.ImageName
                    });
                }
                else
                {
                    //If it is, increment
                    productInCart.Quantity++;
                }
            }
            //Get total qty and price and add to model
            int qty = 0;
            decimal price = 0m;
            foreach (var item in cart)
            {
                qty += item.Quantity;
                price += item.Quantity * item.Price;
            }
            model.Quantity = qty;
            model.Price = price;

            //Save cart back to session
            Session["cart"] = cart;


            //Return partial view with model
            return PartialView(model);
        }

        //GET: /Cart/IncrementProduct
        public JsonResult IncrementProduct(int productId)
        {
            //Init cart list
            List<CartVM> cart = Session["cart"] as List<CartVM>;

            using (Db db = new Db())
            {
                //Get CartVM from list
                CartVM model = cart.FirstOrDefault(x => x.ProductId == productId);

                //Increment qty
                model.Quantity++;

                //store needed data
                var result = new { qty = model.Quantity, price = model.Price };

                //Return json with data
                return Json(result, JsonRequestBehavior.AllowGet);
            }
        }

        //GET: /Cart/DecrementProduct
        public JsonResult DecrementProduct(int productId)
        {
            //Init cart list
            List<CartVM> cart = Session["cart"] as List<CartVM>;

            using (Db db = new Db())
            {
                //Get CartVM from list
                CartVM model = cart.FirstOrDefault(x => x.ProductId == productId);

                //Decrement qty
                if (model.Quantity > 1)
                {
                    model.Quantity--;
                }
                else
                {
                    model.Quantity=0;
                    cart.Remove(model);
                }

                //store needed data
                var result = new { qty = model.Quantity, price = model.Price };

                //Return json with data
                return Json(result, JsonRequestBehavior.AllowGet);
            }
        }

        //GET: /Cart/RemoveProduct
        public void RemoveProduct(int productId)
        {
            //Init CartVM list
            List<CartVM> cart = Session["cart"] as List<CartVM>;

            using (Db db=new Db())
            {
                //Get model from list
                CartVM model = cart.FirstOrDefault(x => x.ProductId==productId);

                //remove from the list
                cart.Remove(model);
            }

        }

        //GET: /Cart/PaypalPartial
        public ActionResult PaypalPartial()
        {
            List<CartVM> cart = Session["cart"] as List<CartVM>;
            return PartialView(cart);
        }

        //POST: /Cart/PlaceOrder
        [HttpPost]
        public void PlaceOrder()
        {
            //Get cart list
            List<CartVM> cart = Session["cart"] as List<CartVM>;

            //Get username
            string username = User.Identity.Name;

            //Declare orderId
            int orderId = 0;

            using (Db db=new Db())
            {
                //Init OrderDTO
                OrderDTO orderDTO = new OrderDTO();

                //Get user Id
                var q = db.Users.FirstOrDefault(x => x.Username.Equals(username));
                int userId = q.Id;

                //Add to OrderDTO and save
                orderDTO.UserId = userId;
                orderDTO.CreatedAt = DateTime.Now;

                db.Orders.Add(orderDTO);

                db.SaveChanges();

                //Get inserted id
                orderId = orderDTO.OrderId;

                //Init OrderDetailsDTO
                OrderDetailsDTO orderDetailsDTO = new OrderDetailsDTO();

                //Add to OrderDetailsDTO
                foreach (var item in cart)
                {
                    orderDetailsDTO.OrderId = orderId;
                    orderDetailsDTO.UserId = userId;
                    orderDetailsDTO.ProductId = item.ProductId;
                    orderDetailsDTO.Quantity = item.Quantity;
                    db.OrderDetails.Add(orderDetailsDTO);
                    db.SaveChanges();
                }
               
                
            }

            //Email Admin
            var client = new SmtpClient("smtp.mailtrap.io", 2525)
            {
                Credentials = new NetworkCredential("e65890402c8129", "66c52ad3b4baf9"),
                EnableSsl = true
            };
            client.Send("admin@example.com", "admin@example.com", "New Order", "You have a new order. Order number is: "+orderId);

            //Reset session
            Session["cart"] = null;
        }
    }
}