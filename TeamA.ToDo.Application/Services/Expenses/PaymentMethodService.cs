using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TeamA.ToDo.Application.DTOs.Expenses;
using TeamA.ToDo.Application.DTOs.General;
using TeamA.ToDo.Application.Interfaces.Expenses;
using TeamA.ToDo.Core.Models.Expenses;
using TeamA.ToDo.EntityFramework;

namespace TeamA.ToDo.Application.Services.Expenses;

public class PaymentMethodService : IPaymentMethodService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PaymentMethodService> _logger;

    public PaymentMethodService(
        ApplicationDbContext context,
        ILogger<PaymentMethodService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<ServiceResponse<List<PaymentMethodDto>>> GetPaymentMethodsAsync(string userId)
    {
        var response = new ServiceResponse<List<PaymentMethodDto>>();

        try
        {
            var paymentMethods = await _context.PaymentMethods
                .Where(p => p.UserId == userId)
                .ToListAsync();

            var paymentMethodCounts = await _context.Expenses
                .Where(e => e.UserId == userId && e.PaymentMethodId != null)
                .GroupBy(e => e.PaymentMethodId)
                .Select(g => new { PaymentMethodId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.PaymentMethodId.Value, x => x.Count);

            var paymentMethodDtos = paymentMethods.Select(p => new PaymentMethodDto
            {
                Id = p.Id,
                Name = p.Name,
                Description = p.Description,
                Icon = p.Icon,
                IsDefault = p.IsDefault,
                ExpenseCount = paymentMethodCounts.ContainsKey(p.Id) ? paymentMethodCounts[p.Id] : 0
            }).ToList();

            response.Data = paymentMethodDtos;
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment methods");
            response.Success = false;
            response.Message = "Failed to retrieve payment methods";
            return response;
        }
    }

    public async Task<ServiceResponse<PaymentMethodDto>> GetPaymentMethodByIdAsync(Guid id, string userId)
    {
        var response = new ServiceResponse<PaymentMethodDto>();

        try
        {
            var paymentMethod = await _context.PaymentMethods
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

            if (paymentMethod == null)
            {
                response.Success = false;
                response.Message = "Payment method not found";
                return response;
            }

            var expenseCount = await _context.Expenses
                .CountAsync(e => e.PaymentMethodId == id && e.UserId == userId);

            response.Data = new PaymentMethodDto
            {
                Id = paymentMethod.Id,
                Name = paymentMethod.Name,
                Description = paymentMethod.Description,
                Icon = paymentMethod.Icon,
                IsDefault = paymentMethod.IsDefault,
                ExpenseCount = expenseCount
            };

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving payment method");
            response.Success = false;
            response.Message = "Failed to retrieve payment method";
            return response;
        }
    }

    public async Task<ServiceResponse<PaymentMethodDto>> CreatePaymentMethodAsync(string userId, CreatePaymentMethodDto dto)
    {
        var response = new ServiceResponse<PaymentMethodDto>();

        try
        {
            // Check if payment method with the same name already exists
            var existingPaymentMethod = await _context.PaymentMethods
                .FirstOrDefaultAsync(p => p.Name == dto.Name && p.UserId == userId);

            if (existingPaymentMethod != null)
            {
                response.Success = false;
                response.Message = "A payment method with this name already exists";
                return response;
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // If this is the default payment method, unset the default flag on all other payment methods
                if (dto.IsDefault)
                {
                    var defaultPaymentMethods = await _context.PaymentMethods
                        .Where(p => p.UserId == userId && p.IsDefault)
                        .ToListAsync();

                    foreach (var method in defaultPaymentMethods)
                    {
                        method.IsDefault = false;
                    }
                }
                // If this is the first payment method, make it the default regardless of input
                else if (!await _context.PaymentMethods.AnyAsync(p => p.UserId == userId))
                {
                    dto.IsDefault = true;
                }

                // Create new payment method
                var paymentMethod = new PaymentMethod
                {
                    Id = Guid.NewGuid(),
                    Name = dto.Name,
                    Description = dto.Description,
                    Icon = dto.Icon,
                    UserId = userId,
                    IsDefault = dto.IsDefault
                };

                await _context.PaymentMethods.AddAsync(paymentMethod);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                response.Data = new PaymentMethodDto
                {
                    Id = paymentMethod.Id,
                    Name = paymentMethod.Name,
                    Description = paymentMethod.Description,
                    Icon = paymentMethod.Icon,
                    IsDefault = paymentMethod.IsDefault,
                    ExpenseCount = 0
                };

                response.Message = "Payment method created successfully";
                return response;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw ex;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment method");
            response.Success = false;
            response.Message = "Failed to create payment method";
            return response;
        }
    }

    public async Task<ServiceResponse<PaymentMethodDto>> UpdatePaymentMethodAsync(Guid id, string userId, UpdatePaymentMethodDto dto)
    {
        var response = new ServiceResponse<PaymentMethodDto>();

        try
        {
            var paymentMethod = await _context.PaymentMethods
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

            if (paymentMethod == null)
            {
                response.Success = false;
                response.Message = "Payment method not found";
                return response;
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Check for name uniqueness if changing the name
                if (!string.IsNullOrEmpty(dto.Name) && dto.Name != paymentMethod.Name)
                {
                    var existingPaymentMethod = await _context.PaymentMethods
                        .FirstOrDefaultAsync(p => p.Name == dto.Name && p.UserId == userId && p.Id != id);

                    if (existingPaymentMethod != null)
                    {
                        response.Success = false;
                        response.Message = "A payment method with this name already exists";
                        return response;
                    }

                    paymentMethod.Name = dto.Name;
                }

                if (dto.Description != null)
                {
                    paymentMethod.Description = dto.Description;
                }

                if (dto.Icon != null)
                {
                    paymentMethod.Icon = dto.Icon;
                }

                // Handle default flag
                if (dto.IsDefault.HasValue && dto.IsDefault.Value != paymentMethod.IsDefault)
                {
                    if (dto.IsDefault.Value)
                    {
                        // If setting this as default, unset all other payment methods
                        var defaultPaymentMethods = await _context.PaymentMethods
                            .Where(p => p.UserId == userId && p.IsDefault && p.Id != id)
                            .ToListAsync();

                        foreach (var method in defaultPaymentMethods)
                        {
                            method.IsDefault = false;
                        }

                        paymentMethod.IsDefault = true;
                    }
                    else
                    {
                        // Cannot unset default if it's the only payment method
                        var totalPaymentMethods = await _context.PaymentMethods
                            .CountAsync(p => p.UserId == userId);

                        if (totalPaymentMethods == 1)
                        {
                            response.Success = false;
                            response.Message = "Cannot unset default flag on the only payment method";
                            return response;
                        }

                        // Cannot unset default without setting another one
                        var otherDefaultExists = await _context.PaymentMethods
                            .AnyAsync(p => p.UserId == userId && p.IsDefault && p.Id != id);

                        if (!otherDefaultExists)
                        {
                            // Set the first other payment method as default
                            var otherPaymentMethod = await _context.PaymentMethods
                                .FirstOrDefaultAsync(p => p.UserId == userId && p.Id != id);

                            if (otherPaymentMethod != null)
                            {
                                otherPaymentMethod.IsDefault = true;
                            }
                        }

                        paymentMethod.IsDefault = false;
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var expenseCount = await _context.Expenses
                    .CountAsync(e => e.PaymentMethodId == id && e.UserId == userId);

                response.Data = new PaymentMethodDto
                {
                    Id = paymentMethod.Id,
                    Name = paymentMethod.Name,
                    Description = paymentMethod.Description,
                    Icon = paymentMethod.Icon,
                    IsDefault = paymentMethod.IsDefault,
                    ExpenseCount = expenseCount
                };

                response.Message = "Payment method updated successfully";
                return response;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw ex;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating payment method");
            response.Success = false;
            response.Message = "Failed to update payment method";
            return response;
        }
    }

    public async Task<ServiceResponse<bool>> DeletePaymentMethodAsync(Guid id, string userId)
    {
        var response = new ServiceResponse<bool>();

        try
        {
            var paymentMethod = await _context.PaymentMethods
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

            if (paymentMethod == null)
            {
                response.Success = false;
                response.Message = "Payment method not found";
                return response;
            }

            // Check if the payment method is used in any expenses
            var hasExpenses = await _context.Expenses
                .AnyAsync(e => e.PaymentMethodId == id && e.UserId == userId);

            if (hasExpenses)
            {
                response.Success = false;
                response.Message = "Cannot delete payment method because it is used in expenses";
                return response;
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // If this is the default payment method, set another payment method as default
                if (paymentMethod.IsDefault)
                {
                    var otherPaymentMethod = await _context.PaymentMethods
                        .FirstOrDefaultAsync(p => p.UserId == userId && p.Id != id);

                    if (otherPaymentMethod != null)
                    {
                        otherPaymentMethod.IsDefault = true;
                    }
                }

                _context.PaymentMethods.Remove(paymentMethod);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                response.Data = true;
                response.Message = "Payment method deleted successfully";
                return response;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw ex;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting payment method");
            response.Success = false;
            response.Message = "Failed to delete payment method";
            return response;
        }
    }

    public async Task<ServiceResponse<bool>> SetDefaultPaymentMethodAsync(Guid id, string userId)
    {
        var response = new ServiceResponse<bool>();

        try
        {
            var paymentMethod = await _context.PaymentMethods
                .FirstOrDefaultAsync(p => p.Id == id && p.UserId == userId);

            if (paymentMethod == null)
            {
                response.Success = false;
                response.Message = "Payment method not found";
                return response;
            }

            if (paymentMethod.IsDefault)
            {
                response.Success = true;
                response.Message = "Payment method is already the default";
                response.Data = true;
                return response;
            }

            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // Unset the default flag on all other payment methods
                var defaultPaymentMethods = await _context.PaymentMethods
                    .Where(p => p.UserId == userId && p.IsDefault)
                    .ToListAsync();

                foreach (var method in defaultPaymentMethods)
                {
                    method.IsDefault = false;
                }

                // Set this payment method as default
                paymentMethod.IsDefault = true;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                response.Data = true;
                response.Message = "Default payment method set successfully";
                return response;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw ex;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting default payment method");
            response.Success = false;
            response.Message = "Failed to set default payment method";
            return response;
        }
    }
}