using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tutorial8.Models.DTOs;
using Tutorial8.Services;

namespace Tutorial8.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClientsController : ControllerBase
    {
        private readonly IClientService _clientService;

        public ClientsController(IClientService clientService)
        {
            _clientService = clientService;
        }

        [HttpGet]
        public async Task<IActionResult> GetClients()
        {
            var clients = await _clientService.GetClients();
            return Ok(clients);
        }

        [HttpGet("{id}/trips")]
        public async Task<IActionResult> GetClientTrips(int id)
        {
            var trips = await _clientService.GetTripsForClient(id);

            if (trips == null || trips.Count == 0)
                return NotFound($"Brak wycieczek dla klienta o id: {id}");
            
            return Ok(trips);
        }

        [HttpPost]
        public async Task<IActionResult> CreateClient([FromBody] ClientsDTO client)
        {
            try
            {
                var id = await _clientService.AddClient(client);
                return CreatedAtAction(nameof(GetClients), new { id = id }, new {IdClient = id});
            }
            catch (ArgumentException e)
            {
                return BadRequest(e.Message);
            }
        }

        [HttpPut("{id}/trips/{tripId}")]
        public async Task<IActionResult> RegisterClient(int id, int tripId)
        {
            try
            {
                await _clientService.RegisterClientToTrip(id, tripId);
                return Ok($"Klient {id} został zapisany do wycieczki {tripId}");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("{id}/trips/{tripId}")]
        public async Task<IActionResult> DeleteClientFromTrip(int id, int tripId)
        {
            try
            {
                await _clientService.DeleteClientFromTrip(id, tripId);
                return Ok($"Klient {id} został wyrejestrowany z wycieczki {tripId}");
            }
            catch (Exception e)
            {
                return BadRequest(e.Message);
            }
        }
    }
}
