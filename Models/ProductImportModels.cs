using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Inventory.Models
{
    public class ProductImportModel
    {
        [Required(ErrorMessage = "Select an Excel file to upload.")]
        [Display(Name = "Product Excel file")]
        public IFormFile? File { get; set; }

        public List<ProductImportError> Errors { get; set; } = new();
        public int TotalRows { get; set; }
        public int ValidRows { get; set; }
    }

    public class ProductImportError
    {
        public int RowNumber { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class ProductImportParseResult
    {
        public List<ElectronicModel> Products { get; set; } = new();
        public List<ProductImportError> Errors { get; set; } = new();
        public int TotalRows { get; set; }
        public bool IsValid => Errors.Count == 0 && Products.Count > 0;
    }
}
