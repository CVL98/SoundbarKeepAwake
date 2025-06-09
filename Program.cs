using System;
using System.Linq;
using System.Threading;
using System.Reflection;
using System.Threading.Tasks;
using System.IO;
using NAudio.Wave;
using NAudio.CoreAudioApi;

class Program
{
    // App settings
    private const string DeviceNameToFind = "Soundbar";
    private const int BeepIntervalMinutes = 10;
    private const int RetryIntervalSeconds = 60;
    private const string BeepSoundResource = "SoundbarKeepAwake.beep.wav";
    private const int WasapiBufferMs = 200;
    
    // Audio resources - kept as static fields to prevent GC
    private static WaveFileReader? audioReader;
    private static WasapiOut? outputDevice;
    private static MemoryStream? audioStream;
    
    static async Task Main(string[] args)
    {
        bool deviceFound = false;
        MMDevice? device = null;
        
        Console.WriteLine($"Looking for audio device containing '{DeviceNameToFind}'...");
        Console.WriteLine("Press Ctrl+C to stop.");

        // Enable graceful shutdown with Ctrl+C
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (s, e) => {
            e.Cancel = true;
            cts.Cancel();
            Console.WriteLine("Shutting down...");
        };

        // Load audio file only once at startup
        byte[]? audioData = LoadBeepSoundData();
        if (audioData == null)
        {
            Console.WriteLine($"Failed to load embedded resource {BeepSoundResource}. Exiting.");
            return;
        }

        // Main program loop
        while (!cts.Token.IsCancellationRequested)
        {
            try
            {
                // Find target audio device
                MMDevice? newDevice = FindAudioDevice(DeviceNameToFind);
                
                // Handle device disconnection or change
                if (device != null && (newDevice == null || !newDevice.ID.Equals(device.ID)))
                {
                    CleanupAudioResources();
                    device.Dispose();
                    device = null;
                    deviceFound = false;
                }
                
                device = newDevice;
                
                if (device != null)
                {
                    // First time device found - initialize audio
                    if (!deviceFound)
                    {
                        Console.WriteLine($"Found device containing '{DeviceNameToFind}': {device.FriendlyName}");
                        Console.WriteLine($"Playing beep every {BeepIntervalMinutes} minutes to keep it awake");
                        deviceFound = true;
                        
                        InitializeAudioResources(device, audioData);
                    }
                    
                    // Play sound and wait for next interval
                    PlayBeep();
                    await Task.Delay(BeepIntervalMinutes * 60 * 1000, cts.Token);
                }
                else
                {
                    // Handle device not found scenarios
                    if (deviceFound)
                    {
                        Console.WriteLine($"Device containing '{DeviceNameToFind}' was lost. Waiting to reconnect...");
                        deviceFound = false;
                    }
                    else
                    {
                        Console.WriteLine($"No device containing '{DeviceNameToFind}' found. Checking again in {RetryIntervalSeconds} seconds...");
                    }
                    
                    await Task.Delay(RetryIntervalSeconds * 1000, cts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                // Normal exit path via cancellation
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"Will try again in {RetryIntervalSeconds} seconds...");
                
                CleanupAudioResources();
                
                if (device != null)
                {
                    device.Dispose();
                    device = null;
                }
                
                deviceFound = false;
                
                try {
                    await Task.Delay(RetryIntervalSeconds * 1000, cts.Token);
                }
                catch (OperationCanceledException) {
                    break;
                }
            }
        }

        // Final cleanup
        CleanupAudioResources();
        if (device != null)
        {
            device.Dispose();
        }
        
        Console.WriteLine("Program terminated.");
    }

    // Searches for an audio output device containing the specified name
    static MMDevice? FindAudioDevice(string deviceNameContains)
    {
        using var enumerator = new MMDeviceEnumerator();
        var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
        
        return devices.FirstOrDefault(d =>
            d.FriendlyName.IndexOf(deviceNameContains, StringComparison.OrdinalIgnoreCase) >= 0);
    }

    // Loads the embedded WAV file into memory
    static byte[]? LoadBeepSoundData()
    {
        try
        {
            var assembly = Assembly.GetExecutingAssembly();
            using var stream = assembly.GetManifestResourceStream(BeepSoundResource);
            if (stream == null)
            {
                Console.WriteLine($"Embedded resource {BeepSoundResource} not found!");
                return null;
            }

            byte[] audioData = new byte[stream.Length];
            stream.Read(audioData, 0, audioData.Length);
            return audioData;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to load audio data: {ex.Message}");
            return null;
        }
    }

    // Sets up audio playback resources for the specified device
    static void InitializeAudioResources(MMDevice device, byte[] audioData)
    {
        CleanupAudioResources();
        
        audioStream = new MemoryStream(audioData);
        audioReader = new WaveFileReader(audioStream);
        
        outputDevice = new WasapiOut(device, AudioClientShareMode.Shared, true, WasapiBufferMs);
        
        outputDevice.PlaybackStopped += (sender, args) => 
        {
            if (args.Exception != null)
            {
                Console.WriteLine($"Playback error: {args.Exception.Message}");
            }
        };
        
        outputDevice.Init(audioReader);
    }

    // Properly disposes all audio resources
    static void CleanupAudioResources()
    {
        if (outputDevice != null)
        {
            outputDevice.Stop();
            outputDevice.Dispose();
            outputDevice = null;
        }
        
        if (audioReader != null)
        {
            audioReader.Dispose();
            audioReader = null;
        }
        
        if (audioStream != null)
        {
            audioStream.Dispose();
            audioStream = null;
        }
    }

    // Plays a short beep sound non-blocking
    static void PlayBeep()
    {
        if (outputDevice == null || audioReader == null)
        {
            Console.WriteLine("Cannot play beep: Audio resources not initialized");
            return;
        }

        try
        {
            // Stop any current playback before starting new one
            if (outputDevice.PlaybackState == PlaybackState.Playing)
            {
                outputDevice.Stop();
            }
            
            // Reset position and play
            audioReader.Position = 0;
            outputDevice.Play();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error playing beep: {ex.Message}");
        }
    }
}
