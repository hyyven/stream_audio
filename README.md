# Real-Time Audio Streamer

This is a simple C# console application for real-time audio streaming over a network using a client-server architecture. The server captures system audio (what you hear) and streams it to a client, which then plays the audio on a different machine.

## Features

-   **Real-time Audio Capture**: The server uses `NAudio` to capture system audio loopback.
-   **UDP Streaming**: Audio data is streamed over UDP for low-latency communication.
-   **Client-Server Model**:
    -   The **Server** captures and sends the audio.
    -   The **Client** receives and plays the audio.
-   **Custom Protocol**: A simple protocol is used for the handshake and data transfer, including packet sequencing and ping calculation.

## How It Works

1.  **Startup**: Run the application and choose Server (`S`) or Client (`C`) mode.
2.  **Server Mode**:
    -   The server starts and displays the IP addresses it's running on.
    -   It waits for a client to connect on UDP port 3333.
3.  **Client Mode**:
    -   The client prompts for the server's IP address.
    -   It sends an initial "Hello" message to connect.
4.  **Streaming**:
    -   Once connected, the server sends the audio format to the client.
    -   The server then continuously streams audio data packets.
    -   The client buffers and plays the incoming audio stream and displays the network ping.

## How to Run

### From Source

1.  Clone the repository.
2.  Open a terminal in the project directory.
3.  Run the application:
    ```sh
    dotnet run
    ```
4.  Follow the on-screen prompts to select Server or Client mode.

### Standalone Executable (Windows)

You can build a single-file executable for Windows:

1.  Run the following command:
    ```sh
    dotnet publish -r win-x64 -c Release -p:PublishSingleFile=true --self-contained true
    ```
2.  The executable will be located in the `bin/Release/net9.0/win-x64/publish/` directory.

## Dependencies

-   [.NET 9.0](https://dotnet.microsoft.com/download/dotnet/9.0)
-   [NAudio](https://github.com/naudio/NAudio) (v2.2.1) - A powerful audio library for .NET.
