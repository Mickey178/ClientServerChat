using ProtoBuf;

namespace MyStream
{
    [ProtoContract]
    public class ChatMessageDto
    {
        [ProtoMember(1)]
        public string Name { get; set; }

        [ProtoMember(2)]
        public string Message { get; set; }
    }
}
