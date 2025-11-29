using NetSdrClientApp.Messages;

namespace NetSdrClientAppTests
{
    public class NetSdrMessageHelperAdditionalTests
    {
        [Test]
        public void TranslateMessage_ControlItem_ShouldParseFields()
        {
            // Arrange
            var type = NetSdrMessageHelper.MsgTypes.Ack;
            var code = NetSdrMessageHelper.ControlItemCodes.ReceiverState;
            byte[] parameters = new byte[] { 0x10, 0x20, 0x30 };

            var msg = NetSdrMessageHelper.GetControlItemMessage(type, code, parameters);

            // Act
            bool ok = NetSdrMessageHelper.TranslateMessage(msg, out var parsedType, out var parsedCode, out var seq, out var body);

            // Assert
            Assert.That(ok, Is.True);
            Assert.That(parsedType, Is.EqualTo(type));
            Assert.That(parsedCode, Is.EqualTo(code));
            Assert.That(seq, Is.EqualTo((ushort)0));
            Assert.That(body, Is.EqualTo(parameters));
        }

        [Test]
        public void TranslateMessage_DataItem_ShouldParseSequenceAndBody()
        {
            // Arrange
            var type = NetSdrMessageHelper.MsgTypes.DataItem1;
            // craft parameters where first two bytes will act as sequence number
            byte[] parameters = new byte[] { 0x01, 0x02, 0xAA, 0xBB };

            var msg = NetSdrMessageHelper.GetDataItemMessage(type, parameters);

            // Act
            bool ok = NetSdrMessageHelper.TranslateMessage(msg, out var parsedType, out var parsedCode, out var seq, out var body);

            // The sequence number should be taken from the first two bytes of the body
            ushort expectedSeq = BitConverter.ToUInt16(parameters.Take(2).ToArray());
            byte[] expectedBody = parameters.Skip(2).ToArray();

            // Assert
            Assert.That(ok, Is.True);
            Assert.That(parsedType, Is.EqualTo(type));
            Assert.That(parsedCode, Is.EqualTo(NetSdrMessageHelper.ControlItemCodes.None));
            Assert.That(seq, Is.EqualTo(expectedSeq));
            Assert.That(body, Is.EqualTo(expectedBody));
        }

        [Test]
        public void GetSamples_VariousSampleSizes_ShouldReturnCorrectInts()
        {
            // sampleSize bits -> bytes
            // Prepare a body for 16-bit samples: two samples [0x01 0x00] and [0xFF 0x7F] -> ints 1 and 32767
            byte[] body16 = new byte[] { 0x01, 0x00, 0xFF, 0x7F };
            var samples16 = NetSdrMessageHelper.GetSamples(16, body16).ToArray();
            Assert.That(samples16.Length, Is.EqualTo(2));
            Assert.That(samples16[0], Is.EqualTo(1));
            Assert.That(samples16[1], Is.EqualTo(32767));

            // 8-bit samples: bytes [0x01, 0xFE] -> ints 1 and 254
            byte[] body8 = new byte[] { 0x01, 0xFE };
            var samples8 = NetSdrMessageHelper.GetSamples(8, body8).ToArray();
            Assert.That(samples8.Length, Is.EqualTo(2));
            Assert.That(samples8[0], Is.EqualTo(1));
            Assert.That(samples8[1], Is.EqualTo(254));

            // 32-bit samples (4 bytes each): [0x01,0x00,0x00,0x00] -> 1
            byte[] body32 = new byte[] { 0x01, 0x00, 0x00, 0x00 };
            var samples32 = NetSdrMessageHelper.GetSamples(32, body32).ToArray();
            Assert.That(samples32.Length, Is.EqualTo(1));
            Assert.That(samples32[0], Is.EqualTo(1));
        }

        [Test]
        public void GetSamples_InvalidSampleSize_ShouldThrow()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => NetSdrMessageHelper.GetSamples(40, new byte[10]).ToArray());
        }
    }
}
