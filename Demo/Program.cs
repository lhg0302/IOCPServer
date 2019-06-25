using System;


namespace IOCPDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            Tcpservert tcpservert = new Tcpservert();
            tcpservert.start();
            Console.WriteLine("Hello World!");
            Console.ReadKey();
        }
    }
}
