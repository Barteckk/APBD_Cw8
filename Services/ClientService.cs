using Microsoft.Data.SqlClient;
using Tutorial8.Models.DTOs;

namespace Tutorial8.Services;

public class ClientService
{
    private readonly string _connectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=APBD;Integrated Security=True;";
    
    public async Task<List<ClientsDTO>> GetClients()
    {
        return new List<ClientsDTO>();
    }
    
    public async Task<List<TripDTO>> GetTripsForClient(int clientId)
    {
        var trips = new List<TripDTO>();
        
        var query = @"
            SELECT t.IdTrip, t.Name, t.Description, t.DateFrom, t.DateTo, t.MaxPeople, c.Name AS CountryName
            FROM Client_Trip ct
            JOIN Trip t ON ct.IdTrip = t.IdTrip
            JOIN Country_Trip ct2 ON ct.IdTrip = ct2.IdTrip
            JOIN Country c ON ct2.IdCountry = c.IdCountry
            WHERE ct.IdClient = @clientId";
        
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@clientId", clientId);
        
        await conn.OpenAsync();
        using var reader = await cmd.ExecuteReaderAsync();
        
        var tripDict = new Dictionary<int, TripDTO>();

        while (await reader.ReadAsync())
        {
            int tripId = reader.GetInt32(reader.GetOrdinal("IdTrip"));

            if (!tripDict.ContainsKey(tripId))
            {
                tripDict[tripId] = new TripDTO()
                {
                    Id = tripId,
                    Name = reader.GetString(reader.GetOrdinal("Name")),
                    Countries = new List<CountryDTO>()
                };
            }

            tripDict[tripId].Countries.Add(new CountryDTO
            {
                Name = reader.GetString(reader.GetOrdinal("CountryName"))
            });
        }

        return tripDict.Values.ToList();
    }
    
    public async Task<int> AddClient(ClientsDTO client)
    {
        if (string.IsNullOrWhiteSpace(client.FirstName) ||
            string.IsNullOrWhiteSpace(client.LastName) ||
            string.IsNullOrWhiteSpace(client.Email) ||
            string.IsNullOrWhiteSpace(client.Telephone) ||
            string.IsNullOrWhiteSpace(client.Pesel))
        {
            throw new ArgumentException("Wszystkie pola są wymagane");
        }

        var query = @"INSERT INTO Client (FirstName, LastName, Email, Telephone, Pesel)
        OUTPUT INSERTED.IdClient
        VALUES (@FirstName, @LastName, @Email, @Telephone, @Pesel)";
        
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@FirstName", client.FirstName);
        cmd.Parameters.AddWithValue("@LastName", client.LastName);
        cmd.Parameters.AddWithValue("@Email", client.Email);
        cmd.Parameters.AddWithValue("@Telephone", client.Telephone);
        cmd.Parameters.AddWithValue("@Pesel", client.Pesel);
        
        await conn.OpenAsync();
        var id = (int)await cmd.ExecuteScalarAsync();
        
        return id;
    }

    public async Task RegisterClientToTrip(int clientId, int tripId)
    {
        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        
        using var tran = conn.BeginTransaction();

        try
        {
            var checkClient = new SqlCommand("SELECT 1 FROM Client WHERE IdClient = @IdClient", conn, tran);
            checkClient.Parameters.AddWithValue("@IdClient", clientId);
            if ((await checkClient.ExecuteScalarAsync()) == null)
                throw new Exception("Client not found");
            
            var checkTrip = new SqlCommand("SELECT MaxPeople FROM Trip WHERE IdTrip = @IdTrip", conn, tran);
            checkTrip.Parameters.AddWithValue("@IdTrip", tripId);
            var maxPeopleObj = await checkTrip.ExecuteScalarAsync();
            if (maxPeopleObj == null)
                throw new Exception("Trip not found");
            
            int maxPeople = (int)maxPeopleObj;
            
            var countCmd = new SqlCommand("SELECT COUNT(*) FROM Client_Trip WHERE IdTrip = @IdTrip", conn, tran);
            countCmd.Parameters.AddWithValue("@IdTrip", tripId);
            int currentCount = (int)await countCmd.ExecuteScalarAsync();
            
            if (currentCount >= maxPeople)
                throw new Exception("Trip is full");
            
            var existsCmd = new SqlCommand("SELECT 1 FROM Client_Trip WHERE IdClient = @IdClient AND IdTrip = @IdTrip", conn, tran);
            existsCmd.Parameters.AddWithValue("@IdClient", clientId);
            existsCmd.Parameters.AddWithValue("@IdTrip", tripId);
            if ((await existsCmd.ExecuteScalarAsync()) != null)
                throw new Exception("Client is already registered for this trip");
            
            var insertCmd = new SqlCommand(@"
            INSERT INTO Client_Trip (IdClient, IdTrip, RegisteredAt, PaymentDate)
            VALUES (@IdClient, @IdTrip, GETDATE(), NULL)", conn, tran);
            insertCmd.Parameters.AddWithValue("@IdClient", clientId);
            insertCmd.Parameters.AddWithValue("@IdTrip", tripId);
            await insertCmd.ExecuteNonQueryAsync();
            
            await tran.CommitAsync();
        }
        catch
        {
            await tran.RollbackAsync();
            throw;
        }
    }

    public async Task DeleteClientFromTrip(int clientId, int tripId)
    {
        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        
        var checkCmd = new SqlCommand("SELECT 1 FROM Client_Trip WHERE IdClient = @IdClient AND IdTrip = @IdTrip", conn);
        checkCmd.Parameters.AddWithValue("@IdClient", clientId);
        checkCmd.Parameters.AddWithValue("@IdTrip", tripId);

        var exists = await checkCmd.ExecuteScalarAsync();
        if (exists == null)
            throw new Exception("Rejestracja klienta na wycieczkę nie istnieje");

        var deleteCmd = new SqlCommand("DELETE FROM Client_Trip WHERE IdClient = @IdClient AND IdTrip = @IdTrip", conn);
        deleteCmd.Parameters.AddWithValue("@IdClient", clientId);
        deleteCmd.Parameters.AddWithValue("@IdTrip", tripId);
        
        await deleteCmd.ExecuteNonQueryAsync();
    }
}