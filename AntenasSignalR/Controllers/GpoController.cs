using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace AntenaGpoControl.Controllers
{
    [ApiController]
    [Route("api/gpo")]
    public class GpoController : ControllerBase
    {
        private readonly HttpClient _httpClient;

        // constructor que recibe un IHttpClientFactory para crear un HttpClient con manejo de SSL inseguro
        public GpoController(IHttpClientFactory httpClientFactory)
        {
            // manejo de SSL inseguro
            _httpClient = httpClientFactory.CreateClient("ImpinjClient");

            // credenciales para acceder en base64
            var authToken = "cm9vdDppbXBpbmo=";
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);
        }



        // metodo para activar los GPOs basado en el nombre de la antena
        [HttpPost("activate")]
        public async Task<IActionResult> ActivateGpos([FromBody] JsonElement data)
        {
            // Extraer el nombre de la antena del cuerpo de la solicitud
            string antennaName = data.GetProperty("antennaName").GetString();
            string gpoUrl;

            // Definir la URL de acuerdo a la antena que detectó el EPC
            if (antennaName == "1")
            {
                gpoUrl = "https://172.16.100.196/api/v1/device/gpos"; // Antena 1
            }
            else if (antennaName == "2")
            {
                gpoUrl = "https://172.16.100.199/api/v1/device/gpos"; // Antena 2
            }
            else
            {
                return BadRequest($"Antena desconocida: {antennaName}");
            }

            var jsonPayload = @"
            {
                ""gpoConfigurations"": [
                    {""gpo"": 1, ""state"": ""high""},
                    {""gpo"": 2, ""state"": ""high""},
                    {""gpo"": 3, ""state"": ""high""}
                ]
            }";

            //creamos el cuerpo de la solicitud http para activar los GPOs
            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            try
            {
                var response = await _httpClient.PutAsync(gpoUrl, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode, $"Error al activar los GPOs en la antena {antennaName}. Respuesta: {responseContent}");
                }

                return Ok($"GPOs activados en la antena {antennaName}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error al activar los GPOs en la antena {antennaName}: {ex.Message}");
            }
        }

        // metodo para desactivar los GPOs (ponerlos en low)
        [HttpPost("deactivate")]
        public async Task<IActionResult> DeactivateGpos()
        {
            var gpoUrlAntena1 = "https://172.16.100.196/api/v1/device/gpos";
            var gpoUrlAntena2 = "https://172.16.100.199/api/v1/device/gpos";

            var jsonPayload = @"
            {
                ""gpoConfigurations"": [
                    {""gpo"": 1, ""state"": ""low""},
                    {""gpo"": 2, ""state"": ""low""},
                    {""gpo"": 3, ""state"": ""low""}
                ]
            }";

            // Crear el cuerpo de la solicitud http para desactivar los GPOs

            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            try
            {
                // Hacer la solicitud a la antena 1
                var responseAntena1 = await _httpClient.PutAsync(gpoUrlAntena1, content);
                var responseContentAntena1 = await responseAntena1.Content.ReadAsStringAsync();

                if (!responseAntena1.IsSuccessStatusCode)
                {
                    return StatusCode((int)responseAntena1.StatusCode, $"Error al desactivar los GPOs en la antena 1. Respuesta: {responseContentAntena1}");
                }

                // Hacer la solicitud a la antena 2
                var responseAntena2 = await _httpClient.PutAsync(gpoUrlAntena2, content);
                var responseContentAntena2 = await responseAntena2.Content.ReadAsStringAsync();

                if (!responseAntena2.IsSuccessStatusCode)
                {
                    return StatusCode((int)responseAntena2.StatusCode, $"Error al desactivar los GPOs en la antena 2. Respuesta: {responseContentAntena2}");
                }

                return Ok("GPOs desactivados en ambas antenas.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error: {ex.Message}");
            }
        }
    }
}

