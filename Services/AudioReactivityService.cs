using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Artemis.Plugins.LayerBrushes.JavascriptCanvas.Services
{
    public class AudioReactivityService : IDisposable
    {
        private readonly object _lock = new object();
        private Thread? _audioThread;
        private bool _isRunning;
        private List<object> _captures = new List<object>();
        private List<MethodInfo> _stopMethods = new List<MethodInfo>();

        public float[] FrequencyBands { get; private set; } = new float[32];
        public float Bass { get; private set; }
        public float Midrange { get; private set; }
        public float Treble { get; private set; }
        public float Volume { get; private set; }
        public bool IsEnabled { get; private set; }

        private const int FftSize = 4096;
        private float[] _fftBuffer = new float[FftSize];
        private float[] _fftReal = new float[FftSize];
        private float[] _fftImag = new float[FftSize];
        private int _bufferPosition = 0;
        private int _channels = 2;
        private bool _receivedData = false;

        public AudioReactivityService()
        {
            IsEnabled = false;
        }

        public void Start()
        {
            if (_isRunning) return;

            lock (_lock)
            {
                if (_isRunning) return;

                _audioThread = new Thread(CaptureAllAudioProc)
                {
                    IsBackground = true,
                    Priority = ThreadPriority.AboveNormal,
                    Name = "AudioCaptureThread"
                };

                _isRunning = true;
                _audioThread.Start();
            }
        }

        private void CaptureAllAudioProc()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("🎵 Searching for device with ALL mixed audio...");

                Assembly? nAudioWasapiAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "NAudio.Wasapi");

                Assembly? nAudioCoreAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "NAudio.Core");

                if (nAudioWasapiAssembly == null || nAudioCoreAssembly == null)
                {
                    System.Diagnostics.Debug.WriteLine("❌ NAudio not found");
                    return;
                }

                Type? enumeratorType = nAudioCoreAssembly.GetTypes()
                    .FirstOrDefault(t => t.Name == "MMDeviceEnumerator");

                Type? captureType = nAudioWasapiAssembly.GetType("NAudio.Wave.WasapiLoopbackCapture");

                if (enumeratorType == null)
                {
                    System.Diagnostics.Debug.WriteLine("⚠️ Using default device");
                    UseDefaultCapture(captureType!);
                    return;
                }

                object? enumerator = Activator.CreateInstance(enumeratorType);
                MethodInfo? enumDevicesMethod = enumeratorType.GetMethod("EnumerateAudioEndPoints");

                if (enumDevicesMethod == null)
                {
                    UseDefaultCapture(captureType!);
                    return;
                }

                // Get all devices
                object? deviceCollection = enumDevicesMethod.Invoke(enumerator, new object[] { 0, 1 });
                if (deviceCollection == null)
                {
                    UseDefaultCapture(captureType!);
                    return;
                }

                PropertyInfo? countProp = deviceCollection.GetType().GetProperty("Count");
                int deviceCount = (int)(countProp?.GetValue(deviceCollection) ?? 0);

                System.Diagnostics.Debug.WriteLine($"📋 Scanning {deviceCount} devices:");

                var indexer = deviceCollection.GetType().GetProperty("Item");
                object? bestDevice = null;
                string? bestName = null;

                // Priority list - look for devices that have MIXED audio
                var keywords = new[]
                {
            "Streaming",     // Sonar Streaming = mixed output
            "Auxiliary",     // Sonar Aux
            "Auxilliary",
            "Aux",
            "Monitor",       // Some systems have monitor device
            "Stereo Mix",    // Windows stereo mix
            "What U Hear",   // Realtek's version
            "Wave",          // Some creative cards
        };

                for (int i = 0; i < deviceCount; i++)
                {
                    object? device = indexer?.GetValue(deviceCollection, new object[] { i });
                    if (device == null) continue;

                    PropertyInfo? friendlyNameProp = device.GetType().GetProperty("FriendlyName");
                    string? deviceName = friendlyNameProp?.GetValue(device) as string;

                    System.Diagnostics.Debug.WriteLine($"  {i + 1}. {deviceName}");

                    // Check if this is a good candidate
                    foreach (var keyword in keywords)
                    {
                        if (deviceName?.Contains(keyword, StringComparison.OrdinalIgnoreCase) == true)
                        {
                            bestDevice = device;
                            bestName = deviceName;
                            System.Diagnostics.Debug.WriteLine($"  ✅ FOUND: {deviceName}");
                            goto DeviceFound; // Exit both loops
                        }
                    }
                }

            DeviceFound:

                if (bestDevice != null)
                {
                    System.Diagnostics.Debug.WriteLine($"✅ Using: {bestName}");
                    var constructor = captureType?.GetConstructor(new[] { bestDevice.GetType() });
                    if (constructor != null)
                    {
                        var capture = constructor.Invoke(new[] { bestDevice });
                        SetupCapture(capture!, captureType!, bestName!);
                        return;
                    }
                }

                // No special device found
                System.Diagnostics.Debug.WriteLine("❌ No mixed audio device found");
                System.Diagnostics.Debug.WriteLine("💡 Available solutions:");
                System.Diagnostics.Debug.WriteLine("   1. Enable 'Stereo Mix' in Windows Sound settings → Recording tab");
                System.Diagnostics.Debug.WriteLine("   2. Use SteelSeries Sonar 'Streaming' output as Windows default");
                System.Diagnostics.Debug.WriteLine("   3. Route all apps to one Sonar channel (Gaming or Media)");
                System.Diagnostics.Debug.WriteLine("");
                System.Diagnostics.Debug.WriteLine("⚠️ Using default device (will only capture one Sonar channel)");

                UseDefaultCapture(captureType!);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Error: {ex.Message}");
            }
        }

        private void UseDefaultCapture(Type captureType)
        {
            var capture = Activator.CreateInstance(captureType);
            if (capture != null)
            {
                SetupCapture(capture, captureType, "Default");
            }
        }

        private void SetupCapture(object capture, Type captureType, string deviceName)
        {
            try
            {
                PropertyInfo? waveFormatProp = captureType.GetProperty("WaveFormat");
                if (waveFormatProp != null)
                {
                    var waveFormat = waveFormatProp.GetValue(capture);
                    var channelsProp = waveFormat?.GetType().GetProperty("Channels");
                    _channels = (int)(channelsProp?.GetValue(waveFormat) ?? 2);
                    System.Diagnostics.Debug.WriteLine($"📊 {waveFormat}");
                }

                EventInfo? dataAvailableEvent = captureType.GetEvent("DataAvailable");
                if (dataAvailableEvent != null)
                {
                    MethodInfo handlerMethod = GetType().GetMethod(nameof(OnDataAvailable),
                        BindingFlags.NonPublic | BindingFlags.Instance)!;

                    var handler = Delegate.CreateDelegate(dataAvailableEvent.EventHandlerType!, this, handlerMethod);
                    dataAvailableEvent.AddEventHandler(capture, handler);
                }

                var startMethod = captureType.GetMethod("StartRecording");
                var stopMethod = captureType.GetMethod("StopRecording");

                startMethod?.Invoke(capture, null);

                _captures.Clear();
                _stopMethods.Clear();
                _captures.Add(capture);
                if (stopMethod != null) _stopMethods.Add(stopMethod);

                System.Diagnostics.Debug.WriteLine($"✅ Capturing from: {deviceName}");
                IsEnabled = true;

                while (_isRunning)
                {
                    Thread.Sleep(100);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Setup error: {ex.Message}");
            }
            finally
            {
                for (int i = 0; i < _captures.Count; i++)
                {
                    try { _stopMethods[i]?.Invoke(_captures[i], null); } catch { }
                }
            }
        }




        private void UseSingleDevice(Assembly nAudioWasapiAssembly)
        {
            Type? captureType = nAudioWasapiAssembly.GetType("NAudio.Wave.WasapiLoopbackCapture");
            if (captureType != null)
            {
                var capture = Activator.CreateInstance(captureType);
                SetupAndStartCapture(capture!, captureType, "Default Device");
            }
        }

        private void SetupAndStartCapture(object capture, Type captureType, string deviceName)
        {
            try
            {
                // Get format
                PropertyInfo? waveFormatProp = captureType.GetProperty("WaveFormat");
                if (waveFormatProp != null)
                {
                    var waveFormat = waveFormatProp.GetValue(capture);
                    var channelsProp = waveFormat?.GetType().GetProperty("Channels");
                    _channels = (int)(channelsProp?.GetValue(waveFormat) ?? 2);

                    System.Diagnostics.Debug.WriteLine($"📊 Device: {deviceName}");
                    System.Diagnostics.Debug.WriteLine($"📊 Format: {waveFormat}");
                }

                // Subscribe
                EventInfo? dataAvailableEvent = captureType.GetEvent("DataAvailable");
                if (dataAvailableEvent != null)
                {
                    MethodInfo handlerMethod = GetType().GetMethod(nameof(OnDataAvailable),
                        BindingFlags.NonPublic | BindingFlags.Instance)!;

                    var handler = Delegate.CreateDelegate(dataAvailableEvent.EventHandlerType!, this, handlerMethod);
                    dataAvailableEvent.AddEventHandler(capture, handler);
                }

                var startMethod = captureType.GetMethod("StartRecording");
                var stopMethod = captureType.GetMethod("StopRecording");

                startMethod?.Invoke(capture, null);

                _captures.Add(capture);
                if (stopMethod != null) _stopMethods.Add(stopMethod);

                System.Diagnostics.Debug.WriteLine($"✅✅✅ Capturing from: {deviceName}");
                System.Diagnostics.Debug.WriteLine("🎵 Play music from ANY source and watch!");

                IsEnabled = true;

                // Monitor
                int waitSeconds = 0;
                while (_isRunning)
                {
                    Thread.Sleep(1000);
                    waitSeconds++;

                    if (!_receivedData && waitSeconds == 5)
                    {
                        System.Diagnostics.Debug.WriteLine("⚠️ No audio data after 5 seconds");
                        System.Diagnostics.Debug.WriteLine($"💡 Current device: {deviceName}");
                        System.Diagnostics.Debug.WriteLine("💡 Make sure audio is playing AND audible");
                    }

                    if (_receivedData && waitSeconds == 2)
                    {
                        System.Diagnostics.Debug.WriteLine("✅ Audio reactivity WORKING! 🎉");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Setup error: {ex.Message}");
            }
            finally
            {
                for (int i = 0; i < _captures.Count; i++)
                {
                    try { _stopMethods[i]?.Invoke(_captures[i], null); } catch { }
                }
            }
        }

        private void OnDataAvailable(object? sender, object e)
        {
            if (!IsEnabled) return;
            try
            {
                Type eventArgsType = e.GetType();
                PropertyInfo? bufferProp = eventArgsType.GetProperty("Buffer");
                PropertyInfo? bytesRecordedProp = eventArgsType.GetProperty("BytesRecorded");

                if (bufferProp != null && bytesRecordedProp != null)
                {
                    byte[]? buffer = bufferProp.GetValue(e) as byte[];
                    int bytesRecorded = (int)(bytesRecordedProp.GetValue(e) ?? 0);

                    if (bytesRecorded > 0 && buffer != null)
                    {
                        if (!_receivedData)
                        {
                            _receivedData = true;
                            System.Diagnostics.Debug.WriteLine($"✅ Audio data flowing! ({bytesRecorded} bytes)");
                        }

                        ProcessAudioData(buffer, bytesRecorded);
                    }
                }
            }
            catch { }
        }

        private void ProcessAudioData(byte[] buffer, int bytesRecorded)
        {
            if (!IsEnabled) return;
            lock (_lock)
            {
                try
                {
                    int bytesPerSample = 4;
                    int bytesPerFrame = bytesPerSample * _channels;
                    int framesRecorded = bytesRecorded / bytesPerFrame;

                    for (int i = 0; i < framesRecorded && _bufferPosition < FftSize; i++)
                    {
                        float sum = 0;
                        for (int ch = 0; ch < _channels; ch++)
                        {
                            int offset = (i * bytesPerFrame) + (ch * bytesPerSample);
                            if (offset + 4 <= buffer.Length)
                            {
                                sum += BitConverter.ToSingle(buffer, offset);
                            }
                        }
                        _fftBuffer[_bufferPosition++] = sum / _channels;
                    }

                    if (_bufferPosition >= FftSize)
                    {
                        PerformFFT();
                        _bufferPosition = 0;
                    }
                }
                catch { }
            }
        }

        private void PerformFFT()
        {
            if (!IsEnabled) return;

            for (int i = 0; i < FftSize; i++)
            {
                float window = (float)(0.54 - 0.46 * Math.Cos(2 * Math.PI * i / (FftSize - 1)));
                _fftReal[i] = _fftBuffer[i] * window;
                _fftImag[i] = 0;
            }

            FFT(_fftReal, _fftImag, FftSize);
            UpdateFrequencyBands();

            Bass = FrequencyBands.Take(8).Average();
            Midrange = FrequencyBands.Skip(8).Take(12).Average();
            Treble = FrequencyBands.Skip(20).Take(12).Average();
            Volume = FrequencyBands.Average();
        }

        private void UpdateFrequencyBands()
        {
            if (!IsEnabled)
            {
                // Reset all values when disabled
                Bass = 0;
                Midrange = 0;
                Treble = 0;
                Volume = 0;
                Array.Fill(FrequencyBands, 0);
                return;
            }

            int samplesPerBand = (FftSize / 2) / 32;

            for (int i = 0; i < 32; i++)
            {
                float sum = 0;
                int start = i * samplesPerBand;
                int end = Math.Min((i + 1) * samplesPerBand, FftSize / 2);

                for (int j = start; j < end; j++)
                {
                    float magnitude = (float)Math.Sqrt(_fftReal[j] * _fftReal[j] + _fftImag[j] * _fftImag[j]);
                    sum += magnitude;
                }

                FrequencyBands[i] = (end > start) ? sum / (end - start) : 0;
                FrequencyBands[i] = Math.Clamp(FrequencyBands[i] * 8f, 0f, 1f);
            }
        }

        private void FFT(float[] real, float[] imag, int n)
        {
            int bits = (int)Math.Log(n, 2);

            for (int i = 1; i < n - 1; i++)
            {
                int j = ReverseBits(i, bits);
                if (i < j)
                {
                    (real[i], real[j]) = (real[j], real[i]);
                    (imag[i], imag[j]) = (imag[j], imag[i]);
                }
            }

            for (int len = 2; len <= n; len <<= 1)
            {
                double angle = -2 * Math.PI / len;
                float wlenReal = (float)Math.Cos(angle);
                float wlenImag = (float)Math.Sin(angle);

                for (int i = 0; i < n; i += len)
                {
                    float wReal = 1;
                    float wImag = 0;

                    for (int j = 0; j < len / 2; j++)
                    {
                        float uReal = real[i + j];
                        float uImag = imag[i + j];
                        float vReal = real[i + j + len / 2];
                        float vImag = imag[i + j + len / 2];

                        float tReal = wReal * vReal - wImag * vImag;
                        float tImag = wReal * vImag + wImag * vReal;

                        real[i + j] = uReal + tReal;
                        imag[i + j] = uImag + tImag;
                        real[i + j + len / 2] = uReal - tReal;
                        imag[i + j + len / 2] = uImag - tImag;

                        float tempW = wReal;
                        wReal = wReal * wlenReal - wImag * wlenImag;
                        wImag = tempW * wlenImag + wImag * wlenReal;
                    }
                }
            }
        }

        private int ReverseBits(int n, int bits)
        {
            int reversed = 0;
            for (int i = 0; i < bits; i++)
            {
                reversed = (reversed << 1) | (n & 1);
                n >>= 1;
            }
            return reversed;
        }

        public void Dispose()
        {
            _isRunning = false;
            for (int i = 0; i < _captures.Count; i++)
            {
                try { _stopMethods[i]?.Invoke(_captures[i], null); } catch { }
            }
            _audioThread?.Join(2000);
        }
    }

    public class AudioData
    {
        public float[] FrequencyBands { get; set; } = Array.Empty<float>();
        public float Bass { get; set; }
        public float Midrange { get; set; }
        public float Treble { get; set; }
        public float Volume { get; set; }
    }
}
