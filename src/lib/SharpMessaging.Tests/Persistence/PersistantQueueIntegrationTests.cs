using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using SharpMessaging.Persistence;
using Xunit;

namespace SharpMessaging.Tests.Persistence
{
    public class PersistantQueueIntegrationTests : IDisposable
    {
        private string _path;
        private PersistantQueue _queue;

        public PersistantQueueIntegrationTests()
        {
            _path = Path.GetTempFileName();
            File.Delete(_path);
            _path = _path.Remove(_path.Length - 4, 4);
            Directory.CreateDirectory(_path);
            _queue = new PersistantQueue(_path, "TestQueue");
            _queue.Open();
        }

        [Fact]
        public void Queue_and_dequeue_one_item_should_return_the_same_and_empty_the_queue()
        {
            var expected = Encoding.ASCII.GetBytes("Hello world");
            var actual = new List<byte[]>();

            _queue.Enqueue(expected);
            _queue.FlushWriter();
            _queue.Dequeue(actual, 100);

            actual.Count.Should().Be(1);
            actual[0].ShouldBeEquivalentTo(expected);
        }

        [Fact]
        public void dequeueing_an_emptied_queue_should_work()
        {
            var expected = Encoding.ASCII.GetBytes("Hello world");
            var actual = new List<byte[]>();
            _queue.Enqueue(expected);
            _queue.FlushWriter();
            _queue.Dequeue(actual, 100);
            actual.Clear();

            _queue.Dequeue(actual, 100);

            actual.Count.Should().Be(0);
        }



        public void Dispose()
        {
            _queue.Close();
            Directory.Delete(_path, true);
            
        }
    }
}
