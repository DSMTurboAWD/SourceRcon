using System;
using System.Diagnostics;
using System.Net;
using System.Reflection;
using System.Threading;
using SourceRcon.Models;


namespace SourceRcon
{
	class Program
	{
		[STAThread]
		static void Main(string[] args)
		{

            var commands = new Commands();
            if (args.Length > 0)
            {
                if (args.Length == 4)
                {
                    commands.Interactive = false;
                    commands.IpAddress = args[0];
                    commands.Port = int.Parse(args[1]);
                    commands.Password = args[2];
                    commands.Command = args[3];
                }

                else
                {
                    Console.WriteLine("To use in interactive mode, use no parameters.");
                    Console.WriteLine("Else use parameters in the form: ip port password command");
                    Console.WriteLine("Enclose the command in \" marks if it is more than one word");
                    Console.WriteLine("E.g. sourcercon 192.168.0.5 27015 testpass \"say Testing!\"");
                    return;
                }
            }
            else
            {
                Console.WriteLine("Welcome to the Remote Console Application");
                Console.WriteLine("Developed by Sterling Development");
                Console.WriteLine($"Version: {Assembly.GetExecutingAssembly().GetName().Version.ToString()}");
                Console.WriteLine("Start by entering an IP address");
                Console.WriteLine("==========================================================");
                Console.WriteLine();

                commands.Interactive = true;
                Console.WriteLine("Enter IP Address:");
                commands.IpAddress = Console.ReadLine();
                Console.WriteLine("Enter port:");
                commands.Port = int.Parse(Console.ReadLine());
                Console.WriteLine("Enter password:");
                commands.Password = Console.ReadLine();
                commands.Command = null;
            }

			var rcon = new SourceRcon();
			rcon.Errors += new StringOutput(ErrorOutput);
			rcon.ServerOutput += new StringOutput(ConsoleOutput);

            if (rcon.Connect(new IPEndPoint(IPAddress.Parse(commands.IpAddress), commands.Port), commands.Password))
			{
				while (!rcon.Connected)
				{
					Thread.Sleep(10);
				}
                if (commands.Interactive)
                {
                    Console.WriteLine("Ready for commands:");
				    while(true)
				    {
				    	var command = Console.ReadLine();
                        if (string.IsNullOrEmpty(command))
                        {
                            continue;
                        }
                        if (command.ToLower() == "quit")
                        {
                            Environment.Exit(0);
                        }
                        rcon.ServerCommand(command);
				    }
                }
                else
                {
                    rcon.ServerCommand(commands.Command);
                    Thread.Sleep(1000);
                    return;
                }
			}
			else
			{
				Console.WriteLine("No connection!");
				Thread.Sleep(1000);
			}
		}

		static void ErrorOutput(string input)
		{
			Console.WriteLine($"Error: {input}");
		}

		static void ConsoleOutput(string input)
		{
			Console.WriteLine($"Console: {input}");
		}

	}
}
