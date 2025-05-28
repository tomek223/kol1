using System.Data;
using System.Data.SqlClient;
using WebApplication1.Models.Dtos;
using WebApplication1.Repository;

public class ClientRepository : IClientRepository

{

    private readonly string _connectionString;
 
    public ClientRepository(string connectionString)

    {

        _connectionString = connectionString;

    }
 
    public async Task<ClientWithRentalsDto?> GetClientWithRentalsAsync(int clientId)

    {

        var client = new ClientWithRentalsDto { Rentals = new List<RentalDto>() };
 
        using var conn = new SqlConnection(_connectionString);

        await conn.OpenAsync();
 
        
        using (var cmd = new SqlCommand("SELECT ID, FirstName, LastName, Address FROM clients WHERE ID = @clientId", conn))

        {

            cmd.Parameters.AddWithValue("@clientId", clientId);

            using var reader = await cmd.ExecuteReaderAsync();

            if (!await reader.ReadAsync())

                return null;
 
            client.Id = reader.GetInt32(0);

            client.FirstName = reader.GetString(1);

            client.LastName = reader.GetString(2);

            client.Address = reader.GetString(3);

        }
        

        string rentalsSql = @"

            SELECT c.VIN, co.Name AS Color, m.Name AS Model, cr.DateFrom, cr.DateTo, cr.TotalPrice

            FROM car_rentals cr

            JOIN cars c ON cr.CarID = c.ID

            JOIN colors co ON c.ColorID = co.ID

            JOIN models m ON c.ModelID = m.ID

            WHERE cr.ClientID = @clientId";
 
        using var cmdRentals = new SqlCommand(rentalsSql, conn);

        cmdRentals.Parameters.AddWithValue("@clientId", clientId);
 
        using var readerRentals = await cmdRentals.ExecuteReaderAsync();

        while (await readerRentals.ReadAsync())

        {

            client.Rentals.Add(new RentalDto

            {

                Vin = readerRentals.GetString(0),

                Color = readerRentals.GetString(1),

                Model = readerRentals.GetString(2),

                DateFrom = readerRentals.GetDateTime(3),

                DateTo = readerRentals.GetDateTime(4),

                TotalPrice = readerRentals.GetInt32(5)

            });

        }
 
        return client;

    }
 
    public async Task<bool> CarExistsAsync(int carId)

    {

        using var conn = new SqlConnection(_connectionString);

        await conn.OpenAsync();
 
        using var cmd = new SqlCommand("SELECT COUNT(1) FROM cars WHERE ID = @carId", conn);

        cmd.Parameters.AddWithValue("@carId", carId);
 
        var count = (int)await cmd.ExecuteScalarAsync();

        return count > 0;

    }
 
    public async Task<int> AddClientWithRentalAsync(NewClientRentalRequest request)

    {

        using var conn = new SqlConnection(_connectionString);

        await conn.OpenAsync();
 
        using var transaction = conn.BeginTransaction();
 
        try

        {


            int clientId;

            using (var cmdClient = new SqlCommand(

                "INSERT INTO clients (FirstName, LastName, Address) OUTPUT INSERTED.ID VALUES (@firstName, @lastName, @address)", 

                conn, transaction))

            {

                cmdClient.Parameters.AddWithValue("@firstName", request.Client.FirstName);

                cmdClient.Parameters.AddWithValue("@lastName", request.Client.LastName);

                cmdClient.Parameters.AddWithValue("@address", request.Client.Address);
 
                clientId = (int)await cmdClient.ExecuteScalarAsync();

            }


            int days = (int)(request.DateTo.Date - request.DateFrom.Date).TotalDays + 1;

            if (days <= 0)

                throw new ArgumentException("DateTo musi być późniejsza lub równa DateFrom.");
 
            int pricePerDay;

            using (var cmdPrice = new SqlCommand("SELECT PricePerDay FROM cars WHERE ID = @carId", conn, transaction))

            {

                cmdPrice.Parameters.AddWithValue("@carId", request.CarId);

                var result = await cmdPrice.ExecuteScalarAsync();

                if (result == null) throw new ArgumentException("Samochód o podanym ID nie istnieje.");

                pricePerDay = (int)result;

            }
 
            int totalPrice = days * pricePerDay;
            

            using (var cmdRental = new SqlCommand(

                @"INSERT INTO car_rentals (ClientID, CarID, DateFrom, DateTo, TotalPrice, Discount) 

                  VALUES (@clientId, @carId, @dateFrom, @dateTo, @totalPrice, NULL)", 

                conn, transaction))

            {

                cmdRental.Parameters.AddWithValue("@clientId", clientId);

                cmdRental.Parameters.AddWithValue("@carId", request.CarId);

                cmdRental.Parameters.AddWithValue("@dateFrom", request.DateFrom);

                cmdRental.Parameters.AddWithValue("@dateTo", request.DateTo);

                cmdRental.Parameters.AddWithValue("@totalPrice", totalPrice);
 
                await cmdRental.ExecuteNonQueryAsync();

            }
 
            await transaction.CommitAsync();
 
            return clientId;

        }

        catch

        {

            await transaction.RollbackAsync();

            throw;

        }

    }

}

 