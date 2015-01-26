using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using SharpMessaging.Persistence;
using Xunit;

namespace SharpMessaging.Tests.Persistence
{
    public class MeassuredIntegrationTest : IDisposable
    {
        private readonly string _path;
        private readonly PersistantQueue _queue;

        public MeassuredIntegrationTest()
        {
            _path = Path.GetTempFileName();
            File.Delete(_path);
            _path = _path.Remove(_path.Length - 4, 4);
            Directory.CreateDirectory(_path);
            _queue = new PersistantQueue(_path, "TestQueue");
            _queue.MaxFileSizeInBytes = 50000000;
            _queue.Open();
        }

        [Fact]
        public void StoreItems()
        {
            const int ItemsToEnqueue = 300000;

            var enqueueOnePerFlush = new Stopwatch();
            enqueueOnePerFlush.Start();
            for (int i = 0; i < ItemsToEnqueue; i++)
            {
                _queue.Enqueue(new byte[2000]);
                _queue.FlushWriter();
            }
            enqueueOnePerFlush.Stop();
            Console.WriteLine("Enqueue items: " + enqueueOnePerFlush.ElapsedMilliseconds + "ms = " + (ItemsToEnqueue * 1000 / enqueueOnePerFlush.ElapsedMilliseconds) + "items/s");

            var enqueueBatch = new Stopwatch();
            enqueueBatch.Start();
            for (int i = 0; i < ItemsToEnqueue; i++)
            {
                _queue.Enqueue(new byte[2000]);
                if (i%100 == 0)
                    _queue.FlushWriter();
            }
            _queue.FlushWriter();
            enqueueBatch.Stop();
            Console.WriteLine("Enqueue items (batching): " + enqueueBatch.ElapsedMilliseconds + "ms = " + (ItemsToEnqueue * 1000 / enqueueBatch.ElapsedMilliseconds) + "items/s");

            var dequeue = new Stopwatch();
            dequeue.Start();
            var items = new List<byte[]>();
            for (int i = 0; i < ItemsToEnqueue*2; i++)
            {
                _queue.Dequeue(items, 100);
            }
            dequeue.Stop();
            Console.WriteLine("Dequeue items: " + dequeue.ElapsedMilliseconds + "ms = " + (ItemsToEnqueue * 2 * 1000 / dequeue.ElapsedMilliseconds) + "items/s");

        }

        public void Dispose()
        {
            _queue.Close();
            Directory.Delete(_path, true);
        }
    }
}