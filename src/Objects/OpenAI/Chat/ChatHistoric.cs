using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ares.src.Objects.OpenAI.Chat
{
    internal class ChatHistoric
    {
        public string Role { get; set; } 
        public IEnumerable<ChatContent> Content { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
