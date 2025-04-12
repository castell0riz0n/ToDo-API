using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamA.ToDo.Application.DTOs.Expenses;
using TeamA.ToDo.Application.DTOs.General;
using TeamA.ToDo.Application.Filters;
using TeamA.ToDo.Application.Interfaces.Expenses;

namespace TeamA.ToDo.Host.Controllers.Expense;

[ApiController]
[Route("api/payment-methods")]
[Authorize]
[FeatureRequirement("ExpenseApp")]
public class PaymentMethodsController : ControllerBase
{
    private readonly IPaymentMethodService _paymentMethodService;
    private readonly ILogger<PaymentMethodsController> _logger;

    public PaymentMethodsController(
        IPaymentMethodService paymentMethodService,
        ILogger<PaymentMethodsController> logger)
    {
        _paymentMethodService = paymentMethodService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ServiceResponse<List<PaymentMethodDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPaymentMethods()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var response = await _paymentMethodService.GetPaymentMethodsAsync(userId);
        return Ok(response);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ServiceResponse<PaymentMethodDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse<PaymentMethodDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPaymentMethodById(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var response = await _paymentMethodService.GetPaymentMethodByIdAsync(id, userId);

        if (!response.Success)
        {
            return NotFound(response);
        }

        return Ok(response);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ServiceResponse<PaymentMethodDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ServiceResponse<PaymentMethodDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePaymentMethod([FromBody] CreatePaymentMethodDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ServiceResponse<PaymentMethodDto>
            {
                Success = false,
                Message = "Invalid input",
                Errors = ModelState.Values.SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList()
            });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var response = await _paymentMethodService.CreatePaymentMethodAsync(userId, dto);

        if (!response.Success)
        {
            return BadRequest(response);
        }

        return CreatedAtAction(nameof(GetPaymentMethodById), new { id = response.Data.Id }, response);
    }

    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ServiceResponse<PaymentMethodDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse<PaymentMethodDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ServiceResponse<PaymentMethodDto>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePaymentMethod(Guid id, [FromBody] UpdatePaymentMethodDto dto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(new ServiceResponse<PaymentMethodDto>
            {
                Success = false,
                Message = "Invalid input",
                Errors = ModelState.Values.SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList()
            });
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var response = await _paymentMethodService.UpdatePaymentMethodAsync(id, userId, dto);

        if (!response.Success)
        {
            if (response.Message.Contains("not found"))
            {
                return NotFound(response);
            }
            return BadRequest(response);
        }

        return Ok(response);
    }

    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePaymentMethod(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var response = await _paymentMethodService.DeletePaymentMethodAsync(id, userId);

        if (!response.Success)
        {
            if (response.Message.Contains("not found"))
            {
                return NotFound(response);
            }
            return BadRequest(response);
        }

        return Ok(response);
    }

    [HttpPost("{id}/set-default")]
    [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ServiceResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetDefaultPaymentMethod(Guid id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var response = await _paymentMethodService.SetDefaultPaymentMethodAsync(id, userId);

        if (!response.Success)
        {
            if (response.Message.Contains("not found"))
            {
                return NotFound(response);
            }
            return BadRequest(response);
        }

        return Ok(response);
    }
}