using Tutorial8.Models;

namespace Tutorial8.Services;

public interface ITripsService
{
    Task<List<Trip>> GetTrips();
}