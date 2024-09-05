using Impinj.OctaneSdk;
using Microsoft.AspNetCore.SignalR;
using AntenasSignalR.Hubs;
using System.Collections.Generic;

public class EpcReaderService
{
    private readonly IHubContext<MessageHub> _hubContext;
    private HashSet<string> seenTags = new HashSet<string>();
    private List<(ImpinjReader reader, string name)> readers;
    private List<string> epcList = new List<string>();

    public EpcReaderService(IServiceProvider services)
    {
        _hubContext = services.GetRequiredService<IHubContext<MessageHub>>();
        InitializeReaders();
    }

    private void InitializeReaders()
    {
        readers = new List<(ImpinjReader reader, string name)>
        {
            (new ImpinjReader(), "Entrada Producto Terminado"),
            (new ImpinjReader(), "Entrada y Salida MP")
        };

        var hostnames = new List<string>
        {
            "172.16.100.10",
            "172.16.100.197"
        };

        for (int i = 0; i < readers.Count; i++)
        {
            var currentReader = readers[i];
            try
            {
                currentReader.reader.Connect(hostnames[i]);
                Settings settings = currentReader.reader.QueryDefaultSettings();
                settings.Report.IncludeAntennaPortNumber = true;

                // Ajuste de la potencia de transmisión de las antenas
                foreach (AntennaConfig antenna in settings.Antennas)
                {
                    antenna.TxPowerInDbm = 18.0; // Ajusta este valor según tus necesidades
                    antenna.MaxRxSensitivity = false; // No usar la sensibilidad máxima
                }

                currentReader.reader.TagsReported += (sender, report) => OnTagsReported(sender, report, currentReader.name);
                currentReader.reader.ApplySettings(settings);
            }
            catch (OctaneSdkException e)
            {
                Console.WriteLine($"Error connecting reader {currentReader.name}: {e.Message}");
            }
        }
    }

    public void StartReading()
    {
        foreach (var (impinjReader, name) in readers)
        {
            impinjReader.Start(); // Iniciar la lectura de las etiquetas
        }
    }

    private void OnTagsReported(ImpinjReader reader, TagReport report, string antennaName)
    {
        foreach (Tag tag in report)
        {
            string epc = tag.Epc.ToString().Replace(" ", "");
            if (!seenTags.Contains(epc))
            {
                seenTags.Add(epc);
                epcList.Add($"[{antennaName}] {epc}");

                // Enviar el EPC a los clientes conectados a través de SignalR
                _hubContext.Clients.All.SendAsync("sendMessage", $"[{antennaName}] EPC: {epc}");
            }
        }
    }
}

