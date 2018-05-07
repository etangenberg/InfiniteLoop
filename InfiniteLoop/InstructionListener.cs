using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfiniteLoop
{
    internal class InstructionListener
    {
        private readonly IContinueListening continueListener;
        private readonly ISomeService service;

        public InstructionListener(IContinueListening continueListener, ISomeService service)
        {
            this.continueListener = continueListener;
            this.service = service;
        }

        public event Action<IInstruction> ReceivedInstruction;

        public async Task StartListeningAsync()
        {
            while (this.continueListener.proceed())
            {
                service.Notify();
                var instruction = await Task.FromResult(service.TryGetItem<IInstruction>());

                await Task.Run(() => ReceivedInstruction?.Invoke(instruction));
            }
        }
    }
}
