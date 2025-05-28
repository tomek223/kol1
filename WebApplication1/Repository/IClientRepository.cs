using WebApplication1.Models.Dtos;

namespace WebApplication1.Repository;

public interface IClientRepository{
    Task<ClientWithRentalsDto?> GetClientWithRentalsAsync(int clientId);
    Task<bool> CarExistsAsync(int carId);
    Task<int> AddClientWithRentalAsync(NewClientRentalRequest request);
}