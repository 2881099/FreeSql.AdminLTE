using System;
using System.Threading;

namespace FreeSql.AdminLTE.Gen
{
    class Program
    {
        static void Main(string[] args)
        {
			if (args != null && args.Length == 0) args = new[] { "?" };
			ManualResetEvent wait = new ManualResetEvent(false);
			new Thread(() => {
				Thread.CurrentThread.Join(TimeSpan.FromMilliseconds(10));
				ConsoleApp app = new ConsoleApp(args, wait);
			}).Start();
			wait.WaitOne();
			return;
		}
    }
}
