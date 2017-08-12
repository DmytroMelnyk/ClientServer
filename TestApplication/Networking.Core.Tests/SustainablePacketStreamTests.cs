using System;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NUnit.Framework;

namespace Networking.Core.Tests
{
    [TestFixture]
    public class SustainablePacketStreamTests
    {
        [Test]
        public async Task Messages_WithSinglePacketInStream_ExpectedCorrectPacket()
        {
            var body = new byte[] { 0, 1, 2, 3, 4 };
            var header = BitConverter.GetBytes(body.Length);
            var packet = header.Concat(body);
            var byteStream = packet.ToList();
            var skipCounter = 0;

            var stream = new Mock<Stream>();
            stream
                .Setup(x => x.ReadAsync(It.IsAny<byte[]>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
                .Callback((byte[] buffer, int offset, int count, CancellationToken ct) =>
                {
                    byteStream.GetRange(skipCounter, buffer.Length - offset).CopyTo(buffer, offset);
                    skipCounter += count;
                })
                .ReturnsAsync((byte[] buffer, int offset, int count, CancellationToken ct) => buffer.Length - offset);

            var sustainablePacketStream = new SustainablePacketStream(stream.Object, TimeSpan.FromSeconds(5));
            var receivedBody = await sustainablePacketStream
                .Messages
                .FirstAsync();

            CollectionAssert.AreEqual(body, receivedBody);
        }

        [TestCase(10, 0.1)]
        [TestCase(1, 1)]
        [TestCase(100, 0.01)]
        public async Task Messages_WithNoPacketsInStream_ExpectedKeepAliveWasSend(int expectedKeepAliveCounter, double keepAliveSeconds)
        {
            var timeoutDelay = TimeSpan.FromSeconds(1);
            var keepAliveHeader = BitConverter.GetBytes(0);
            var stream = new Mock<Stream>();
            stream
                .Setup(x => x.ReadAsync(It.IsAny<byte[]>(), 0, sizeof(int),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.Delay(Timeout.Infinite).ContinueWith(_ => default(int)));

            Expression<Func<Stream, Task>> assertedExpression =
                x => x.WriteAsync(It.Is<byte[]>(packet => packet.SequenceEqual(keepAliveHeader)), 0, 4,
                    It.IsAny<CancellationToken>());

            var keepAliveCounter = 0;
            var tcs = new TaskCompletionSource<bool>();
            stream.Setup(assertedExpression)
                .Callback(() =>
                {
                    TestContext.WriteLine($"Keepalive at: {DateTime.Now:hh:mm:ss.ff}");
                    if (++keepAliveCounter == expectedKeepAliveCounter)
                        tcs.TrySetResult(true);
                })
                .Returns(Task.Delay(10));

            var sustainablePacketStream = new SustainablePacketStream(stream.Object, TimeSpan.FromSeconds(keepAliveSeconds));
            sustainablePacketStream.Messages.IgnoreElements().Subscribe();

            TestContext.WriteLine($"StartWaiting at: {DateTime.Now:hh:mm:ss.ff}");
            await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(keepAliveSeconds * expectedKeepAliveCounter).Add(timeoutDelay)));

            TestContext.WriteLine($"number of invocation: {keepAliveCounter}");
            TestContext.WriteLine($"EndWaiting at: {DateTime.Now:hh:mm:ss.ff}");
            stream.Verify(assertedExpression, Times.AtLeast(expectedKeepAliveCounter));
        }

        [Test]
        public async Task Messages_WithLongReadOperation_ExpectedKeepAlivesWereNotSend()
        {
            var keepAliveHeader = BitConverter.GetBytes(0);
            var stream = new Mock<Stream>();

            stream
                .Setup(x => x.ReadAsync(It.IsAny<byte[]>(), 0, sizeof(int),
                    It.IsAny<CancellationToken>()))
                .Callback((byte[] buffer, int offset, int count, CancellationToken ct) => 
                    Array.Copy(BitConverter.GetBytes(40000), buffer, buffer.Length))
                .ReturnsAsync(sizeof(int));

            stream
                .Setup(x => x.ReadAsync(It.IsAny<byte[]>(), 0, 40000,
                    It.IsAny<CancellationToken>()))
                .Returns(Task.Delay(Timeout.Infinite).ContinueWith(_ => default(int)));

            var keepAliveTimeout = TimeSpan.FromSeconds(0.1);
            var sustainablePacketStream = new SustainablePacketStream(stream.Object, keepAliveTimeout);
            sustainablePacketStream.Messages.IgnoreElements().Subscribe();

            await Task.Delay(5000);
            stream.Verify(x => x.WriteAsync(It.Is<byte[]>(packet => packet.SequenceEqual(keepAliveHeader)), 0, 4,
                It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
