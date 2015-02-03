using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NUnit.SocketRunner
{
    class Program
    {
        private const int Port = 4711;

        static void Main(string[] args)
        {
            Socket client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            Console.Write("IP Address: ");
            var ip = Console.ReadLine();

            try
            {
                client.Connect(ip, 4711);

                Console.WriteLine("Connected. Specify tests to run? [y] [n] [f]");
                var line = Console.ReadLine();

                if (line.StartsWith("y")) // Yes
                {
                    WriteLine(client, line);
                    Console.WriteLine("Enter tests to run, comma separated with full name in a single line:");
                    var tests = Console.ReadLine();
                    WriteLine(client, tests);
                }
                else if (line.StartsWith("f")) // File
                {
                    WriteLine(client, "y");
                    WriteLine(client, File.ReadAllText("failed.txt"));
                }
                else // No
                    WriteLine(client, line);

                line = "Running tests...";
                while (!line.StartsWith("Run finished"))
                {
                    Console.WriteLine(line);
                    line = ReadLine(client);
                }

                Console.WriteLine(line);

                var xml = ReadLine(client).Replace("**RETURN**", "\r\n");
                File.WriteAllText("TestResult.xml", xml, Encoding.Unicode);

                Jatech.NUnit.Program.Main(new[] { "TestResult.xml" });
                Process.Start("TestResult.html");
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.ToString());
                Console.ReadLine();
            }
        }

        private static string ReadLine(Socket handler)
        {
            using (var stream = new NetworkStream(handler))
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadLine();
            }
        }

        private static void WriteLine(Socket handler, string text)
        {
            using (var stream = new NetworkStream(handler))
            using (var writer = new StreamWriter(stream))
            {
                writer.WriteLine(text);
            }
        }
    }
}
