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

        [HttpGet]
        public IActionResult Tracking()
        {
            return View();
        }
        [HttpGet]
        public JsonResult GetOrderProgress(string trackingNo)
        {
            try
            {
                DataLayer.Operations dl = new DataLayer.Operations();
                SqlParameter[] parameters = new SqlParameter[]
                {
            new SqlParameter("@Shipmnt_Tracking_No", trackingNo)
                };

                DataTable dt = dl.ExecuteProcedureSelect("GetOrderProgressByTrackingNo", parameters);

                if (dt.Rows.Count > 0)
                {
                    string status = dt.Rows[0]["Shipmnt_Status"].ToString().Trim();

                    // Log status for debugging purposes
                    _logger.LogInformation("Fetched shipment status: {Shipmnt_Status}", status);

                    // Map statuses to step numbers
                    int currentStep = status switch
                    {
                        "Order Placed" => 1,
                        "Shipped" => 2,
                        "Out for Delivery" => 3,
                        "Delivered" => 4,
                        _ => 1 // Default to step 1 if the status is unrecognized
                    };

                    return Json(new { currentStep });
                }

                return Json(new { currentStep = 1 }); // Default to step 1 if no data is found
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching order progress for TrackingNo: {TrackingNo}", trackingNo);
                return Json(new { currentStep = 1 }); // Default to step 1 if there's an error
            }
        }


    }

}

