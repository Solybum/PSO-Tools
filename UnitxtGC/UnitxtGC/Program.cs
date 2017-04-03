using System;

namespace UnitxtGC
{
    class Program
    {
        static void UnhandledExceptionHandler(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = (Exception)e.ExceptionObject;
            Console.WriteLine("Unhandled exception\n{0}", ex);
        }

        static void Main(string[] args)
        {
            AppDomain domain = AppDomain.CurrentDomain;
            domain.UnhandledException += new UnhandledExceptionEventHandler(UnhandledExceptionHandler);

            UnitxtGC unitxt = new UnitxtGC();
            unitxt.ProcessArgs(args);

#if DEBUG
            Console.WriteLine("DEBUG: Execution finished, press any key to exit");
            Console.Read();
#endif
        }
    }
}
