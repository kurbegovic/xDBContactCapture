using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using xDBCC.Sites.nonSitecore.Models;

namespace xDBCC.Sites.nonSitecore.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Form()
        {
            ViewBag.Message = "Sample form page.";

            var formData = new FormData()
            {
                FirstName = "Joe",
                LastName = "Doe",
                Email = "xdbcontactcapture@testemail.org",
                PostalCode = "90028",
                Country = "USA"
            };

            return View(formData);
        }

        [HttpPost]
        public ActionResult Form(FormData formData)
        {
            return RedirectToAction("Confirmation");
        }

        public ActionResult Confirmation()
        {
            ViewBag.Message = "Sample form confirmation page.";

            return View();
        }
    }
}