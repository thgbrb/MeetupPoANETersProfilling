using System.Threading;

namespace WindowsServiceRefatorado
{
    internal class Program
    {
        private static CancellationTokenSource cancelationTokenSource =
            new CancellationTokenSource();

        private static void Main(string[] args)
            => new Loader(cancellationToken: cancelationTokenSource.Token)
                .StartProcess();

        public static void OnStop()
            => cancelationTokenSource.Cancel();
    }
}