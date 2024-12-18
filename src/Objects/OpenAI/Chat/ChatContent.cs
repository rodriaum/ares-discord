using OpenAI.Chat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ares.src.Objects.OpenAI.Chat
{
    public class ChatContent
    {
        private readonly ChatMessageContentPartKind _kind;
        private readonly string _text;
        private readonly string _refusal;
        private readonly string _imageUrl;
        private readonly string _dataUri;
    }
}
