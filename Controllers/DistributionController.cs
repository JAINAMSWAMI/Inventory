using Inventory.Models;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Data.SqlClient;

namespace Inventory.Controllers
{
    public class DistributionController : Controller
    {
        private readonly ILogger<DistributionController> _logger;

        public DistributionController(ILogger<DistributionController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public IActionResult ViewShipment()
        {
            DataLayer.Operations dl = new DataLayer.Operations();
            DataLayer.GetShipmentDetails GS = new DataLayer.GetShipmentDetails();


            DataTable dt = GS.SelectShippingDetails();

            List<GetShipmentDetailsModel> data = new List<GetShipmentDetailsModel>();


            foreach (DataRow row in dt.Rows)
            {
                GetShipmentDetailsModel item = new GetShipmentDetailsModel
                {
                    Shipmnt_Id = Convert.ToInt32(row["Shipmnt_Id"]),
                    Order_No = row["Order_No"].ToString(),
                    Pickup_Date = DateOnly.FromDateTime(Convert.ToDateTime(row["Pickup_Date"])),
                    Dispatch_Date = DateOnly.FromDateTime(Convert.ToDateTime(row["Dispatch_Date"])),
                    Delivery_Expected_Date = DateOnly.FromDateTime(Convert.ToDateTime(row["Delivery_Expected_Date"])),
                    Shipmnt_Status = row["Shipmnt_Status"].ToString(),
                    Warehouse_Name = row["Warehouse_Name"].ToString(),
                    Shipping_Company = row["Shipping_Company"].ToString(),
                    Shipmnt_Tracking_No = row["Shipmnt_Tracking_No"].ToString()
                };

                data.Add(item);
            }


            var model = new GetShipmentDetailsModel { Items = data };
            return View(model);
        }

    }
}