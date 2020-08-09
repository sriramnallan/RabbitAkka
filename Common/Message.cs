using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Common
{
    [Serializable]
    public class Message
    {
        public long MessageId { get; private set; }
        public string MessageContent { get; private set; }
        public DateTime QueuedTime { get; private set; }
        public Message(long messageId, string messageContent, DateTime queuedTime)
        {
            MessageId = messageId;
            MessageContent = messageContent;
            QueuedTime = queuedTime;
        }

        public static byte[] SerializeIntoBinary(Message message)
        {
            MemoryStream memoryStream = new MemoryStream();
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            binaryFormatter.Serialize(memoryStream, message);
            memoryStream.Flush();
            memoryStream.Seek(0, SeekOrigin.Begin);
            return memoryStream.GetBuffer();
        }

        public static Message DeserializeFromBinary(byte[] messageBody)
        {
            MemoryStream memoryStream = new MemoryStream();
            memoryStream.Write(messageBody, 0, messageBody.Length);
            memoryStream.Seek(0, SeekOrigin.Begin);
            BinaryFormatter binaryFormatter = new BinaryFormatter();
            return binaryFormatter.Deserialize(memoryStream) as Message;
        }
    }
}
