using AntenasSignalR.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Timers;

namespace AntenasSignalR.Controllers
{
    [ApiController]
    [Route("api/epc")]
    public class EpcController : ControllerBase
    {
        private readonly IHubContext<MessageHub> _hubContext;
        private readonly HttpClient _httpClient;
        private static System.Timers.Timer _timer;

        // constructor para inyectar IHubContext y IHttpClientFactory
        public EpcController(IHubContext<MessageHub> hubContext, IHttpClientFactory httpClientFactory)
        {
            _hubContext = hubContext;
            _httpClient = httpClientFactory.CreateClient();

            // Configurar el temporizador para monitorear cuando no se detecten etiquetas
            _timer = new System.Timers.Timer(5000); // 5 segundos sin lecturas desactivan los GPOs
            _timer.Elapsed += async (sender, e) => await DeactivateGpos();
            _timer.AutoReset = false;
        }

        // metodo para recibir eventos de EPC de la antena
        [HttpPost]
        public async Task<IActionResult> ReceiveEpcEvents([FromBody] JsonElement data)
        {
            try
            {
                // Verificar si el cuerpo de la solicitud está vacío o es un array vacío
                if (data.ValueKind == JsonValueKind.Array && data.GetArrayLength() == 0)
                {
                    Console.WriteLine("POST recibido con array vacío. No hay datos de etiquetas.");
                    return Ok("POST recibido con array vacío."); // Responder con 200 OK para array vacío
                }
                else if (data.ValueKind == JsonValueKind.Undefined || data.ValueKind == JsonValueKind.Object || data.ValueKind == JsonValueKind.Array)
                {
                    // ciclo para sacar informacion del json que arroja la antena
                    if (data.ValueKind == JsonValueKind.Array && data.GetArrayLength() > 0)
                    {
                        foreach (JsonElement eventElement in data.EnumerateArray())
                        {
                            if (eventElement.TryGetProperty("eventType", out JsonElement eventTypeElement) &&
                                eventTypeElement.GetString() == "tagInventory")
                            {
                                if (eventElement.TryGetProperty("tagInventoryEvent", out JsonElement tagInventoryEventElement))
                                {
                                    // Extraer EPC en formato hexadecimal
                                    if (tagInventoryEventElement.TryGetProperty("epcHex", out JsonElement epcHexElement))
                                    {
                                        string epcHex = epcHexElement.GetString();

                                        // Extraer nombre de la antena
                                        string antennaName = tagInventoryEventElement.TryGetProperty("antennaName", out JsonElement antennaNameElement)
                                                             ? antennaNameElement.GetString()
                                                             : "Desconocido";

                                        // Extraer la última vez visto
                                        string lastSeenTime = tagInventoryEventElement.TryGetProperty("lastSeenTime", out JsonElement lastSeenTimeElement)
                                                              ? lastSeenTimeElement.GetString()
                                                              : "No disponible";

                                        // Enviar el EPC, nombre de la antena y la última vez visto a través de SignalR a todos los clientes
                                        await _hubContext.Clients.All.SendAsync("sendMessage",
                                            $"EPC: {epcHex}, Antenna: {antennaName}, Last Seen: {lastSeenTime}");

                                        // Llamar al GpoController para activar los GPOs
                                        await CallGpoController(antennaName);

                                        // Reiniciar el temporizador al recibir una etiqueta
                                        _timer.Stop();
                                        _timer.Start(); // Reiniciar el temporizador para desactivar GPOs si no se recibe otra etiqueta
                                    }
                                }
                            }
                        }
                        return Ok("POST procesado con éxito."); // Retornar 200 OK si los datos son válidos
                    }
                    else
                    {
                        Console.WriteLine("POST recibido con formato inválido.");
                        return Ok("POST recibido con formato inválido."); // Responder con 200 OK incluso si el formato es inválido
                    }
                }
                else
                {
                    return BadRequest("Formato JSON inválido o vacío.");
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al procesar el evento de la antena: {ex.Message}");
            }
        }

        // metodo para activar los GPOs basado en el nombre de la antena
        private async Task CallGpoController(string antennaName)
        {
            string gpoActivateUrl = $"http://localhost:88/api/gpo/activate"; // URL del GpoController

            var content = new StringContent($"{{\"antennaName\": \"{antennaName}\"}}", System.Text.Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PostAsync(gpoActivateUrl, content);
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("GPOs activados para la antena: " + antennaName);
                }
                else
                {
                    Console.WriteLine("Error al activar los GPOs: " + response.ReasonPhrase);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error al hacer la llamada al GpoController: " + ex.Message);
            }
        }

        // metodo para desactivar los GPOs
        private async Task DeactivateGpos()
        {
            var gpoDeactivateUrl = "http://localhost:88/api/gpo/deactivate";
            await _httpClient.PostAsync(gpoDeactivateUrl, null);
            Console.WriteLine("GPOs desactivados.");
        }
    }
}
