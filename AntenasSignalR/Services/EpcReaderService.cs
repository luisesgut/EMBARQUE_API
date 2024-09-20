using System;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using AntenasSignalR.Hubs;

namespace AntenasSignalR.Services
{
    public class EpcReaderService
    {
        private readonly IHubContext<MessageHub> _hubContext;

        public EpcReaderService(IHubContext<MessageHub> hubContext)
        {
            _hubContext = hubContext;
        }

       
        
    }
}
