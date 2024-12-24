using Microsoft.AspNetCore.Mvc;

namespace Inventory.Controllers
{
    public class OrderingController : Controller
    {

        public IActionResult Cart()
        {
            return View();
        }
        public IActionResult OrderDetails()
        {
            return View();
        }
        public IActionResult Payment()
        {
            return View();
        }
        public IActionResult PurchaseOrder()
        {
            return View();
        }
        public IActionResult RawMaterialOrdering()
        {
            return View();
        }
    }
}
