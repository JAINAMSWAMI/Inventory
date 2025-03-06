using DataLayer;
using Inventory.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
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
            
                    Order_No = row["Order_No"].ToString(),
                    Order_Date = DateOnly.FromDateTime(Convert.ToDateTime(row["Order_Date"])),
                    Order_Total_Cost =Convert.ToInt32( row["Order_Total_Cost"]),  
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
                       
                        "Processing" => 1,
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




        public IActionResult DistributionCenter()
        {
            return View();
        }


        public ActionResult CreateOrder()
        {
            return View();
        }

        [HttpPost]
        public IActionResult CreateOrder(CreateOrderModel createordermodel)
        {
            if (createordermodel == null)
            {
                _logger.LogError("CreateOrderModel is null.");
                return BadRequest("Invalid request.");
            }

            createordermodel.Order_No = "ORD-" + DateTime.Now.ToString("yyyyMMddHHmmss") + "-" + new Random().Next(1000, 9999);
            createordermodel.Shipmnt_Tracking_No = "TRK-" + Guid.NewGuid().ToString("N").Substring(0, 12).ToUpper();

            AddOrder AO = new AddOrder
            {
                Order_No = createordermodel.Order_No,
                Order_Date = createordermodel.Order_Date,
                Payment_Type = createordermodel.Payment_Type,
                Payment_Status = createordermodel.Payment_Status,
                Order_Total_Cost = createordermodel.Order_Total_Cost,
                Shipping_Address = createordermodel.Shipping_Address,
                Delivery_Expected_Date = createordermodel.Delivery_Expected_Date,
                Shipmnt_Tracking_No = createordermodel.Shipmnt_Tracking_No,
                Shipmnt_Status = createordermodel.Shipmnt_Status,
                Warehouse_Name = createordermodel.Warehouse_Name,
                Shipping_Company = createordermodel.Shipping_Company
            };

            try
            {
                int result = AO.AddOrderDetails();
                if (result <= 0)
                {
                    _logger.LogError("Database error while inserting the order.");
                    ModelState.AddModelError("", "Database error. Please try again.");
                    return View(createordermodel);
                }

                return RedirectToAction("OrderSuccess");  // Redirect after successful order creation
            }
            catch (Exception ex)
            {
                _logger.LogError($"Exception: {ex.Message}");
                return View(createordermodel);
            }
        }



        public IActionResult OrderSuccess()
        {
            return View();
        }





    }

}

