﻿
namespace Tutorial8.Models;

public class ClientTrip
{
    public int IdClient { get; set; }
    public int IdTrip { get; set; }
    public int? RegisteredAt { get; set; }
    public int? PaymentDate { get; set; }
    
    public List<Trip> Trips { get; set; } = new List<Trip>();
}