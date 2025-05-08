using Microsoft.Data.SqlClient;
using Tutorial8.Models;
using Tutorial8.Models.DTOs;

namespace Tutorial8.Services;

public class ClientsService : IClientsService
{
    private readonly string _connectionString = "Data Source=db-mssql;Initial Catalog=2019SBD;Integrated Security=True;Trust Server Certificate=True";


    public async Task<int> AddClient(Client client, CancellationToken cancellationToken)
    {

        if (string.IsNullOrWhiteSpace(client.FirstName) || string.IsNullOrWhiteSpace(client.LastName) || string.IsNullOrWhiteSpace(client.Email) 
            || string.IsNullOrWhiteSpace(client.Pesel) || string.IsNullOrWhiteSpace(client.Telephone))
        {
            return 0;
        }
        
        await using var conn = new SqlConnection(_connectionString);
        await using var com = new SqlCommand();


        com.Connection = conn;
        //To zapytanie SQL wstawia nowego klienta z podanymi danymi do tabeli CLIENT
        string command = @"INSERT INTO CLIENT (FirstName,LastName,Email,Telephone,Pesel) VALUES
                (@FirstName,@LastName,@Email,@Telephone,@Pesel); 
                SELECT SCOPE_IDENTITY();";
        
        com.CommandText = command;
        
        com.Parameters.AddWithValue("@FirstName", client.FirstName);
        com.Parameters.AddWithValue("@LastName", client.LastName);
        com.Parameters.AddWithValue("@Email", client.Email);
        com.Parameters.AddWithValue("@Telephone", client.Telephone);
        com.Parameters.AddWithValue("@Pesel", client.Pesel);
            
        await conn.OpenAsync(cancellationToken);
        var result = await com.ExecuteScalarAsync(cancellationToken);

        if (result != DBNull.Value)
        { 
            return Convert.ToInt32(result);
        }
        return 0;
    }
    
    public async Task<List<ClientTrip>> GetClientTrips(int clientId)
    {
        //To zapytanie SQL sprawdza, czy istnieje klient o podanym ID w tabeli CLIENT
        string clientExistsCommand = "SELECT 1 FROM Client WHERE IdClient = @IdClient";
        await using var validateConn = new SqlConnection(_connectionString);
        
        await using var validateCom = new SqlCommand(clientExistsCommand, validateConn);
        validateCom.Parameters.AddWithValue("@IdClient", clientId);
        await validateConn.OpenAsync();

        var exist = await validateCom.ExecuteScalarAsync();
        if (exist == null)
        {
            return null;
        }
        
        var clientTrips = new List<ClientTrip>();

        //To zapytanie SQL zwraca wszystkie wycieczki klienta o podanym ID, wraz z datą rejestracji i opłatą
        string command = @"SELECT
                            ct.IdClient,
                            ct.IdTrip,
                            ct.RegisteredAt,
                            ct.PaymentDate,
                            t.Name AS TripName,
                            t.Description AS TripDescription,
                            t.DateFrom AS TripDateFrom,
                            t.DateTo AS TripDateTo,
                            t.MaxPeople AS TripMaxPeople
                        FROM Client c
                        INNER JOIN Client_Trip ct ON c.IdClient = ct.IdClient
                        INNER JOIN Trip t ON ct.IdTrip = t.IdTrip
                        WHERE c.IdClient = @IdClient";
        await using var conn = new SqlConnection(_connectionString);
        await using var com = new SqlCommand(command, conn);
        com.Parameters.AddWithValue("@IdClient", clientId);

        await conn.OpenAsync();
        await using (var reader = await com.ExecuteReaderAsync())
        {
            while (await reader.ReadAsync())
            {
                clientTrips.Add(new ClientTrip()
                {
                    IdClient = reader.GetInt32(0),
                    IdTrip = reader.GetInt32(1),
                    RegisteredAt = reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2),
                    PaymentDate = reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3),
                    Trips = new List<Trip>
                    {
                        new Trip()
                        {
                            Id = reader.GetInt32(1),
                            Name = reader.GetString(4),
                            Description = reader.GetString(5),
                            DateFrom = reader.GetDateTime(6),
                            DateTo = reader.GetDateTime(7),
                            MaxPeople = reader.GetInt32(8),
                            Countries = new List<CountryDTO>()
                        }
                    }
                });
            }
        }

        //To zapytanie SQL zwraca wszystkie kraje, w których odbywa się wycieczka o podanym ID
        string countriesCommand = @"SELECT tc.IdTrip, c.Name AS CountryName
                                    FROM Country_Trip tc
                                    INNER JOIN Country c ON tc.IdCountry = c.IdCountry
                                    WHERE tc.IdTrip = @IdTrip";

        await using var countryConn = new SqlConnection(_connectionString);
        await using var countryCom = new SqlCommand(countriesCommand, countryConn);
        await countryConn.OpenAsync();
        foreach (var clientTrip in clientTrips)
        {
            countryCom.Parameters.Clear();
            countryCom.Parameters.AddWithValue("@IdTrip", clientTrip.IdTrip);

            await using var countriesReader = await countryCom.ExecuteReaderAsync();
            while (await countriesReader.ReadAsync())
            {
                clientTrip.Trips.First().Countries.Add(new CountryDTO()
                {
                    Name = countriesReader.GetString(1)
                });
            }
        }

        return clientTrips;
    }
    
    public async Task<bool> DeleteClientRegistration(int clientId, int tripId, CancellationToken cancellationToken)
    {
        await using var conn = new SqlConnection(_connectionString);
        //To zapytanie SQL sprawdza, czy istnieje klient o podanym ID w tabeli CLIENT
        await using var validateCom = new SqlCommand("SELECT 1 FROM Client_Trip WHERE IdClient = @IdClient AND IdTrip = @IdTrip", conn);
        validateCom.Parameters.AddWithValue("@IdClient", clientId);
        validateCom.Parameters.AddWithValue("@IdTrip", tripId);
        await conn.OpenAsync(cancellationToken);

        var registrationExists = await validateCom.ExecuteScalarAsync(cancellationToken);

        if (registrationExists == null)
        {
            return false;
        }

        //To zapytanie SQL usuwa rejestrację klienta o podanym ID na wycieczkę o podanym ID
        await using var deleteCom = new SqlCommand("DELETE FROM Client_Trip WHERE IdClient = @IdClient AND IdTrip = @IdTrip", conn);
        deleteCom.Parameters.AddWithValue("@IdClient", clientId);
        deleteCom.Parameters.AddWithValue("@IdTrip", tripId);

        var rowsAffected = await deleteCom.ExecuteNonQueryAsync(cancellationToken);

        return rowsAffected > 0;
    }
    
    public async Task<int> RegisterClient(int idClient, int idTrip, CancellationToken cancellationToken)
    {
        await using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        //To zapytanie SQL sprawdza, czy istnieje klient o podanym ID w tabeli CLIENT
        await using var clientCheckCmd = new SqlCommand("SELECT 1 FROM Client WHERE IdClient = @IdClient", conn);
        clientCheckCmd.Parameters.AddWithValue("@IdClient", idClient);
        if (await clientCheckCmd.ExecuteScalarAsync(cancellationToken) == null)
        {
            return -1;
        }

        //To zapytanie SQL zwraca maksymalną liczbę uczestników wycieczki o podanym ID
        await using var tripCheckCmd = new SqlCommand("SELECT MaxPeople FROM Trip WHERE IdTrip = @IdTrip", conn);
        tripCheckCmd.Parameters.AddWithValue("@IdTrip", idTrip);
        var maxPeopleObj = await tripCheckCmd.ExecuteScalarAsync(cancellationToken);
        if (maxPeopleObj == null)
        {
            return -2;
        }
        int maxPeople = Convert.ToInt32(maxPeopleObj);

        //To zapytanie SQL zwraca liczbę uczestników wycieczki o podanym ID
        await using var maxCountCmd = new SqlCommand("SELECT COUNT(*) FROM Client_Trip WHERE IdTrip = @IdTrip", conn);
        maxCountCmd.Parameters.AddWithValue("@IdTrip", idTrip);
        int currentParticipants = Convert.ToInt32(await maxCountCmd.ExecuteScalarAsync(cancellationToken));

        if (currentParticipants >= maxPeople)
        {
            return -3;
        }

        //To zapytanie SQL sprawdza, czy klient jest już zarejestrowany na wycieczkę o podanym ID
        await using var registrationCheckCmd = new SqlCommand("SELECT 1 FROM Client_Trip WHERE IdClient = @IdClient AND IdTrip = @IdTrip", conn);
        registrationCheckCmd.Parameters.AddWithValue("@IdClient", idClient);
        registrationCheckCmd.Parameters.AddWithValue("@IdTrip", idTrip);
        if (await registrationCheckCmd.ExecuteScalarAsync(cancellationToken) != null)
        {
            return -4;
        }

        //To zapytanie SQL wstawia nową rejestrację klienta na wycieczkę o podanym ID
        await using var insertCmd = new SqlCommand("INSERT INTO Client_Trip (IdClient, IdTrip, RegisteredAt) VALUES (@IdClient, @IdTrip, CONVERT(INT, CONVERT(VARCHAR, GETDATE(), 112)));", conn);
        insertCmd.Parameters.AddWithValue("@IdClient", idClient);
        insertCmd.Parameters.AddWithValue("@IdTrip", idTrip);
        var rowsAffected = await insertCmd.ExecuteNonQueryAsync(cancellationToken);

        if (rowsAffected > 0)
        {
            return 1;
        }
        return -5;
    }
}