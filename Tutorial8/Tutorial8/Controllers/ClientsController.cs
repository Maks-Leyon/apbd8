using Microsoft.AspNetCore.Mvc;
using Tutorial8.Models;
using Tutorial8.Services;

namespace Tutorial8.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ClientsController : ControllerBase
{
    private readonly IClientsService _clientsService;
        

    public ClientsController(IClientsService clientsService)
    {
        _clientsService = clientsService;
    }
    
    //Ten endpoint używany jest do dodawania klientów do bazy danych
    [HttpPost]
    public async Task<IActionResult> AddClient([FromBody] Client client, CancellationToken cancellationToken)
    {
        var c = await _clientsService.AddClient(client, cancellationToken);

        if (c > 0)
        {
            return Created(string.Empty, c);
        }
        return BadRequest("Dane potrzebne do dodania klienta są niepoprawne.");
    }
    
    //Ten endpoint używany jest do pobierania wszystkich wycieczek klienta o podanym numerze ID, wraz z datą rejestracji i opłatą
    [HttpGet("{idClient}/trips")]
    public async Task<IActionResult> GetClientTrips(int idClient)
    {
        var ct = await _clientsService.GetClientTrips(idClient);
        
        if (ct == null)
        {
            return NotFound("Nie istnieje klient o podanym numerze ID."); 
        }
        if (!ct.Any())
        {
            return NotFound("Klient nie posiada żadnych zarejestrowanych wycieczek."); 
        }

        return Ok(ct);
    }
    
    //Ten endpoint używany jest do usuwania rejestracji klienta o podanym ID na wycieczkę o podanym ID
    [HttpDelete("{client_id}/trips/{trip_id}")]
    public async Task<IActionResult> DeleteClientRegistration(int client_id, int trip_id, CancellationToken cancellationToken)
    {
        var isRegistrationDeleted = await _clientsService.DeleteClientRegistration(client_id, trip_id, cancellationToken);

        if (isRegistrationDeleted)
        {
            return Ok("Rejestracja usunieta pomyslnie");
        }
        return NotFound($"Rejestracja klienta o ID {client_id} na wycieczkę o ID {trip_id} nie istnieje."); // 404 Not Found
        
    }
    
    //Ten endpoint używany jest do rejestracji klienta o podanym ID na wycieczkę o podanym ID
    [HttpPut("{client_id}/trips/{trip_id}")]
    public async Task<IActionResult> RegisterClientForTrip(int client_id, int trip_id, CancellationToken cancellationToken)
    {
        var registrationResult = await _clientsService.RegisterClient(client_id, trip_id, cancellationToken);

        switch (registrationResult)
        {
            case 1: // RegistrationSuccess
                return Created(string.Empty,"Klient został pomyślnie zarejestrowany.");
            case -1: // ClientNotFound
                return NotFound($"Klient o ID {client_id} nie istnieje.");
            case -2: // TripNotFound
                return NotFound($"Wycieczka o ID {trip_id} nie istnieje.");
            case -3: // MaxParticipantsReached
                return Conflict($"Osiągnięto maksymalną liczbę uczestników dla wycieczki o ID {trip_id}.");
            case -4: // RegistrationExists
                return Conflict($"Klient o ID {client_id} jest już zarejestrowany na wycieczkę o ID {trip_id}.");
            default:
                return StatusCode(500, "Wystąpił błąd podczas rejestracji klienta na wycieczkę.");
        }
    }
}