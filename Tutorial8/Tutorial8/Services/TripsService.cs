using Microsoft.Data.SqlClient;
using Tutorial8.Models;
using Tutorial8.Models.DTOs;

namespace Tutorial8.Services;

public class TripsService : ITripsService
{
    private readonly string _connectionString = "Data Source=db-mssql;Initial Catalog=2019SBD;Integrated Security=True;Trust Server Certificate=True";
    
    public async Task<List<Trip>> GetTrips()
    {
        var trips = new List<Trip>();

        //Ta metoda SQL zwraca rekordy każdej wycieczki dla każdego kraju, z którym jest ona powiązana
        string command = @"SELECT 
                        t.IdTrip, t.Name AS TripName, t.Description, t.DateFrom, t.DateTo, t.MaxPeople,
                        c.Name AS CountryName
                    FROM Trip t
                    LEFT JOIN Country_Trip ct ON t.IdTrip = ct.IdTrip
                    LEFT JOIN Country c ON ct.IdCountry = c.IdCountry";
        
        using (SqlConnection conn = new SqlConnection(_connectionString))
        using (SqlCommand cmd = new SqlCommand(command, conn))
        {
            await conn.OpenAsync();

            using (var reader = await cmd.ExecuteReaderAsync())
            {
                while (await reader.ReadAsync())
                {
                    int idOrdinal = reader.GetOrdinal("IdTrip");
                    if (trips.Count > 0 && trips.Last().Id == reader.GetInt32(idOrdinal))
                    {
                        if (!reader.IsDBNull(6))
                        {
                            trips.Last().Countries.Add(new CountryDTO()
                            {
                                Name = reader.GetString(6)
                            });
                        }
                        continue;
                    }
                    trips.Add(new Trip()
                    {
                        Id = reader.GetInt32(idOrdinal),
                        Name = reader.GetString(1),
                        Description = reader.GetString(2),
                        DateFrom = reader.GetDateTime(3),
                        DateTo = reader.GetDateTime(4),
                        MaxPeople = reader.GetInt32(5),
                        Countries = new List<CountryDTO>() {new CountryDTO()
                        {
                            Name = reader.GetString(6)
                        }}
                    });
                }
            }
        }
        

        return trips;
    }
}