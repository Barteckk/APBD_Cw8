using Microsoft.Data.SqlClient;
using Tutorial8.Models.DTOs;

namespace Tutorial8.Services;

public class TripsService : ITripsService
{
    private readonly string _connectionString = "Data Source=(localdb)\\MSSQLLocalDB;Initial Catalog=APBD;Integrated Security=True;";
    
    public async Task<List<TripDTO>> GetTrips()
    {
        var trips = new List<TripDTO>();

        string command = "SELECT IdTrip, Name FROM Trip";
        
        using (SqlConnection conn = new SqlConnection(_connectionString))
        using (SqlCommand cmd = new SqlCommand(command, conn))
        {
            await conn.OpenAsync();

            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    int idOrdinal = reader.GetOrdinal("IdTrip");
                    trips.Add(new TripDTO()
                    {
                        Id = reader.GetInt32(idOrdinal),
                        Name = reader.GetString(1),
                    });
                }
            }
        }
        

        return trips;
    }
    
    public async Task<TripDTO> GetTripById(int id)
    {
        TripDTO? trip = null;
        
        string query = @"
        SELECT t.IdTrip, t.Name, c.Name AS CountryName
        FROM Trip t
        LEFT JOIN Country_Trip ct ON t.IdTrip = ct.IdTrip
        LEFT JOIN Country c ON ct.IdCountry = c.IdCountry
        WHERE t.IdTrip = @IdTrip";
        
        using var conn = new SqlConnection(_connectionString);
        using var cmd = new SqlCommand(query, conn);
        cmd.Parameters.AddWithValue("@IdTrip", id);
        
        await conn.OpenAsync();
        using var reader = await cmd.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            if (trip == null)
            {
                trip = new TripDTO()
                {
                    Id = reader.GetInt32(reader.GetOrdinal("IdTrip")),
                    Name = reader.GetString(reader.GetOrdinal("Name")),
                    Countries = new List<CountryDTO>()
                };
            }

            if (!reader.IsDBNull(reader.GetOrdinal("CountryName")))
            {
                trip.Countries.Add(new CountryDTO
                {
                    Name = reader.GetString(reader.GetOrdinal("CountryName"))
                });
            }
        }
        return trip;
    }
}