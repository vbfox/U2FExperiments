using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BlackFox.UsbHid;
using Moq;

namespace BlackFox.U2FHid.Tests
{
    class HidScenario
    {
        enum Operation
        {
            Read,
            Write
        }

        abstract class OperationBase
        {
            public abstract Operation Operation { get; }
            public abstract void Poison(Mock<IHidDevice> device, Action callback);
            public abstract void Setup(Mock<IHidDevice> device, Action successContinuation);
        }

        class ReadOperation : OperationBase
        {
            public override Operation Operation => Operation.Read;
            readonly Func<Task<HidInputReport>> func;

            public ReadOperation(Func<Task<HidInputReport>> func)
            {
                this.func = func;
            }

            public override void Poison(Mock<IHidDevice> device, Action callback)
            {
                device.Setup(h => h.GetInputReportAsync(It.IsAny<CancellationToken>()))
                    .Callback(callback);
            }

            public override void Setup(Mock<IHidDevice> device, Action successContinuation)
            {
                device.Setup(h => h.GetInputReportAsync(It.IsAny<CancellationToken>()))
                    .Returns<CancellationToken>(_ =>
                    {
                        var result = func();
                        successContinuation();
                        return result;
                    });
            }
        }

        class WriteOperation : OperationBase
        {
            public override Operation Operation => Operation.Write;

            readonly Func<HidOutputReport, Task<int>> func;

            public WriteOperation(Func<HidOutputReport, Task<int>> func)
            {
                this.func = func;
            }

            public override void Poison(Mock<IHidDevice> device, Action callback)
            {
                device.Setup(h => h.SendOutputReportAsync(It.IsAny<HidOutputReport>(), It.IsAny<CancellationToken>()))
                    .Callback(callback);
            }

            public override void Setup(Mock<IHidDevice> device, Action successContinuation)
            {
                device.Setup(h => h.SendOutputReportAsync(It.IsAny<HidOutputReport>(), It.IsAny<CancellationToken>()))
                    .Returns<HidOutputReport, CancellationToken>((report, _) =>
                    {
                        var result = func(report);
                        successContinuation();
                        return result;
                    });
            }
        }

        public static Definition Build()
        {
            return new Definition();
        }

        public class Definition
        {
            readonly List<OperationBase> steps = new List<OperationBase>();
            Runner runner;
            bool building = true;

            public void Read(Func<HidInputReport> read)
            {
                Read(() => Task.FromResult(read()));
            }

            public void Read(Func<Task<HidInputReport>> read)
            {
                if (!building) throw new InvalidOperationException("Not building");
                steps.Add(new ReadOperation(read));
            }

            public void Write(Action<HidOutputReport> write)
            {
                Write(report =>
                {
                    write(report);
                    return Task.FromResult(report.Data.Count);
                });
            }

            public void Write(Func<HidOutputReport, int> write)
            {
                Write(report => Task.FromResult(write(report)));
            }

            public void Write(Func<HidOutputReport, Task<int>> write)
            {
                if (!building) throw new InvalidOperationException("Not building");
                steps.Add(new WriteOperation(write));
            }

            public void Run(Mock<IHidDevice> device)
            {
                if (!building) throw new InvalidOperationException("Already running");

                building = false;
                runner = new Runner(steps, device);
                runner.Start();
            }

            public void End()
            {
                if (building) throw new InvalidOperationException("Not running");
                runner.End();
            }
        }

        class Runner 
        {
            private readonly List<OperationBase> steps;
            readonly Mock<IHidDevice> device;

            int index;
            OperationBase expected;

            public Runner(List<OperationBase> steps, Mock<IHidDevice> device)
            {
                this.steps = steps;
                this.device = device;
            }

            void MoveNext()
            {
                Poison(expected);

                index = index + 1;

                if (index < steps.Count)
                {
                    expected = steps[index];
                    Setup(expected);
                }
                else
                {
                    expected = null;
                }
            }

            void Poison(OperationBase op)
            {
                op.Poison(device, () =>
                {
                    if (expected == null)
                    {
                        throw new InvalidOperationException(
                            $"Executed an unexpected action of type {op.Operation} after the last step");
                    }

                    throw new InvalidOperationException(
                        $"Executed an unexpected action of type {op.Operation} expecting a {expected.Operation} at step {index}");
                });
            }

            void UnPoison(OperationBase op)
            {
                op.Poison(device, () => {});
            }

            void AllOperationTypes(Action<OperationBase> action)
            {
                var operationTypes = steps.GroupBy(op => op.Operation).Select(g => g.First());
                foreach (var operationType in operationTypes)
                {
                    action(operationType);
                }
            }

            void Setup(OperationBase op)
            {
                op.Setup(device, MoveNext);
            }

            public void Start()
            {
                index = 0;
                expected = steps.FirstOrDefault();

                AllOperationTypes(Poison);
                Setup(expected);
            }

            public void End()
            {
                AllOperationTypes(UnPoison);
                if (expected != null)
                {
                    throw new InvalidOperationException(
                        $"Not all steps were executed. Finished while still expecting step {index+1}/{steps.Count} : {expected.Operation}.");
                }
            }
        }
    }
}