using System;
using System.Diagnostics;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace NUnit.SocketRunner {
    class Program
    {
        private const int Port = 4711;

        static void Main()
        {
            Socket client = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            Console.Write("IP Address: ");
            var ip = Console.ReadLine();

            string line;
            try
            {
                client.Connect(ip, Port);

                Console.WriteLine("Connected");

                bool appendPassFile = false;

                Console.WriteLine("Specify tests to run? [y] [n] [f]");
                line = Console.ReadLine();

                if (line.StartsWith("y")) // Yes
                {
                    WriteLine(client, line);
                    Console.WriteLine("Enter tests to run, comma separated with full name in a single line:");
                    var tests = Console.ReadLine();
                    WriteLine(client, tests);
                    appendPassFile = true;
                }
                else if (line.StartsWith("f")) // File
                {
                    WriteLine(client, "y");
                    WriteLine(client, File.ReadAllText("failed.txt"));
                    appendPassFile = true;
                }
                else // No
                    WriteLine(client, line);

                Console.WriteLine("Specify tests to exclude? [y] [n] [f]");
                line = Console.ReadLine();

                if (line.StartsWith("y")) // Yes
                {
                    WriteLine(client, line);
                    Console.WriteLine("Enter tests to exclude, comma separated with full name in a single line:");
                    var tests = Console.ReadLine();
                    WriteLine(client, tests);
                }
                else if (line.StartsWith("f")) // File
                {
                    WriteLine(client, "y");
                    WriteLine(client, File.ReadAllText("passed.txt"));
                    appendPassFile = true;
                }
                else // No
                    WriteLine(client, line);

                line = "Running tests...";

                using (var fs = new FileStream("failed.txt", FileMode.Create))
                using (var ps = new FileStream("passed.txt", appendPassFile ? FileMode.Append : FileMode.Create))
                using (var fw = new StreamWriter(fs))
                using (var pw = new StreamWriter(ps))
                {
                    while (!line.StartsWith("Run finished"))
                    {
                        //Console.WriteLine(line);

                        if (line.StartsWith("Passed "))
                        {
                            Console.Write(".");
                            pw.Write(string.Concat(line.AsSpan("Passed ".Length), ","));
                            pw.Flush();
                        }
                        else if (line.StartsWith("Failed "))
                        {
                            Console.Write("X");
                            fw.Write(string.Concat(line.AsSpan("Failed ".Length), ","));
                            fw.Flush();
                        }
                        else if (line.StartsWith("Skipped "))
                            Console.Write(">");

                        var newLine = ReadLine(client);
                        if (newLine == null)
                        {
                            if (Debugger.IsAttached)
                                Debugger.Break();
                            else
                                throw new Exception("Connection failed. Last message: " + line);
                        }

                        line = newLine;
                    }
                }

                Console.WriteLine();
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
            using var stream = new NetworkStream(handler);
            using var reader = new StreamReader(stream);
            return reader.ReadLine();
        }

        private static void WriteLine(Socket handler, string text)
        {
            using var stream = new NetworkStream(handler);
            using var writer = new StreamWriter(stream);
            writer.WriteLine(text);
        }
    }
}
