using AcxiomCRM.DTOs;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Api.Controllers
{
    [ApiController]
    [Route("api/opportunities")]
    [Authorize]
    [Produces("application/json")]
    public class OpportunitiesApiController : ControllerBase
    {
        private readonly IOpportunityService _opportunityService;

        public OpportunitiesApiController(IOpportunityService opportunityService)
        {
            _opportunityService = opportunityService;
        }

        private UserContext GetUserContext()
        {
            return UserContext.FromClaims(User, HttpContext.Connection.RemoteIpAddress?.ToString());
        }

        private static OpportunityDto ToDto(Opportunity o)
        {
            return new OpportunityDto
            {
                OpportunityId = o.OpportunityId,
                OpportunityCode = o.OpportunityCode,
                OpportunityName = o.OpportunityName,
                CustomerId = o.CustomerId,
                CustomerName = o.Customer?.CustomerName ?? string.Empty,
                LeadId = o.LeadId,
                LeadName = o.Lead?.LeadName,
                Amount = o.Amount,
                Stage = o.Stage,
                Probability = o.Probability,
                ExpectedCloseDate = o.ExpectedCloseDate,
                Status = o.Status,
                WeightedPipeline = o.WeightedPipeline,
                CreatedDate = o.CreatedDate,
                CreatedBy = o.CreatedBy,
                AssignedToId = o.AssignedToId,
                AssignedToName = o.AssignedTo?.FullName,
                Description = o.Description
            };
        }

        /// <summary>
        /// Retrieves paginated list of opportunities with weighted pipeline computations.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<OpportunityDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetOpportunities(
            [FromQuery] string? searchTerm,
            [FromQuery] string? stageFilter,
            [FromQuery] string? statusFilter,
            [FromQuery] string? assignedToFilter,
            [FromQuery] string? sortOrder,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var userContext = GetUserContext();
            var list = await _opportunityService.GetOpportunitiesAsync(
                userContext,
                searchTerm,
                stageFilter,
                statusFilter,
                assignedToFilter,
                sortOrder,
                page,
                pageSize);

            var dtos = list.Select(ToDto).ToList();
            return Ok(ApiResponse<List<OpportunityDto>>.Ok(dtos, $"Retrieved {dtos.Count} opportunity(ies). Total: {list.TotalCount}"));
        }

        /// <summary>
        /// Retrieves a single opportunity by ID.
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<OpportunityDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<OpportunityDto>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetOpportunityById(int id)
        {
            var userContext = GetUserContext();
            try
            {
                var opportunity = await _opportunityService.GetOpportunityByIdAsync(id, userContext);
                if (opportunity == null)
                {
                    return NotFound(ApiResponse<OpportunityDto>.Fail($"Opportunity with ID {id} was not found."));
                }

                return Ok(ApiResponse<OpportunityDto>.Ok(ToDto(opportunity)));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        /// <summary>
        /// Creates a new opportunity with business validation rules.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<OpportunityDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<OpportunityDto>), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateOpportunity([FromBody] CreateOpportunityDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<OpportunityDto>.Fail("Validation failed.", errors));
            }

            // Business Rules
            if (dto.Amount <= 0)
            {
                return BadRequest(ApiResponse<OpportunityDto>.Fail("Opportunity Amount must be greater than 0."));
            }

            if (dto.Probability < 0 || dto.Probability > 100)
            {
                return BadRequest(ApiResponse<OpportunityDto>.Fail("Probability must be between 0 and 100."));
            }

            if (dto.Status == "Open" && dto.ExpectedCloseDate.Date < DateTime.Today)
            {
                return BadRequest(ApiResponse<OpportunityDto>.Fail("Expected Close Date cannot be in the past."));
            }

            var userContext = GetUserContext();

            var opp = new Opportunity
            {
                OpportunityName = dto.OpportunityName,
                CustomerId = dto.CustomerId,
                LeadId = dto.LeadId > 0 ? dto.LeadId : null,
                Amount = dto.Amount,
                Stage = dto.Stage,
                Probability = dto.Probability,
                ExpectedCloseDate = dto.ExpectedCloseDate,
                Status = dto.Status,
                AssignedToId = dto.AssignedToId,
                Description = dto.Description
            };

            var (success, message, created) = await _opportunityService.CreateOpportunityAsync(opp, userContext);
            if (!success)
            {
                return BadRequest(ApiResponse<OpportunityDto>.Fail(message));
            }

            var resultDto = ToDto(created!);
            return CreatedAtAction(nameof(GetOpportunityById), new { id = resultDto.OpportunityId }, ApiResponse<OpportunityDto>.Ok(resultDto, message));
        }

        /// <summary>
        /// Updates an opportunity.
        /// </summary>
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<OpportunityDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<OpportunityDto>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<OpportunityDto>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateOpportunity(int id, [FromBody] UpdateOpportunityDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<OpportunityDto>.Fail("Validation failed.", errors));
            }

            if (dto.Amount <= 0)
            {
                return BadRequest(ApiResponse<OpportunityDto>.Fail("Opportunity Amount must be greater than 0."));
            }

            if (dto.Probability < 0 || dto.Probability > 100)
            {
                return BadRequest(ApiResponse<OpportunityDto>.Fail("Probability must be between 0 and 100."));
            }

            if (dto.Status == "Open" && dto.ExpectedCloseDate.Date < DateTime.Today)
            {
                return BadRequest(ApiResponse<OpportunityDto>.Fail("Expected Close Date cannot be in the past."));
            }

            var userContext = GetUserContext();

            try
            {
                var opp = new Opportunity
                {
                    OpportunityId = id,
                    OpportunityName = dto.OpportunityName,
                    CustomerId = dto.CustomerId,
                    LeadId = dto.LeadId > 0 ? dto.LeadId : null,
                    Amount = dto.Amount,
                    Stage = dto.Stage,
                    Probability = dto.Probability,
                    ExpectedCloseDate = dto.ExpectedCloseDate,
                    Status = dto.Status,
                    AssignedToId = dto.AssignedToId,
                    Description = dto.Description
                };

                var (success, message, updated) = await _opportunityService.UpdateOpportunityAsync(opp, userContext);
                if (!success)
                {
                    return BadRequest(ApiResponse<OpportunityDto>.Fail(message));
                }

                return Ok(ApiResponse<OpportunityDto>.Ok(ToDto(updated!), message));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        /// <summary>
        /// Deletes an opportunity.
        /// </summary>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteOpportunity(int id)
        {
            var userContext = GetUserContext();
            try
            {
                var (success, message) = await _opportunityService.DeleteOrArchiveOpportunityAsync(id, userContext);
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
