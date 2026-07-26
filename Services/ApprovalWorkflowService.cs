using DataLayer;
using System.Data;
using System.Data.SqlClient;

namespace Inventory.Services
{
    public interface IApprovalWorkflowService
    {
        bool HasActiveWorkflow(string formTypeKey, int businessSegmentId, Guid branchId, int? categoryId, int? subCategoryId);
    }

    /// <summary>
    /// Shared approval routing checks. Actual request creation happens inside submit SPs
    /// so payment/expense + approval stay in one DB transaction.
    /// </summary>
    public sealed class ApprovalWorkflowService : IApprovalWorkflowService
    {
        private readonly ILogger<ApprovalWorkflowService> _logger;

        public ApprovalWorkflowService(ILogger<ApprovalWorkflowService> logger)
        {
            _logger = logger;
        }

        public bool HasActiveWorkflow(string formTypeKey, int businessSegmentId, Guid branchId, int? categoryId, int? subCategoryId)
        {
            try
            {
                var masters = new FinanceMasterStore();
                int? formTypeId = null;
                foreach (DataRow row in masters.GetFormTypes().Rows)
                {
                    if (string.Equals(row["FormTypeKey"]?.ToString(), formTypeKey, StringComparison.OrdinalIgnoreCase))
                    {
                        formTypeId = Convert.ToInt32(row["FormTypeId"]);
                        break;
                    }
                }
                if (formTypeId == null) return false;

                foreach (DataRow row in new ApprovalWorkflowStore().GetList().Rows)
                {
                    if (Convert.ToInt32(row["FormTypeId"]) != formTypeId) continue;
                    if (Convert.ToInt32(row["BusinessSegmentId"]) != businessSegmentId) continue;
                    if ((Guid)row["BranchId"] != branchId) continue;
                    if (Convert.ToInt32(row["ApprovalLevel"]) != 1) continue;

                    var rowCat = row["BusinessCategoryId"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["BusinessCategoryId"]);
                    var rowSub = row["BusinessSubCategoryId"] == DBNull.Value ? (int?)null : Convert.ToInt32(row["BusinessSubCategoryId"]);
                    if (rowCat != null && categoryId != null && rowCat != categoryId) continue;
                    if (rowSub != null && subCategoryId != null && rowSub != subCategoryId) continue;
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "HasActiveWorkflow failed for {FormTypeKey}", formTypeKey);
            }
            return false;
        }

        public static SqlParameter[] BuildWorkflowParams(Models.ApprovalWorkflowEditModel model, string? user, bool isUpdate)
        {
            int? userId = null, roleId = null;
            var isRm = false;
            switch ((model.ApproverKind ?? "user").ToLowerInvariant())
            {
                case "role":
                    roleId = model.ApprovalRoleId;
                    break;
                case "reportingmanager":
                    isRm = true;
                    break;
                default:
                    userId = model.ApprovalUserId;
                    break;
            }

            var list = new List<SqlParameter>
            {
                new("@FormTypeId", model.FormTypeId),
                new("@BusinessSegmentId", model.BusinessSegmentId),
                new("@BusinessCategoryId", model.BusinessCategoryId.HasValue ? model.BusinessCategoryId.Value : DBNull.Value),
                new("@BusinessSubCategoryId", model.BusinessSubCategoryId.HasValue ? model.BusinessSubCategoryId.Value : DBNull.Value),
                new("@BranchId", model.BranchId),
                new("@IsHierarchyBased", model.IsHierarchyBased),
                new("@ApprovalUserId", userId.HasValue ? userId.Value : DBNull.Value),
                new("@ApprovalRoleId", roleId.HasValue ? roleId.Value : DBNull.Value),
                new("@IsReportingManager", isRm),
                new("@ApprovalLevel", model.ApprovalLevel),
                new("@ApprovalForId", model.ApprovalForId),
                new("@PreApprovalEmailTemplateId", model.PreApprovalEmailTemplateId.HasValue ? model.PreApprovalEmailTemplateId.Value : DBNull.Value),
                new("@PreApprovalSmsTemplateId", model.PreApprovalSmsTemplateId.HasValue ? model.PreApprovalSmsTemplateId.Value : DBNull.Value),
                new("@PostApprovalEmailTemplateId", model.PostApprovalEmailTemplateId.HasValue ? model.PostApprovalEmailTemplateId.Value : DBNull.Value),
                new("@PostApprovalSmsTemplateId", model.PostApprovalSmsTemplateId.HasValue ? model.PostApprovalSmsTemplateId.Value : DBNull.Value),
                new("@PostRejectEmailTemplateId", model.PostRejectEmailTemplateId.HasValue ? model.PostRejectEmailTemplateId.Value : DBNull.Value),
                new("@PostRejectSmsTemplateId", model.PostRejectSmsTemplateId.HasValue ? model.PostRejectSmsTemplateId.Value : DBNull.Value)
            };

            if (isUpdate)
            {
                list.Insert(0, new SqlParameter("@Id", model.Id ?? 0));
                list.Add(new SqlParameter("@ModifiedBy", user ?? (object)DBNull.Value));
            }
            else
            {
                list.Add(new SqlParameter("@CreatedBy", user ?? (object)DBNull.Value));
            }

            return list.ToArray();
        }
    }
}
