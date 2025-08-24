using System;
using System.Threading.Tasks;

class Program
{
	static async Task Main(string[] args)
	{
		string choice;

		Console.WriteLine("Choose mode: (S)erver or (C)lient?");
		choice = Console.ReadLine()?.ToUpper();
		if (choice == "S")
		{
			await Server.RunServer();
		}
		else if (choice == "C")
		{
			await Client.RunClient();
		}
		else
		{
			Console.WriteLine("Invalid choice.");
		}
	}
}

