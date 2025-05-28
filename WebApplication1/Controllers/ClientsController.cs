using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models.Dtos;
using WebApplication1.Repository;

[ApiController]
[Route("api/[controller]")]
public class ClientsController : ControllerBase
{
    private readonly IClientRepository _repo;
 
    public ClientsController(IClientRepository repo)
    {
        _repo = repo;
    }
 
    [HttpGet("{clientId}")]
    public async Task<IActionResult> GetClientWithRentals(int clientId)
    {
        var client = await _repo.GetClientWithRentalsAsync(clientId);
        if (client == null)
            return NotFound();
        return Ok(client);
    }
 
    [HttpPost]
    public async Task<IActionResult> AddClientWithRental([FromBody] NewClientRentalRequest request)
    {
        if (request.DateTo < request.DateFrom)
            return BadRequest("DateTo musi być późniejsza lub równa DateFrom."); // 400 Bad Request
 
        bool carExists = await _repo.CarExistsAsync(request.CarId);
        if (!carExists)
            return BadRequest("Samochód o podanym ID nie istnieje.");
 
        int clientId = await _repo.AddClientWithRentalAsync(request);
        
        return CreatedAtAction(nameof(GetClientWithRentals), new { clientId }, null);
    }
}