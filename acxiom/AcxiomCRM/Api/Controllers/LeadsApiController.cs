using AcxiomCRM.DTOs;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using AcxiomCRM.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Api.Controllers
{
    [ApiController]
    [Route("api/leads")]
    [Authorize]
    [Produces("application/json")]
    public class LeadsApiController : ControllerBase
    {
        private readonly ILeadService _leadService;

        public LeadsApiController(ILeadService leadService)
        {
            _leadService = leadService;
        }

        private UserContext GetUserContext()
        {
            return UserContext.FromClaims(User, HttpContext.Connection.RemoteIpAddress?.ToString());
        }

        private static LeadDto ToDto(Lead l)
        {
            return new LeadDto
            {
                LeadId = l.LeadId,
                LeadCode = l.LeadCode,
                LeadName = l.LeadName,
                Email = l.Email,
                Phone = l.Phone,
                CompanyName = l.CompanyName,
                Source = l.Source,
                Status = l.Status,
                Priority = l.Priority,
                ExpectedValue = l.ExpectedValue,
                CreatedDate = l.CreatedDate,
                CreatedBy = l.CreatedBy,
                AssignedToId = l.AssignedToId,
                AssignedToName = l.AssignedTo?.FullName,
                CustomerId = l.CustomerId,
                CustomerName = l.Customer?.CustomerName,
                ConvertedDate = l.ConvertedDate,
                Notes = l.Notes
            };
        }

        /// <summary>
        /// Retrieves paginated list of leads based on role authorization.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<LeadDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetLeads(
            [FromQuery] string? searchTerm,
            [FromQuery] string? statusFilter,
            [FromQuery] string? sourceFilter,
            [FromQuery] string? priorityFilter,
            [FromQuery] string? assignedToFilter,
            [FromQuery] string? sortOrder,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var userContext = GetUserContext();
            var list = await _leadService.GetLeadsAsync(
                userContext,
                searchTerm,
                statusFilter,
                sourceFilter,
                priorityFilter,
                assignedToFilter,
                sortOrder,
                page,
                pageSize);

            var dtos = list.Select(ToDto).ToList();
            return Ok(ApiResponse<List<LeadDto>>.Ok(dtos, $"Retrieved {dtos.Count} lead(s). Total: {list.TotalCount}"));
        }

        /// <summary>
        /// Retrieves a lead by ID with ownership scope verification.
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<LeadDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<LeadDto>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetLeadById(int id)
        {
            var userContext = GetUserContext();
            try
            {
                var lead = await _leadService.GetLeadByIdAsync(id, userContext);
                if (lead == null)
                {
                    return NotFound(ApiResponse<LeadDto>.Fail($"Lead with ID {id} was not found."));
                }

                return Ok(ApiResponse<LeadDto>.Ok(ToDto(lead)));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        /// <summary>
        /// Creates a new lead.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<LeadDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<LeadDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateLead([FromBody] CreateLeadDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<LeadDto>.Fail("Validation failed.", errors));
            }

            var userContext = GetUserContext();

            if (dto.ExpectedValue < 0)
            {
                return BadRequest(ApiResponse<LeadDto>.Fail("Expected value cannot be negative."));
            }

            var lead = new Lead
            {
                LeadName = dto.LeadName,
                Email = dto.Email,
                Phone = dto.Phone,
                CompanyName = dto.CompanyName,
                Source = dto.Source,
                Status = dto.Status,
                Priority = dto.Priority,
                ExpectedValue = dto.ExpectedValue,
                AssignedToId = dto.AssignedToId,
                Notes = dto.Notes
            };

            var (success, message, created) = await _leadService.CreateLeadAsync(lead, userContext);
            if (!success)
            {
                return BadRequest(ApiResponse<LeadDto>.Fail(message));
            }

            var resultDto = ToDto(created!);
            return CreatedAtAction(nameof(GetLeadById), new { id = resultDto.LeadId }, ApiResponse<LeadDto>.Ok(resultDto, message));
        }

        /// <summary>
        /// Updates an existing lead.
        /// </summary>
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<LeadDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<LeadDto>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<LeadDto>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateLead(int id, [FromBody] UpdateLeadDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<LeadDto>.Fail("Validation failed.", errors));
            }

            var userContext = GetUserContext();

            try
            {
                var lead = new Lead
                {
                    LeadId = id,
                    LeadName = dto.LeadName,
                    Email = dto.Email,
                    Phone = dto.Phone,
                    CompanyName = dto.CompanyName,
                    Source = dto.Source,
                    Status = dto.Status,
                    Priority = dto.Priority,
                    ExpectedValue = dto.ExpectedValue,
                    AssignedToId = dto.AssignedToId,
                    Notes = dto.Notes
                };

                var (success, message, updated) = await _leadService.UpdateLeadAsync(lead, userContext);
                if (!success)
                {
                    return BadRequest(ApiResponse<LeadDto>.Fail(message));
                }

                return Ok(ApiResponse<LeadDto>.Ok(ToDto(updated!), message));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        /// <summary>
        /// Converts a qualified lead into a Customer and linked Opportunity.
        /// </summary>
        [HttpPost("{id:int}/convert")]
        [ProducesResponseType(typeof(ApiResponse<CustomerDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<CustomerDto>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> ConvertLead(int id, [FromBody] ConvertLeadDto dto)
        {
            var userContext = GetUserContext();

            var convertVm = new ConvertLeadViewModel
            {
                LeadId = id,
                CreateOpportunity = dto.CreateOpportunity,
                OpportunityName = dto.OpportunityName,
                OpportunityAmount = dto.OpportunityAmount,
                ExpectedCloseDate = dto.ExpectedCloseDate
            };

            try
            {
                var (success, message, customer, _) = await _leadService.ConvertLeadAsync(id, convertVm, userContext);
                if (!success)
                {
                    return BadRequest(ApiResponse<CustomerDto>.Fail(message));
                }

                var custDto = new CustomerDto
                {
                    CustomerId = customer!.CustomerId,
                    CustomerCode = customer.CustomerCode,
                    CustomerName = customer.CustomerName,
                    Email = customer.Email,
                    Phone = customer.Phone,
                    CompanyName = customer.CompanyName,
                    Status = customer.Status
                };

                return Ok(ApiResponse<CustomerDto>.Ok(custDto, message));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        /// <summary>
        /// Deletes or marks lead as lost based on dependencies.
        /// </summary>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteLead(int id)
        {
            var userContext = GetUserContext();
            try
            {
                var (success, message) = await _leadService.DeleteOrDeactivateLeadAsync(id, userContext);
                if (!success)
                {
                    return NotFound(ApiResponse<bool>.Fail(message));
                }

                return Ok(ApiResponse<bool>.Ok(true, message));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }
    }
}
