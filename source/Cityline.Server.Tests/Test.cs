using System;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net.Http;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using System.Text;
using System.Diagnostics;
using System.Collections.Concurrent;
using Newtonsoft.Json;

namespace Cityline.Client.Tests
{
    [TestClass]
    public class Class1
    {
        [TestMethod]
        public async Task Can_subscribe()
        {
            // ignore ssl errors
            ServicePointManager.ServerCertificateValidationCallback += (sender, cert, chain, sslPolicyErrors) => true;

            var client = new CitylineClient(new Uri("https://localhost:5001/api/cityline"));
            client.Subscribe("user-account", frame => HandleUserAccount(frame));
            client.Subscribe("sentences", frame => HandleChat(frame));
            client.Subscribe("channels", frame => HandleChannel(frame));

            await client.StartListening();
            await Task.Delay(30000);
        }

        static void HandleUserAccount(Frame frame) 
        {
            var userAccount = frame.As<UserAccount>();
            if (string.IsNullOrWhiteSpace(userAccount.Username))
            {
                 Debug.WriteLine("Please enter username ...");
            }

            Debug.WriteLine(frame.EventName + " " + frame.Data);
        }

        static void HandleChat(Frame frame) 
        {
            var sentences = frame.As<Sentence[]>();
            
            sentences.ToList().ForEach(sentence => {
                Debug.WriteLine($"{sentence.Username}: {sentence.Text}");
            });
        }
        
        static void HandleChannel(Frame frame) 
        {
            var channels = frame.As<Channel[]>();
            
            Debug.WriteLine($"Available channels: " + string.Join(", ", channels.Select(c => c.Id)));
        }

        class Channel 
        {
            public string Id { get; set;}
            public string Name { get; set; }
        }

        class Sentence 
        {
            public string Text {get;set;}
            public string ChannelId {get;set;}
            public string Username {get;set;}
            public DateTime Created {get;set;}
        }

        class UserAccount 
        {
            public string Username { get; set; }
            public string Id { get; set; }
        }
    }

    

    public class CitylineClient : EventEmitter
    {
        private readonly Uri _serverUrl;
        private readonly IDictionary<string, Frame> _frames = new Dictionary<string, Frame>();
        private readonly IDictionary<string, string> _idCache = new Dictionary<string, string>();

        public CitylineClient(Uri serverUrl)
        {
            _serverUrl = serverUrl;
        }

        public async Task StartListening()
        {
            var buffer = new Buffer();

            var handler = new HttpClientHandler
            {
                ClientCertificateOptions = ClientCertificateOption.Manual,
                ServerCertificateCustomValidationCallback = (httpRequestMessage, cert, cetChain, policyErrors) => { return true; }
            };

            using (HttpClient httpClient = new HttpClient(handler))
            {
                httpClient.Timeout = TimeSpan.FromMilliseconds(Timeout.Infinite);

                var message = new HttpRequestMessage(HttpMethod.Post, this._serverUrl);

                message.Headers.Add("device-id", Guid.NewGuid().ToString("N"));

                message.Content = new StringContent("{ tickets: {} }", Encoding.UTF8, "application/json");
                var response = await httpClient.SendAsync(message, HttpCompletionOption.ResponseHeadersRead);
                var stream = await response.Content.ReadAsStreamAsync();

                using (var reader = new StreamReader(stream))
                {
                    while (!reader.EndOfStream)
                    {
                        buffer.Add(reader.ReadLine());

                        while (buffer.HasTerminator()) {
                            var chunk = buffer.Take();
                            var frame = ParseFrame(chunk);
                            AddFrame(frame);
                        }                        
                    }
                    Console.WriteLine("End of stream?");
                }
            }
        }

         private void AddFrame(Frame frame) 
         {
            if (frame != null && !string.IsNullOrEmpty(frame.EventName)) {
                if (!string.IsNullOrEmpty(frame.Id))
                    _idCache[frame.EventName] = frame.Id;

                _frames[frame.EventName] = frame;
                Emit(frame.EventName, frame);
            }
        }

         private Frame ParseFrame(IEnumerable<string> lines) 
         {
            var result = new Frame();
            lines.ToList().ForEach(line => {
                var parts = line.Split(": ");
                if (parts.Count() != 2)
                    return;

                switch (parts[0]) {
                    case "data":
                        result.Data = parts[1].Trim();
                        break;
                    case "id":
                        result.Id = parts[1].Trim();
                        break;
                    case "event":
                        result.EventName = parts[1].Trim();
                        break;
                        
                }
            });
            return result;
        }

    }

    class Buffer 
    {
        private List<string> _buffer = new List<string>();

        public void Add(string chunk) 
        {
            _buffer.AddRange(chunk.Split("\n"));;
        }

        public bool HasTerminator()
        {
            return _buffer.IndexOf("") != -1;
        }

        public IEnumerable<string> Take() 
        {
            var position = _buffer.IndexOf("");
            var chunk = _buffer.Take(position);
            _buffer = _buffer.Skip(position+1).ToList();  
            return chunk;
        }

        public void Clear() 
        {
            this._buffer.Clear();
        }
    }

    public class Frame {
        public string Id { get; set; }
        public string EventName { get; set;}
        public string Data { get; set; }

        public T As<T>() where T: class
        {
            return JsonConvert.DeserializeObject<T>(Data);
        }
    }

    public class CitylineEventArgs : EventArgs
    {
        public string EventName { get; }

        public string Data { get; }

        public CitylineEventArgs(string eventName, string data)
        {
            this.EventName = eventName;
            this.Data = data;
        }

        // public As<T>() {
        //     return JsonConvert.Deserialize
        // }
    }

    public class EventEmitter 
    {
        private readonly ConcurrentDictionary<string, ConcurrentBag<Action<Frame>>> _handlers = new ConcurrentDictionary<string, ConcurrentBag<Action<Frame>>>();

        public void Subscribe(string eventName, Action<Frame> handler) 
        {
            var eventHandlers = _handlers.AddOrUpdate(eventName, new ConcurrentBag<Action<Frame>>(), (k, v) => v);
            eventHandlers.Add(handler);
        }

        protected void Emit(string eventName, Frame frame) 
        {
            if (!_handlers.TryGetValue(eventName, out ConcurrentBag<Action<Frame>> eventHandlers))
                return;

            eventHandlers.ToList().ForEach(handler =>
            {
                handler.Invoke(frame);
            });
        }
    }
}
