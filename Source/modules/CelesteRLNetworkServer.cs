using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Celeste.Mod.CelesteRLAgentBridge;

public class CelesteRLNetworkServer
{
    private TcpListener _listener;
    private TcpClient _client;
    private StreamWriter _writer;
    private StreamReader _reader;
    private int _currentPort;
    private readonly StringBuilder receiveBuffer = new StringBuilder();

    public bool IsConnected { get; private set; }

    public void Start(int port)
    {
        _currentPort = port;
        try
        {
            _listener?.Stop();
            _listener = new TcpListener(IPAddress.Loopback, port);
            _listener.Start();
            Listen();
        }
        catch (Exception ex)
        {
            Logger.Error("CelesteRL", $"Failed to start server: {ex.Message}");
        }
    }

    private void Listen()
    {
        IsConnected = false;
        _listener.BeginAcceptTcpClient(OnClientConnected, null);
    }

    private void OnClientConnected(IAsyncResult ar)
    {
        try
        {
            _client = _listener.EndAcceptTcpClient(ar);
            _client.NoDelay = true;
            var stream = _client.GetStream();

            UTF8Encoding encoding = new UTF8Encoding(false);
            _writer = new StreamWriter(stream, encoding) { AutoFlush = true };
            _reader = new StreamReader(stream, encoding);
            IsConnected = true;
            Logger.Log("CelesteRL", "Python agent attached successfully.");
        }
        catch (Exception ex)
        {
            Logger.Error("CelesteRL", $"Error accepting client: {ex.Message}");
            Listen();
        }
    }

    public void Send(string data)
    {
        if (!IsConnected || _writer == null) return;

        try
        {
            _writer.WriteLine(data);
        }
        catch (Exception)
        {
            HandleDisconnect();
        }
    }


    public string Receive()
    {
        if (!IsConnected || _reader == null) return null;

        try
        {
            if (_client.GetStream().DataAvailable)
            {
                return _reader.ReadLine();
            }
        }
        catch (Exception ex)
        {
            Logger.Log(LogLevel.Warn, "CelesteRL", $"Receive Error: {ex.Message}");
            HandleDisconnect();
        }
        return null;
    }

    private void HandleDisconnect()
    {
        if (!IsConnected) return;

        Logger.Log("CelesteRL", "Connection lost. Cleaning up and waiting for reconnect...");
        IsConnected = false;
        _writer?.Dispose();
        _reader?.Dispose();
        _client?.Close();
        Listen();
    }

    public void ShutDown()
    {
        IsConnected = false;
        _client?.Close();
        _listener?.Stop();
    }
}
