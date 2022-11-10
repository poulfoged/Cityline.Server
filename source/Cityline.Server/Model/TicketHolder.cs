using System;
using System.IO;
using Cityline.Server;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Cityline.Server.Model
{
    public class TicketHolder : ITicketHolder
    {
        public string _source;
        private static readonly JsonSerializerSettings settings = new JsonSerializerSettings { ContractResolver = new CamelCasePropertyNamesContractResolver() };
        private static readonly JsonSerializer serializer = new JsonSerializer() { ContractResolver = new CamelCasePropertyNamesContractResolver() };

        public TicketHolder()
        {
        }

        public TicketHolder(string source)
        {
            _source = source;
        }

        public void UpdateTicket<TTicket>(TTicket ticket) where TTicket : class
        {
            _source = Encode(ticket);
        }

        private string Encode(object source)
        {
            var ms = new MemoryStream();
            using (var textWriter = new StreamWriter(ms))
            using (JsonWriter writer = new JsonTextWriter(textWriter))
                serializer.Serialize(writer, source);

            return Convert.ToBase64String(ms.ToArray());
        }

        private TTicket Decode<TTicket>(string source) where TTicket : class
        {
            if (string.IsNullOrWhiteSpace(source))
                return null;

            var ms = new MemoryStream(Convert.FromBase64String(source));
            using (var textReader = new StreamReader(ms))
            using (JsonReader reader = new JsonTextReader(textReader))
                return serializer.Deserialize<TTicket>(reader);
        }

        public TTicket GetTicket<TTicket>() where TTicket : class
        {
            var decoded = Decode<TTicket>(_source);
            return decoded;
        }

        public string AsString()
        {
            return _source;
        }
    }
}