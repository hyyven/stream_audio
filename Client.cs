using NAudio.Wave;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

static class Client
{
	public static async Task RunClient()
	{
		IPEndPoint		serverEndPoint;
		UdpClient		udpClient;
		IWaveProvider	waveProvider;

		serverEndPoint = GetServerEndPoint();
		if (serverEndPoint == null)
			return;
		udpClient = new UdpClient();
		waveProvider = await ConnectAndGetWaveProvider(udpClient, serverEndPoint);
		if (waveProvider == null)
		{
			Console.WriteLine("Failed to connect to server.");
			udpClient.Close();
			return;
		}
		await PlayAudioStream(udpClient, waveProvider);
		udpClient.Close();
	}

	private static IPEndPoint GetServerEndPoint()
	{
		string		serverIp;
		IPAddress	ipAddress;

		Console.WriteLine("Enter server IP address:");
		serverIp = Console.ReadLine();
		if (!IPAddress.TryParse(serverIp, out ipAddress))
		{
			Console.WriteLine("Invalid IP address.");
			return null;
		}
		return new IPEndPoint(ipAddress, 3333);
	}

	private static async Task<IWaveProvider> ConnectAndGetWaveProvider(UdpClient udpClient, IPEndPoint serverEndPoint)
	{
		byte[]					helloMessage;
		Task<UdpReceiveResult>	receiveTask;
		UdpReceiveResult		formatResults;

		helloMessage = Encoding.UTF8.GetBytes("Hello");
		await udpClient.SendAsync(helloMessage, helloMessage.Length, serverEndPoint);
		Console.WriteLine("Connecting to server...");
		receiveTask = udpClient.ReceiveAsync();
		if (await Task.WhenAny(receiveTask, Task.Delay(5000)) != receiveTask)
		{
			Console.WriteLine("Connection timed out. No response from server.");
			return null;
		}
		formatResults = await receiveTask;
		if (formatResults.Buffer.Length < 12)
		{
			Console.WriteLine("Invalid format data received from server.");
			return null;
		}
		return CreateWaveProviderFromFormat(formatResults.Buffer);
	}

	private static IWaveProvider CreateWaveProviderFromFormat(byte[] formatBuffer)
	{
		int						sampleRate;
		int						bitsPerSample;
		int						channels;
		WaveFormat				waveFormat;
		BufferedWaveProvider	bufferedWaveProvider = null;

		sampleRate = BitConverter.ToInt32(formatBuffer, 0);
		bitsPerSample = BitConverter.ToInt32(formatBuffer, 4);
		channels = BitConverter.ToInt32(formatBuffer, 8);
		if (bitsPerSample == 32)
		{
			waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channels);
			Console.WriteLine($"Received audio format: {waveFormat} (IEEE Float)");
		}
		else
		{
			waveFormat = new WaveFormat(sampleRate, bitsPerSample, channels);
			Console.WriteLine($"Received audio format: {waveFormat} (PCM)");
		}
		try
		{
			bufferedWaveProvider = new BufferedWaveProvider(waveFormat)
			{
				BufferDuration = TimeSpan.FromMilliseconds(500),
				DiscardOnBufferOverflow = true
			};
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error creating BufferedWaveProvider: {ex.Message}");
		}
		return bufferedWaveProvider;
	}

	private static async Task PlayAudioStream(UdpClient udpClient, IWaveProvider waveProvider)
	{
		WasapiOut	waveOut;

		waveOut = new WasapiOut();
		waveOut.Init(waveProvider);
		try
		{
			await AudioReceiveLoop(udpClient, (BufferedWaveProvider)waveProvider, waveOut);
		}
		catch (Exception ex)
		{
			Console.WriteLine($"\nAn error occurred: {ex.Message}");
		}
		finally
		{
			waveOut.Stop();
			Console.WriteLine("\nClient stopped.");
		}
	}

	private static async Task AudioReceiveLoop(UdpClient udpClient, BufferedWaveProvider waveProvider, WasapiOut waveOut)
	{
		bool				isPlaying;
		TimeSpan			targetBufferDuration;
		int					oldnbr;
		UdpReceiveResult	receivedResults;

		isPlaying = false;
		targetBufferDuration = TimeSpan.FromMilliseconds(5);
		oldnbr = 0;
		while (true)
		{
			receivedResults = await udpClient.ReceiveAsync();
			if (receivedResults.Buffer.Length > 12)
			{
				oldnbr = ProcessAudioPacket(receivedResults.Buffer, waveProvider, oldnbr);
			}
			if (!isPlaying && waveProvider.BufferedDuration > targetBufferDuration)
			{
				waveOut.Play();
				isPlaying = true;
			}
		}
	}

	private static int ProcessAudioPacket(byte[] buffer, BufferedWaveProvider waveProvider, int oldnbr)
	{
		int						nbr;
		long					serverTicks;
		long					clientTicks;
		double					ping;

		nbr = BitConverter.ToInt32(buffer, 0);
		if (nbr != oldnbr + 1 && oldnbr != 0)
		{
			Console.WriteLine($"\nPacket loss detected: {oldnbr} -> {nbr}");
		}
		serverTicks = BitConverter.ToInt64(buffer, 4);
		clientTicks = DateTime.UtcNow.Ticks;
		ping = TimeSpan.FromTicks(clientTicks - serverTicks).TotalMilliseconds;
		Console.Write($"\rPing: {ping:F2} ms   ");
		waveProvider.AddSamples(buffer, 12, buffer.Length - 12);
		return nbr;
	}
}
