using System.ComponentModel.DataAnnotations;

namespace Inventory.Models
{
    public class MasterPriceModel : IValidatableObject
    {
        public int Master_Price_Id { get; set; }

        [Required(ErrorMessage = "Select a product.")]
        [Range(1, int.MaxValue, ErrorMessage = "Select a product.")]
        [Display(Name = "Product")]
        public int Electronic_Id { get; set; }

        [Required, StringLength(100)]
        [Display(Name = "Product Category")]
        public string? Product_Category { get; set; }

        public string? Product_Name { get; set; }

        [Required, StringLength(150)]
        [Display(Name = "Product Variant")]
        public string? Product_Variant { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "From quantity must be at least 1.")]
        [Display(Name = "From Quantity")]
        public int From_Quantity { get; set; } = 1;

        [Range(1, int.MaxValue, ErrorMessage = "To quantity must be at least 1.")]
        [Display(Name = "To Quantity")]
        public int To_Quantity { get; set; } = 1;

        [Range(typeof(decimal), "0", "9999999999999999", ErrorMessage = "Enter a valid purchase price.")]
        [Display(Name = "Purchase Price")]
        public decimal Purchase_Price { get; set; }

        [Range(typeof(decimal), "0", "9999999999999999", ErrorMessage = "Enter a valid selling price.")]
        [Display(Name = "Selling Price")]
        public decimal Selling_Price { get; set; }

        public List<MasterPriceModel> Items { get; set; } = new();
        public List<MasterPriceProductOption> Products { get; set; } = new();
        public List<string> Categories { get; set; } = new();

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (To_Quantity < From_Quantity)
                yield return new ValidationResult(
                    "To quantity must be greater than or equal to from quantity.",
                    new[] { nameof(To_Quantity) });
        }
    }

    public class MasterPriceProductOption
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        public string Variant { get; set; } = "";
    }
}
