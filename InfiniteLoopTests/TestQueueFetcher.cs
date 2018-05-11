using System;
using System.Collections.Specialized;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using InfiniteLoop;
using Moq;

namespace InfiniteLoopTests
{
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class TestQueueFetcher
    {
        [TestMethod]
        public async Task TestFetchItem()
        {
            // Arrange
            var queueMock = new Mock<IQueue<Maybe<object>>>();
            var item = new object();
            queueMock.Setup(q => q.Pop()).Returns(new Maybe<object>(item));
            var fetcher = new QueueFetcher<object>(queueMock.Object);


            // Act
            var fetchedItem = await fetcher.FetchItemAsync();

            //Assert
            Assert.AreEqual(item, fetchedItem);
            queueMock.Verify(q => q.Pop(), Times.Once);
        }

        [TestMethod]
        public async Task TestFetchItemWhenMaybeIsEmptyWaitsForAPush()
        {
            // Arrange
            var queueMock = new Mock<IQueue<Maybe<object>>>();
            
            // timer simulates other process
            var timer = new System.Timers.Timer();
            timer.Elapsed += (sender, e) => queueMock.Raise(q => q.Pushed += null);

            object postponedObject = null;
   
            queueMock.Setup(q => q.Pop())
                .Returns(
                    () =>
                    {
                        if (postponedObject == null)
                        {
                            postponedObject = new object();
                            timer.Enabled = true;
                            return new Maybe<object>();
                        }
                        return new Maybe<object>(postponedObject);
                    });
            var fetcher = new QueueFetcher<object>(queueMock.Object);
            timer.Enabled = true;

            // Act
            var fetchedItem = await fetcher.FetchItemAsync();

            //Assert
            Assert.AreEqual(postponedObject, fetchedItem);
            queueMock.Verify(q => q.Pop(), Times.Exactly(2));
        }
    }

    public class QueueFetcher<T>
    {
        private readonly IQueue<Maybe<T>> queue;

        private readonly object lockObject = new object(); 
        private SemaphoreSlim semaphore;

        public QueueFetcher(IQueue<Maybe<T>> queue)
        {
            this.queue = queue;
            this.queue.Pushed += this.Notify;
        }

        private void Notify()
        {
            lock (lockObject)
            {
                semaphore?.Release();
            }    
        }

        public async Task<T> FetchItemAsync()
        {
            lock (lockObject)
            {
                var pop = this.queue.Pop();
                if (pop.Any())
                {
                    return pop.Single();
                }
                semaphore = new SemaphoreSlim(0, 1);
            }

            await semaphore.WaitAsync();

            lock (lockObject)
            {
                semaphore.Dispose();
                semaphore = null;
            }

            return this.queue.Pop().Single();
        }
    }

    public interface IQueue<out T>
    {
        event Action Pushed;

        T Pop();
    }
}
