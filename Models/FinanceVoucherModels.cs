using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Inventory.Models
{
    public enum PaymentEntryType
    {
        Payment = 0,
        Settlement = 1
    }

    /// <summary>Shared create model for Payment and Expense (identical fields except allocation shape).</summary>
    public class FinanceVoucherCreateModel
    {
        public PaymentEntryType PaymentType { get; set; } = PaymentEntryType.Payment;
        public bool IsSettlement => PaymentType == PaymentEntryType.Settlement;

        [Required, Display(Name = "Billing company")]
        public Guid BillingCompanyId { get; set; }

        [Required, Display(Name = "Payment date")]
        public DateTime PaymentDate { get; set; } = DateTime.Now;

        [Required, Display(Name = "Payment mode")]
        public int PaymentModeId { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [Required, Display(Name = "Party")]
        public int PartyId { get; set; }

        [Required, Display(Name = "Payment account")]
        public int PaymentAccountId { get; set; }

        /// <summary>Read-only display; not posted as source of truth.</summary>
        public string? PartyBalanceDisplay { get; set; }

        [Range(0, double.MaxValue), Display(Name = "Gross amount")]
        public decimal? TaxableAmount { get; set; }

        [Display(Name = "Discount type")]
        public string DiscountType { get; set; } = "Percent";

        [Range(0, double.MaxValue), Display(Name = "Discount")]
        public decimal DiscountValue { get; set; }

        public decimal DiscountAmount { get; set; }

        [Display(Name = "Tax %")]
        public decimal TaxPercent { get; set; }

        public decimal TaxAmount { get; set; }

        [Required, Range(0.0001, double.MaxValue), Display(Name = "Payment amount")]
        public decimal PaymentAmount { get; set; }

        [StringLength(100), Display(Name = "Reference no.")]
        public string? ReferenceNo { get; set; }

        public int? BusinessSegmentId { get; set; }
        public int? BusinessCategoryId { get; set; }
        public int? BusinessSubCategoryId { get; set; }

        public List<PaymentAllocationRowModel> Allocations { get; set; } = new();
        public List<ExpenseAllocationRowModel> ExpenseAllocations { get; set; } = new();

        /// <summary>Legacy multi-row tax lines — kept for DB writes; UI uses TaxPercent only.</summary>
        public List<TaxComponentRowModel> TaxLines { get; set; } = new();

        public List<IFormFile>? UploadDocuments { get; set; }

        public decimal TotalAllocated =>
            (Allocations?.Sum(a => a.AllocatedAmount) ?? 0m)
            + (ExpenseAllocations?.Sum(a => a.AllocatedAmount) ?? 0m);

        public decimal TotalTax => TaxAmount > 0 ? TaxAmount : (TaxLines?.Sum(t => t.TaxAmount) ?? 0m);

        public decimal FinalAmount => PaymentAmount - (
            (Allocations?.Sum(a => a.AllocatedAmount) ?? 0m)
            + (ExpenseAllocations?.Sum(a => a.AllocatedAmount) ?? 0m));
    }

    public class TaxComponentRowModel
    {
        [Display(Name = "Tax component")]
        public int TaxComponentId { get; set; }

        [Display(Name = "Rate %")]
        public decimal RatePercent { get; set; }

        [Display(Name = "Taxable amount")]
        public decimal TaxableAmount { get; set; }

        [Range(0, double.MaxValue), Display(Name = "Tax amount")]
        public decimal TaxAmount { get; set; }
    }

    public class PaymentAllocationRowModel
    {
        [Display(Name = "Order / Invoice")]
        public string? OrderId { get; set; }
        public string DocumentType { get; set; } = "Invoice";
        public decimal TotalAmount { get; set; }
        public decimal DueAmount { get; set; }

        [Range(0.0001, double.MaxValue)]
        public decimal AllocatedAmount { get; set; }
    }

    public class ExpenseAllocationRowModel
    {
        [Required]
        public int ExpenseHeadId { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal DueAmount { get; set; }

        [Range(0.0001, double.MaxValue)]
        public decimal AllocatedAmount { get; set; }
    }

    public class PartyCreateModel
    {
        [Required, StringLength(200)]
        public string PartyName { get; set; } = string.Empty;
        public string PartyType { get; set; } = "Customer";
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? GSTIN { get; set; }
    }

    public class FinanceVoucherListItem
    {
        public int Id { get; set; }
        public string DocumentNo { get; set; } = string.Empty;
        public DateTime PaymentDate { get; set; }
        public string PartyName { get; set; } = string.Empty;
        public string ModeName { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string? BillingCompanyName { get; set; }
        public decimal PaymentAmount { get; set; }
        public string ApprovalStatus { get; set; } = string.Empty;
        public bool IsSettlement { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ApprovalWorkflowEditModel
    {
        public int? Id { get; set; }

        [Required]
        public int FormTypeId { get; set; }

        [Required]
        public int BusinessSegmentId { get; set; }

        public int? BusinessCategoryId { get; set; }
        public int? BusinessSubCategoryId { get; set; }

        [Required]
        public Guid BranchId { get; set; }

        public bool IsHierarchyBased { get; set; }

        /// <summary>user | role | reportingManager</summary>
        [Required]
        public string ApproverKind { get; set; } = "user";

        public int? ApprovalUserId { get; set; }
        public int? ApprovalRoleId { get; set; }
        public bool IsReportingManager { get; set; }

        [Required, Range(1, 20)]
        public int ApprovalLevel { get; set; } = 1;

        [Required]
        public int ApprovalForId { get; set; }

        public int? PreApprovalEmailTemplateId { get; set; }
        public int? PreApprovalSmsTemplateId { get; set; }
        public int? PostApprovalEmailTemplateId { get; set; }
        public int? PostApprovalSmsTemplateId { get; set; }
        public int? PostRejectEmailTemplateId { get; set; }
        public int? PostRejectSmsTemplateId { get; set; }
    }

    public class ApprovalWorkflowListItem
    {
        public int Id { get; set; }
        public string FormTypeName { get; set; } = string.Empty;
        public string SegmentName { get; set; } = string.Empty;
        public string? CategoryName { get; set; }
        public string? BranchName { get; set; }
        public int ApprovalLevel { get; set; }
        public string? ApprovalUserName { get; set; }
        public string? ApprovalRoleName { get; set; }
        public bool IsReportingManager { get; set; }
        public string ApprovalForName { get; set; } = string.Empty;
    }

    public class ApprovalInboxItem
    {
        public int Id { get; set; }
        public string FormTypeKey { get; set; } = string.Empty;
        public string FormTypeName { get; set; } = string.Empty;
        public int RecordId { get; set; }
        public int ApprovalLevel { get; set; }
        public DateTime RequestedAt { get; set; }
        public string? RequestedByName { get; set; }
        public string? Remarks { get; set; }
    }

    public class ApprovalActionModel
    {
        public int ApprovalRequestId { get; set; }
        public string? Remarks { get; set; }
    }
}
