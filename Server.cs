using NAudio.Wave;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

static class Server
{
	public static async Task RunServer()
	{
		UdpClient	udpClient;
		IPEndPoint	remoteEndPoint;

		DisplayServerIP();
		udpClient = new UdpClient(3333);
		Console.WriteLine("Audio Server Started on port 3333. Waiting for a client to connect...");
		remoteEndPoint = await WaitForClientConnection(udpClient);
		Console.WriteLine($"Client connected from {remoteEndPoint}");
		await StreamAudio(udpClient, remoteEndPoint);
		udpClient.Close();
		Console.WriteLine("Server stopped.");
	}

	private static void DisplayServerIP()
	{
		string		hostName;
		IPHostEntry	host;
		int			i;

		i = -1;
		try
		{
			hostName = Dns.GetHostName();
			host = Dns.GetHostEntry(hostName);
			Console.WriteLine("Server IP Addresses:");
			while (++i < host.AddressList.Length)
			{
				if (host.AddressList[i].AddressFamily == AddressFamily.InterNetwork)
					Console.WriteLine($"- {host.AddressList[i]}");
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Could not determine local IP address: {ex.Message}");
		}
	}

	private static async Task<IPEndPoint> WaitForClientConnection(UdpClient udpClient)
	{
		UdpReceiveResult	receivedResults;
		
		receivedResults = await udpClient.ReceiveAsync();
		return receivedResults.RemoteEndPoint;
	}

	private static async Task StreamAudio(UdpClient udpClient, IPEndPoint remoteEndPoint)
	{
		WasapiLoopbackCapture	capture;
		int						nbr;

		nbr = 0;
		capture = new WasapiLoopbackCapture();
		await SendAudioFormat(udpClient, remoteEndPoint, capture.WaveFormat);
		capture.DataAvailable += (s, a) =>
		{
			byte[] packet;
			packet = CreateAudioPacket(a.Buffer, a.BytesRecorded, ++nbr);
			udpClient.Send(packet, packet.Length, remoteEndPoint);
		};
		capture.RecordingStopped += (s, a) => { capture.Dispose(); };
		capture.StartRecording();
		Console.WriteLine("Recording and streaming started. Press Enter to stop.");
		Console.ReadLine();
		capture.StopRecording();
	}

	private static async Task SendAudioFormat(UdpClient udpClient, IPEndPoint remoteEndPoint, WaveFormat waveFormat)
	{
		byte[]	formatBytes;

		formatBytes = new byte[12];
		BitConverter.GetBytes(waveFormat.SampleRate).CopyTo(formatBytes, 0);
		BitConverter.GetBytes(waveFormat.BitsPerSample).CopyTo(formatBytes, 4);
		BitConverter.GetBytes(waveFormat.Channels).CopyTo(formatBytes, 8);
		await udpClient.SendAsync(formatBytes, formatBytes.Length, remoteEndPoint);
	}

	private static byte[] CreateAudioPacket(byte[] audioBuffer, int bytesRecorded, int nbr)
	{
		byte[]	packet;

		packet = new byte[12 + bytesRecorded];
		BitConverter.GetBytes(nbr).CopyTo(packet, 0);
		BitConverter.GetBytes(DateTime.UtcNow.Ticks).CopyTo(packet, 4);
		Buffer.BlockCopy(audioBuffer, 0, packet, 12, bytesRecorded);
		return packet;
	}
}
