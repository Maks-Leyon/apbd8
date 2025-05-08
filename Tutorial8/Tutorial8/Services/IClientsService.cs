using Tutorial8.Models;

namespace Tutorial8.Services;

public interface IClientsService
{
    Task<int> AddClient(Client client, CancellationToken cancellationToken);
    public Task<List<ClientTrip>> GetClientTrips(int id);
    public Task<bool> DeleteClientRegistration(int idClient, int idTrip, CancellationToken cancellationToken);
    Task<int> RegisterClient(int idClient, int idTrip, CancellationToken cancellationToken);

}