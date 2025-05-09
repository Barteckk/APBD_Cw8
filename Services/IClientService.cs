using Tutorial8.Models.DTOs;

namespace Tutorial8.Services;

public interface IClientService
{
    Task<List<ClientsDTO>> GetClients();
    Task<List<TripDTO>> GetTripsForClient(int clientId);
    Task<int> AddClient(ClientsDTO client);
    Task RegisterClientToTrip(int id, int tripId);
    Task DeleteClientFromTrip(int id, int tripId);
}