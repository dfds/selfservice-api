using System.Text;
using Xunit.Abstractions;

namespace SelfService.Tests.Infrastructure.Api;

public abstract class CaptureConsoleOut : IDisposable
{
    private readonly ConsoleOutTextWriter _tw;

    protected CaptureConsoleOut(ITestOutputHelper output)
    {
        _tw = ConsoleOutTextWriter.Begin(output);
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        _tw.Dispose();
    }

    private class ConsoleOutTextWriter : TextWriter
    {
        public static ConsoleOutTextWriter Begin(ITestOutputHelper output)
        {
            var oldOut = Console.Out;

            var newOut = new ConsoleOutTextWriter(output, () => Console.SetOut(oldOut));
            Console.SetOut(newOut);

            return newOut;
        }

        private readonly StringBuilder _sb = new();
        private readonly ITestOutputHelper _output;
        private readonly Action _onDispose;

        private ConsoleOutTextWriter(ITestOutputHelper output, Action onDispose)
        {
            _output = output;
            _onDispose = onDispose;
        }

        public override Encoding Encoding => Encoding.UTF8;

        public override void WriteLine(string? message)
        {
            _sb.AppendLine(message);
        }

        public override void WriteLine(string format, params object?[] args)
        {
            _sb.AppendFormat(format, args);
            _sb.AppendLine();
        }

        public override void Write(char value)
        {
            _sb.Append(value);
        }

        protected override void Dispose(bool disposing)
        {
            _output.WriteLine("--- [BEGIN] CAPTURED OUTPUT -------------------------------");
            _output.WriteLine(_sb.ToString());
            _output.WriteLine("--- [END]   CAPTURED OUTPUT -------------------------------");
            _onDispose();
        }
    }
}
