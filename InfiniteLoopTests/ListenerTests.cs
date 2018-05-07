using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using InfiniteLoop;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace InfiniteLoopTests
{
    [TestClass]
    public class ListenerTests
    {
        private MockRepository mockRepository;

        private Mock<IContinueListening> mockContinueListening;
        private Mock<ISomeService> mockService = new Mock<ISomeService>();

        [TestInitialize]
        public void TestInitialize()
        {
            this.mockRepository = new MockRepository(MockBehavior.Strict);

            this.mockContinueListening = this.mockRepository.Create<IContinueListening>();
            this.mockService = this.mockRepository.Create<ISomeService>();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            this.mockRepository.VerifyAll();
        }

        [TestMethod]
        public async Task TestIfMethodIsInvoked()
        {
            // Arrange
            IInstruction instruction = null;
            var data = new Mock<IInstruction>();

            var repeat = 1;
            Func<bool> function = () => repeat-- >= 0;

            mockContinueListening.Setup(c => c.proceed()).Returns( function );
            mockService.Setup(s => s.Notify()).Verifiable();
            mockService.Setup(s => s.TryGetItem<IInstruction>()).Returns(data.Object);

            InstructionListener listener = this.CreateListener();
            listener.ReceivedInstruction += i => instruction = i;

            // Act
            await listener.StartListeningAsync();

            // Assert
            Assert.AreEqual(data.Object, instruction);
        }

        [TestMethod]
        public async Task TestIfNewInstructionIsFetchedAfterHandlingFirst()
        {
            // Arrange
            var timer = new Stopwatch();
            var callList = new List<(int, long)>();

            IInstruction instruction = null;
            var data = new Mock<IInstruction>();

            var repeat = 2;
            Func<bool> function = () => --repeat >= 0;

            mockContinueListening.Setup(c => c.proceed()).Returns(function);
            mockService.Setup(s => s.Notify()).Callback(() => callList.Add((1, timer.ElapsedMilliseconds))).Verifiable();
            mockService.Setup(s => s.TryGetItem<IInstruction>()).Callback(() => callList.Add((2, timer.ElapsedMilliseconds))).Returns(data.Object);

            InstructionListener listener = this.CreateListener();
            listener.ReceivedInstruction += i =>
            {
                if (repeat == 1)
                {
                    Task.Delay(10000).Wait();
                }

                ;
            };

            // Act
            timer.Start();
            await listener.StartListeningAsync();

            // Assert
            foreach (var (type, time) in callList)
            {
                Debug.WriteLine($"call {type} @{time}ms");
            }
        }


        private InstructionListener CreateListener()
        {
            return new InstructionListener(
                this.mockContinueListening.Object,
                this.mockService.Object);
        }
    }
}
