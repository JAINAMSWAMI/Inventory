using DataLayer;
using Inventory.Models;
using Microsoft.AspNetCore.Mvc;
using System.Data;

namespace Inventory.Controllers
{
    
    public class InventoryControlController : Controller
    {
        private readonly ILogger<InventoryControlController> _logger;

        public InventoryControlController(ILogger<InventoryControlController> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        public ActionResult DeleteElectronic(int id)
        {
            DataLayer.CRUD_Electronic ge = new CRUD_Electronic();

            int result = ge.DeleteElectronic(id);
            if (result > 0)
            {
                _logger.LogInformation("Electronic item with id {Id} deleted successfully.", id);
                return RedirectToAction("GetElectronicData"); // Redirect to the list after deletion
            }
            else
            {
                _logger.LogWarning("Failed to delete electronic item with id {Id}.", id);
                ViewBag.Message = "Failed to delete electronic item. Please try again.";
                return View(); // Return the view with a message, if applicable
            }

        }


        //PRODUCT DETAILS GET METHOD (DatLayer:- GetElectronic Model:- GetElectronicModel View:- Product)

        [HttpGet]
        public IActionResult GetElectronicData()
        {

            DataLayer.Operations dl = new DataLayer.Operations();

            DataLayer.CRUD_Electronic ge = new DataLayer.CRUD_Electronic();

            DataTable dt = ge.SelectElectronicData();

            List<GetElectronicModel> data = new List<GetElectronicModel>();

            foreach (DataRow row in dt.Rows)
            {

                GetElectronicModel item = new GetElectronicModel
                {
                    Electronic_Id = Convert.ToInt32(row["Electronic_Id"]),
                    Electronic_Sub_Category = row["Electronic_Sub_Category"].ToString(),
                    Electronic_Brand = row["Electronic_Brand"].ToString(),
                    Electronic_Name = row["Electronic_Name"].ToString(),
                    Electronic_Price = Convert.ToInt32(row["Electronic_Price"]),
                    Electronic_CRStock = Convert.ToInt32(row["Electronic_CRStock"]),
                    Electronic_Warrenty_Period = Convert.ToDateTime(row["Electronic_Warrenty_Period"]),
                    Electronic_Specs = row["Electronic_Specs"].ToString(),
                    Electronic_Supplier = row["Electronic_Supplier"].ToString(),
                    Electronic_Status = row["Electronic_Status"].ToString(),

                };



                data.Add(item);
            }



            var model = new GetElectronicModel { Data = data };


            return View(model);

        }


        // ADD CATEGORY POST METHOD(DataLayer:- AddCategory Model:- AddCategoryModel View:- AddCategory)
        public ActionResult AddCategory()
        {
            return View();
        }

        [HttpPost]
        public IActionResult AddCategory(AddCategoryModel addcategoryModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    AddCategory AD = new AddCategory
                    {

                        Category_Added_Date = addcategoryModel.Category_Added_Date,
                        Category_Status = addcategoryModel.Category_Status,
                        Category_Type = addcategoryModel.Category_Type
                    };

                    int result = AD.NewCategory();

                    if (result > 0)
                    {
                        _logger.LogInformation("Item added successfully.");
                        return RedirectToAction("GetElectronicData", "InventoryControl");
                    }
                    else
                    {
                        _logger.LogWarning("Failed to add Item.");
                        ViewBag.Message = "Failed to add category. Please try again.";
                        return View(addcategoryModel);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while adding the category.");
                    ViewBag.Message = "An unexpected error occurred. Please try again later.";
                    return View(addcategoryModel);
                }
            }
            else
            {
                return View(addcategoryModel);
            }
        }




        //ADD PRODUCT POST MEHTOD(DataLayer:- AddElectronic,Model:- AddElectronicModel,View:-  AddElectronic)

        public ActionResult AddElectronic()
        {
            return View();
        }

        [HttpPost]
        public IActionResult AddElectronic(ElectronicModel electronicModel)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    CRUD_Electronic AE = new CRUD_Electronic
                    {
                        Electronic_Sub_Category = electronicModel.Electronic_Sub_Category,
                        Electronic_Brand = electronicModel.Electronic_Brand,
                        Electronic_Name = electronicModel.Electronic_Name,
                        Electronic_Price = electronicModel.Electronic_Price,
                        Electronic_CRStock = electronicModel.Electronic_CRStock,
                        Electronic_Warrenty_Period = electronicModel.Electronic_Warrenty_Period,
                        Electronic_Specs = electronicModel.Electronic_Specs,
                        Electronic_Supplier = electronicModel.Electronic_Supplier,
                        Electronic_Status = electronicModel.Electronic_Status
                    };

                    int result = AE.AddNewElectronic();

                    if (result > 0)
                    {
                        _logger.LogInformation("Category added successfully.");
                        return RedirectToAction("GetElectronicData", "InventoryControl");
                    }
                    else
                    {
                        _logger.LogWarning("Failed to add Electronic Item.");
                        ViewBag.Message = "Failed to add category. Please try again.";
                        return View(electronicModel);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "An error occurred while adding the category.");
                    ViewBag.Message = "An unexpected error occurred. Please try again later.";
                    return View(electronicModel);
                }
            }
            else
            {
                return View(electronicModel);
            }

        }

        [HttpGet, HttpPost]
        public IActionResult EditElectronic(int id, ElectronicModel? electronicModel = null)
        {
            if (HttpContext.Request.Method == "GET")
            {
                DataLayer.CRUD_Electronic ge = new DataLayer.CRUD_Electronic();

                DataTable dt = ge.SelectElectronicData();
                foreach (DataRow row in dt.Rows)
                {
                    if (Convert.ToInt32(row["Electronic_Id"]) == id)
                    {
                        electronicModel = new ElectronicModel
                        {
                            Electronic_Id = Convert.ToInt32(row["Electronic_Id"]),
                            Electronic_Sub_Category = row["Electronic_Sub_Category"].ToString(),
                            Electronic_Brand = row["Electronic_Brand"].ToString(),
                            Electronic_Name = row["Electronic_Name"].ToString(),
                            Electronic_Price = Convert.ToInt32(row["Electronic_Price"]),
                            Electronic_CRStock = Convert.ToInt32(row["Electronic_CRStock"]),
                            Electronic_Warrenty_Period = Convert.ToDateTime(row["Electronic_Warrenty_Period"]),
                            Electronic_Specs = row["Electronic_Specs"].ToString(),
                            Electronic_Supplier = row["Electronic_Supplier"].ToString(),
                            Electronic_Status = row["Electronic_Status"].ToString()
                        };
                        break;
                    }
                }

                if (electronicModel == null)
                {
                    _logger.LogWarning("Electronic item with id {Id} not found.", id);
                    return NotFound();
                }

                return View(electronicModel);
            }
            else if (HttpContext.Request.Method == "POST")
            {
                if (ModelState.IsValid)
                {
                    try
                    {
                        CRUD_Electronic ge = new CRUD_Electronic
                        {
                            Electronic_Id = electronicModel.Electronic_Id,
                            Electronic_Sub_Category = electronicModel.Electronic_Sub_Category,
                            Electronic_Brand = electronicModel.Electronic_Brand,
                            Electronic_Name = electronicModel.Electronic_Name,
                            Electronic_Price = electronicModel.Electronic_Price,
                            Electronic_CRStock = electronicModel.Electronic_CRStock,
                            Electronic_Warrenty_Period = electronicModel.Electronic_Warrenty_Period,
                            Electronic_Specs = electronicModel.Electronic_Specs,
                            Electronic_Supplier = electronicModel.Electronic_Supplier,
                            Electronic_Status = electronicModel.Electronic_Status
                        };

                        int result = ge.EditRecord(electronicModel.Electronic_Id);

                        if (result > 0)
                        {
                            _logger.LogInformation("Item Updated Successfully.");
                            return RedirectToAction("GetElectronicData");
                        }
                        else
                        {
                            _logger.LogWarning("Failed to Update Item.");
                            ViewBag.Message = "Failed to update electronic item. Please try again.";
                            return View(electronicModel);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "An error occurred while updating the electronic item.");
                        ViewBag.Message = "An unexpected error occurred. Please try again later.";
                        return View(electronicModel);
                    }
                }
                else
                {
                    _logger.LogWarning("Model state is invalid.");
                    return View(electronicModel);
                }
            }

            return BadRequest(); // Should not reach here
        }


    }
}
