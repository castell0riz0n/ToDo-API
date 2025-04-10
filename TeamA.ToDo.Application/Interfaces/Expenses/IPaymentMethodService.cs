using TeamA.ToDo.Application.DTOs.Expenses;
using TeamA.ToDo.Application.DTOs.General;

namespace TeamA.ToDo.Application.Interfaces.Expenses;

public interface IPaymentMethodService
{
    Task<ServiceResponse<List<PaymentMethodDto>>> GetPaymentMethodsAsync(string userId);
    Task<ServiceResponse<PaymentMethodDto>> GetPaymentMethodByIdAsync(Guid id, string userId);
    Task<ServiceResponse<PaymentMethodDto>> CreatePaymentMethodAsync(string userId, CreatePaymentMethodDto dto);
    Task<ServiceResponse<PaymentMethodDto>> UpdatePaymentMethodAsync(Guid id, string userId, UpdatePaymentMethodDto dto);
    Task<ServiceResponse<bool>> DeletePaymentMethodAsync(Guid id, string userId);
    Task<ServiceResponse<bool>> SetDefaultPaymentMethodAsync(Guid id, string userId);
}