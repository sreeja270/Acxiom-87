namespace AcxiomCRM.DTOs
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; } = true;
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string> Errors { get; set; } = new();

        public static ApiResponse<T> Ok(T data, string message = "Operation completed successfully.")
        {
            return new ApiResponse<T> { Success = true, Message = message, Data = data };
        }

        public static ApiResponse<T> Fail(string message, List<string>? errors = null)
        {
            return new ApiResponse<T> { Success = false, Message = message, Errors = errors ?? new() };
        }
    }

    public class CustomerDto
    {
        public int CustomerId { get; set; }
        public string CustomerCode { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string Status { get; set; } = "Active";
        public DateTime CreatedDate { get; set; }
        public string? CreatedBy { get; set; }
        public string? AssignedToId { get; set; }
        public string? AssignedToName { get; set; }
        public int TotalOpportunities { get; set; }
        public decimal TotalOpportunityValue { get; set; }
    }

    public class CreateCustomerDto
    {
        public string CustomerName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string Status { get; set; } = "Active";
        public string? AssignedToId { get; set; }
    }

    public class UpdateCustomerDto
    {
        public string CustomerName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public string? Address { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string Status { get; set; } = "Active";
        public string? AssignedToId { get; set; }
    }

    public class LeadDto
    {
        public int LeadId { get; set; }
        public string LeadCode { get; set; } = string.Empty;
        public string LeadName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public string Source { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Priority { get; set; } = "Medium";
        public decimal ExpectedValue { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? CreatedBy { get; set; }
        public string? AssignedToId { get; set; }
        public string? AssignedToName { get; set; }
        public int? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public DateTime? ConvertedDate { get; set; }
        public string? Notes { get; set; }
    }

    public class CreateLeadDto
    {
        public string LeadName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public string Source { get; set; } = "Website";
        public string Status { get; set; } = "New";
        public string Priority { get; set; } = "Medium";
        public decimal ExpectedValue { get; set; }
        public string? AssignedToId { get; set; }
        public string? Notes { get; set; }
    }

    public class UpdateLeadDto
    {
        public string LeadName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string? CompanyName { get; set; }
        public string Source { get; set; } = "Website";
        public string Status { get; set; } = "New";
        public string Priority { get; set; } = "Medium";
        public decimal ExpectedValue { get; set; }
        public string? AssignedToId { get; set; }
        public string? Notes { get; set; }
    }

    public class ConvertLeadDto
    {
        public bool CreateOpportunity { get; set; } = true;
        public string? OpportunityName { get; set; }
        public decimal OpportunityAmount { get; set; }
        public DateTime? ExpectedCloseDate { get; set; }
    }

    public class OpportunityDto
    {
        public int OpportunityId { get; set; }
        public string OpportunityCode { get; set; } = string.Empty;
        public string OpportunityName { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public int? LeadId { get; set; }
        public string? LeadName { get; set; }
        public decimal Amount { get; set; }
        public string Stage { get; set; } = string.Empty;
        public int Probability { get; set; }
        public DateTime ExpectedCloseDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal WeightedPipeline { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? CreatedBy { get; set; }
        public string? AssignedToId { get; set; }
        public string? AssignedToName { get; set; }
        public string? Description { get; set; }
    }

    public class CreateOpportunityDto
    {
        public string OpportunityName { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public int? LeadId { get; set; }
        public decimal Amount { get; set; }
        public string Stage { get; set; } = "Qualification";
        public int Probability { get; set; } = 20;
        public DateTime ExpectedCloseDate { get; set; }
        public string Status { get; set; } = "Open";
        public string? AssignedToId { get; set; }
        public string? Description { get; set; }
    }

    public class UpdateOpportunityDto
    {
        public string OpportunityName { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public int? LeadId { get; set; }
        public decimal Amount { get; set; }
        public string Stage { get; set; } = "Qualification";
        public int Probability { get; set; } = 20;
        public DateTime ExpectedCloseDate { get; set; }
        public string Status { get; set; } = "Open";
        public string? AssignedToId { get; set; }
        public string? Description { get; set; }
    }
}
