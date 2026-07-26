using DataLayer;
using Inventory.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Data;

namespace Inventory.Controllers
{
    [Authorize]
    public class LookupController : Controller
    {
        private readonly ILogger<LookupController> _logger;

        public LookupController(ILogger<LookupController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public JsonResult Cities(int stateId)
        {
            var list = new List<object>();
            if (stateId <= 0) return Json(list);

            try
            {
                foreach (DataRow row in new Lookup().GetCitiesByState(stateId).Rows)
                {
                    list.Add(new
                    {
                        id = Convert.ToInt32(row["City_Id"]),
                        name = row["City_Name"]?.ToString()
                    });
                }
            }
            catch (Exception ex)
            {
                ControllerError.CaptureWarning(this, _logger, ex,
                    "Failed to load cities for state {StateId}",
                    stateId);
            }

            return Json(list);
        }

        [HttpGet]
        public JsonResult ProductPrice(int id)
        {
            try
            {
                var dt = new CRUD_Electronic().GetElectronicById(id);
                if (dt.Rows.Count == 0)
                    return Json(new { price = 0, stock = 0, name = "" });

                var row = dt.Rows[0];
                return Json(new
                {
                    price = Convert.ToInt32(row["Electronic_Price"]),
                    stock = Convert.ToInt32(row["Electronic_CRStock"]),
                    name = row["Electronic_Name"]?.ToString()
                });
            }
            catch (Exception ex)
            {
                ControllerError.CaptureWarning(this, _logger, ex,
                    "Failed to load product price for {ProductId}",
                    id);
                return Json(new { price = 0, stock = 0, name = "" });
            }
        }
    }
}
