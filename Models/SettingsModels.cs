using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Inventory.Models
{
    public class SettingsPageModel
    {
        public List<UserListItem> Users { get; set; } = new();
        public List<RoleItem> Roles { get; set; } = new();
        public List<ModuleItem> Modules { get; set; } = new();
        public CreateUserModel NewUser { get; set; } = new();
        public PrefixSettingsModel Prefixes { get; set; } = new();
        public List<CompanyListItem> Companies { get; set; } = new();
        public CompanyEditModel NewCompany { get; set; } = new();
        public RoleEditModel NewRole { get; set; } = new();
        public LookupPageModel Lookups { get; set; } = new();
        public List<NumberFormatRow> NumberFormats { get; set; } = new();
        public string ActiveSection { get; set; } = "hub";
    }

    public class NumberFormatRow
    {
        public int Format_Id { get; set; }
        public string Form_Key { get; set; } = string.Empty;
        public string Form_Name { get; set; } = string.Empty;
        public string? Location_Code { get; set; }
        public string? Business_Segment { get; set; }
        public string? Business_Category { get; set; }
        public string? Business_SubCategory { get; set; }
        public string Prefix { get; set; } = string.Empty;
        public int Starting_Number { get; set; } = 1;
        public int Next_Number { get; set; } = 1;
    }

    public class UserListItem
    {
        public int User_Id { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public int? Role_Id { get; set; }
        public string? Role_Name { get; set; }
        public string? Status { get; set; }
        public string? Phone { get; set; }
        public DateTime Created_Date { get; set; }

        public string DisplayName =>
            string.IsNullOrWhiteSpace(FirstName) && string.IsNullOrWhiteSpace(LastName)
                ? Email ?? "User"
                : $"{FirstName} {LastName}".Trim();
    }

    public class RoleItem
    {
        public int Role_Id { get; set; }
        public string? Role_Name { get; set; }
        public string? Description { get; set; }
        public bool Is_System { get; set; }
        public List<string> ModuleKeys { get; set; } = new();
    }

    public class RoleEditModel
    {
        [Required, StringLength(50), Display(Name = "Role name")]
        public string Role_Name { get; set; } = string.Empty;

        [StringLength(200), Display(Name = "Description")]
        public string? Description { get; set; }
    }

    public class ModuleItem
    {
        public int Module_Id { get; set; }
        public string Module_Key { get; set; } = string.Empty;
        public string Module_Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Module_Group { get; set; } = string.Empty;
        public string? Icon_Class { get; set; }
        public int Display_Order { get; set; }
    }

    public class CreateUserModel
    {
        [Required, StringLength(100), Display(Name = "First name")]
        public string FirstName { get; set; } = string.Empty;

        [Required, StringLength(100), Display(Name = "Last name")]
        public string LastName { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(200)]
        public string Email { get; set; } = string.Empty;

        [Required, StringLength(100, MinimumLength = 6), DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Required, Display(Name = "Role")]
        public int Role_Id { get; set; }

        [Phone, StringLength(50)]
        public string? Phone { get; set; }

        public string Status { get; set; } = "Active";
    }

    public class PrefixSettingsModel
    {
        [Required, StringLength(20), Display(Name = "Customer order")]
        public string OrderPrefix { get; set; } = "ORD-";

        [Required, StringLength(20), Display(Name = "Tracking")]
        public string TrackingPrefix { get; set; } = "TRK-";

        [Required, StringLength(20), Display(Name = "GRN")]
        public string GRNPrefix { get; set; } = "GRN-";

        [Required, StringLength(20), Display(Name = "DCN / Invoice dispatch")]
        public string DCNPrefix { get; set; } = "DCN-";

        [Required, StringLength(20), Display(Name = "Product / SKU")]
        public string ProductPrefix { get; set; } = "PRD-";

        [Required, StringLength(20), Display(Name = "Invoice")]
        public string InvoicePrefix { get; set; } = "INV-";

        [Required, StringLength(20), Display(Name = "Customer code")]
        public string CustomerPrefix { get; set; } = "CUS-";

        [Required, StringLength(20), Display(Name = "Quotation")]
        public string QuotePrefix { get; set; } = "QTE-";

        [StringLength(100), Display(Name = "Company display name")]
        public string? CompanyName { get; set; } = "Fluxx";
    }

    public class CompanyListItem
    {
        public Guid CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string? Gstin { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? AddressLine { get; set; }
        public string? LogoUrl { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CompanyEditModel
    {
        [Required, StringLength(200), Display(Name = "Company name")]
        public string CompanyName { get; set; } = string.Empty;

        [StringLength(30), Display(Name = "GSTIN")]
        public string? Gstin { get; set; }

        [EmailAddress, StringLength(200)]
        public string? Email { get; set; }

        [Phone, StringLength(50)]
        public string? Phone { get; set; }

        [StringLength(500), Display(Name = "Address")]
        public string? AddressLine { get; set; }

        [Display(Name = "Invoice logo")]
        public IFormFile? LogoFile { get; set; }
    }

    public class LookupPageModel
    {
        public List<LookupCategoryItem> Categories { get; set; } = new();
        public List<LookupValueItem> Items { get; set; } = new();
        public List<LookupStateItem> States { get; set; } = new();
        public List<LookupCityItem> Cities { get; set; } = new();
        public int? SelectedCategoryId { get; set; }
        public string ActiveTab { get; set; } = "values";
        public LookupCategoryEditModel NewCategory { get; set; } = new();
        public LookupValueEditModel NewItem { get; set; } = new();
        public LookupStateEditModel NewState { get; set; } = new();
        public LookupCityEditModel NewCity { get; set; } = new();
    }

    public class LookupCategoryItem
    {
        public int Category_Id { get; set; }
        public string Category_Key { get; set; } = string.Empty;
        public string Category_Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    public class LookupValueItem
    {
        public int Item_Id { get; set; }
        public int Category_Id { get; set; }
        public string Category_Key { get; set; } = string.Empty;
        public string Category_Name { get; set; } = string.Empty;
        public string? Item_Code { get; set; }
        public string Item_Label { get; set; } = string.Empty;
        public int Sort_Order { get; set; }
        public bool Is_Active { get; set; }
    }

    public class LookupCategoryEditModel
    {
        [Required, StringLength(50), Display(Name = "Key (no spaces)")]
        public string Category_Key { get; set; } = string.Empty;

        [Required, StringLength(100), Display(Name = "Display name")]
        public string Category_Name { get; set; } = string.Empty;

        [StringLength(300)]
        public string? Description { get; set; }
    }

    public class LookupValueEditModel
    {
        [Required, Display(Name = "Category")]
        public int Category_Id { get; set; }

        [StringLength(50), Display(Name = "Code")]
        public string? Item_Code { get; set; }

        [Required, StringLength(200), Display(Name = "Label")]
        public string Item_Label { get; set; } = string.Empty;

        [Display(Name = "Sort order")]
        public int Sort_Order { get; set; }
    }

    public class LookupStateItem
    {
        public int State_Id { get; set; }
        public string State_Name { get; set; } = string.Empty;
        public string? State_Code { get; set; }
    }

    public class LookupCityItem
    {
        public int City_Id { get; set; }
        public int State_Id { get; set; }
        public string City_Name { get; set; } = string.Empty;
        public string? State_Name { get; set; }
    }

    public class LookupStateEditModel
    {
        [Required, StringLength(100), Display(Name = "State name")]
        public string State_Name { get; set; } = string.Empty;

        [StringLength(10), Display(Name = "Code")]
        public string? State_Code { get; set; }
    }

    public class LookupCityEditModel
    {
        [Required, Display(Name = "State")]
        public int State_Id { get; set; }

        [Required, StringLength(120), Display(Name = "City name")]
        public string City_Name { get; set; } = string.Empty;
    }

    public class AutomationHubViewModel
    {
        public int ActiveWorkflows { get; set; }
        public int LowStockAlerts { get; set; }
        public int ActiveUsers { get; set; }
    }
}
