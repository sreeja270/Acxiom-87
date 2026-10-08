using AcxiomCRM.DTOs;
using AcxiomCRM.Models;
using AcxiomCRM.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AcxiomCRM.Api.Controllers
{
    [ApiController]
    [Route("api/customers")]
    [Authorize]
    [Produces("application/json")]
    public class CustomersApiController : ControllerBase
    {
        private readonly ICustomerService _customerService;

        public CustomersApiController(ICustomerService customerService)
        {
            _customerService = customerService;
        }

        private UserContext GetUserContext()
        {
            return UserContext.FromClaims(User, HttpContext.Connection.RemoteIpAddress?.ToString());
        }

        private static CustomerDto ToDto(Customer c)
        {
            return new CustomerDto
            {
                CustomerId = c.CustomerId,
                CustomerCode = c.CustomerCode,
                CustomerName = c.CustomerName,
                Email = c.Email,
                Phone = c.Phone,
                CompanyName = c.CompanyName,
                Address = c.Address,
                City = c.City,
                State = c.State,
                Status = c.Status,
                CreatedDate = c.CreatedDate,
                CreatedBy = c.CreatedBy,
                AssignedToId = c.AssignedToId,
                AssignedToName = c.AssignedTo?.FullName,
                TotalOpportunities = c.Opportunities?.Count ?? 0,
                TotalOpportunityValue = c.Opportunities?.Sum(o => o.Amount) ?? 0
            };
        }

        /// <summary>
        /// Retrieves a paginated list of customers filtered according to authorization scope.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(ApiResponse<List<CustomerDto>>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetCustomers(
            [FromQuery] string? searchTerm,
            [FromQuery] string? statusFilter,
            [FromQuery] string? assignedToFilter,
            [FromQuery] string? sortOrder,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var userContext = GetUserContext();
            var list = await _customerService.GetCustomersAsync(
                userContext,
                searchTerm,
                statusFilter,
                assignedToFilter,
                sortOrder,
                page,
                pageSize);

            var dtos = list.Select(ToDto).ToList();
            return Ok(ApiResponse<List<CustomerDto>>.Ok(dtos, $"Retrieved {dtos.Count} customer(s). Total: {list.TotalCount}"));
        }

        /// <summary>
        /// Retrieves a single customer by ID with strict scope authorization check.
        /// </summary>
        [HttpGet("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<CustomerDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<CustomerDto>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetCustomerById(int id)
        {
            var userContext = GetUserContext();
            try
            {
                var customer = await _customerService.GetCustomerByIdAsync(id, userContext);
                if (customer == null)
                {
                    return NotFound(ApiResponse<CustomerDto>.Fail($"Customer with ID {id} was not found."));
                }

                return Ok(ApiResponse<CustomerDto>.Ok(ToDto(customer)));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        /// <summary>
        /// Creates a new customer record after validating uniqueness and business rules.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(typeof(ApiResponse<CustomerDto>), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiResponse<CustomerDto>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<CustomerDto>), StatusCodes.Status409Conflict)]
        public async Task<IActionResult> CreateCustomer([FromBody] CreateCustomerDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<CustomerDto>.Fail("Validation failed.", errors));
            }

            var userContext = GetUserContext();

            if (string.IsNullOrWhiteSpace(dto.CustomerName))
            {
                return BadRequest(ApiResponse<CustomerDto>.Fail("Customer Name is required."));
            }

            if (!await _customerService.IsEmailUniqueAsync(dto.Email))
            {
                return Conflict(ApiResponse<CustomerDto>.Fail("Email already exists."));
            }

            if (!await _customerService.IsPhoneUniqueAsync(dto.Phone))
            {
                return Conflict(ApiResponse<CustomerDto>.Fail("Phone number already exists."));
            }

            if (await _customerService.IsDuplicateCustomerAsync(dto.CustomerName, dto.CompanyName))
            {
                return Conflict(ApiResponse<CustomerDto>.Fail("Duplicate customer cannot be created."));
            }

            var customer = new Customer
            {
                CustomerName = dto.CustomerName,
                Email = dto.Email,
                Phone = dto.Phone,
                CompanyName = dto.CompanyName,
                Address = dto.Address,
                City = dto.City,
                State = dto.State,
                Status = dto.Status,
                AssignedToId = dto.AssignedToId
            };

            var (success, message, created) = await _customerService.CreateCustomerAsync(customer, userContext);
            if (!success)
            {
                return BadRequest(ApiResponse<CustomerDto>.Fail(message));
            }

            var resultDto = ToDto(created!);
            return CreatedAtAction(nameof(GetCustomerById), new { id = resultDto.CustomerId }, ApiResponse<CustomerDto>.Ok(resultDto, message));
        }

        /// <summary>
        /// Updates an existing customer record.
        /// </summary>
        [HttpPut("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<CustomerDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<CustomerDto>), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiResponse<CustomerDto>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiResponse<CustomerDto>), StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateCustomer(int id, [FromBody] UpdateCustomerDto dto)
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<CustomerDto>.Fail("Validation failed.", errors));
            }

            var userContext = GetUserContext();

            if (!await _customerService.IsEmailUniqueAsync(dto.Email, id))
            {
                return Conflict(ApiResponse<CustomerDto>.Fail("Email already exists."));
            }

            if (!await _customerService.IsPhoneUniqueAsync(dto.Phone, id))
            {
                return Conflict(ApiResponse<CustomerDto>.Fail("Phone number already exists."));
            }

            if (await _customerService.IsDuplicateCustomerAsync(dto.CustomerName, dto.CompanyName, id))
            {
                return Conflict(ApiResponse<CustomerDto>.Fail("Duplicate customer cannot be created."));
            }

            try
            {
                var customer = new Customer
                {
                    CustomerId = id,
                    CustomerName = dto.CustomerName,
                    Email = dto.Email,
                    Phone = dto.Phone,
                    CompanyName = dto.CompanyName,
                    Address = dto.Address,
                    City = dto.City,
                    State = dto.State,
                    Status = dto.Status,
                    AssignedToId = dto.AssignedToId
                };

                var (success, message, updated) = await _customerService.UpdateCustomerAsync(customer, userContext);
                if (!success)
                {
                    return BadRequest(ApiResponse<CustomerDto>.Fail(message));
                }

                return Ok(ApiResponse<CustomerDto>.Ok(ToDto(updated!), message));
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
        }

        /// <summary>
        /// Deletes or deactivates a customer record according to linked dependencies.
        /// </summary>
        [HttpDelete("{id:int}")]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteCustomer(int id)
        {
            var userContext = GetUserContext();
            try
            {
                var (success, message) = await _customerService.DeleteOrDeactivateCustomerAsync(id, userContext);
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
