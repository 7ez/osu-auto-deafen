using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using OsuMemoryDataProvider;
using OsuMemoryDataProvider.OsuMemoryModels.Direct;
using WindowsInput;
using WindowsInput.Events;
using WindowsInput.Native;

namespace osu_auto_deafen
{
    internal class Program
    {
        private static readonly Dictionary<int, float> _lengthCache = new Dictionary<int, float>();
        private static readonly WebClient _wc = new WebClient();
        private static readonly Config _config = new Config("./config.ini");
        private static readonly KeyCode[] KeyCodes = _config.GetKeyCodes();
        private static StructuredOsuMemoryReader _osuMemReader;
        private static bool _isDeafened;
        private static bool _isSimulating;
        private static int _countKeysPressed;

        private static float GetMapLength(int MapId)
        {
            if (_lengthCache.TryGetValue(MapId, out var length)) return length;
            
            var mapData = _wc.DownloadString($"https://osu.direct/api/b/{MapId}");
            
            foreach (var line in mapData.Split('\n'))
            {
                var new_line = line.Trim();
                if (new_line.StartsWith("\"TotalLength\":"))
                {
                    length = float.Parse(new_line.Split(':')[1].Trim().Replace(",", ""));
                    _lengthCache.Add(MapId, length);
                    return length;
                }
            }

            return 0f;
        }

        public async static void DeafenOrUndeafen()
        {
            _isDeafened = !_isDeafened;
            _isSimulating = true;
            if (KeyCodes.Length > 1)
                await Simulate
                    .Events()
                    .ClickChord(KeyCodes)
                    .Invoke();
            else
                await Simulate
                    .Events()
                    .Click(KeyCodes[0])
                    .Invoke();
            _isSimulating = false;
        }
        
        [STAThread]
        public static void Main()
        {
            Console.WriteLine("Auto-deafen for osu! by Aochi");
            Console.WriteLine("Keys for deafening: " + string.Join(" + ", KeyCodes));
            Console.WriteLine($"Deafen point: {_config.Get<string>("DeafenPoint")}%");
            Console.WriteLine("Starting...");
            
            Start:
            while (true)
            {
                if (Process.GetProcessesByName("osu!").Length == 0)
                {
                    Console.WriteLine("osu! is not running.");
                    Console.WriteLine("Waiting for osu! to start...");
                    Thread.Sleep(3000);
                }
                else
                {
                    break;
                }
            }

            Console.WriteLine("osu! is running!");
            _osuMemReader = StructuredOsuMemoryReader.Instance.GetInstanceForWindowTitleHint("osu!");

            using (var keyboard = Capture.Global.KeyboardAsync())
            {
                keyboard.KeyDown += (sender, eventArgs) =>
                {
                    if (KeyCodes.Contains(eventArgs.Data.Key) && !_isSimulating)
                        _countKeysPressed++;
                    
                    if (_countKeysPressed == KeyCodes.Length)
                        _isDeafened = !_isDeafened;
                };
                keyboard.KeyUp += (sender, eventArgs) =>
                {
                    if (KeyCodes.Contains(eventArgs.Data.Key) && !_isSimulating)
                        _countKeysPressed--;
                };
                
                while (true)
                {
                    var generalData = new GeneralData();
                    var beatmap = new CurrentBeatmap();

                    if (!_osuMemReader.TryRead(generalData))
                    {
                        Thread.Sleep(3000);

                        if (Process.GetProcessesByName("osu!").Length == 0)
                            goto Start;

                        continue;
                    }

                    if (generalData.OsuStatus == OsuMemoryStatus.Playing)
                    {
                        if (!_osuMemReader.TryRead(beatmap))
                        {
                            Thread.Sleep(3000);
                            continue;
                        }

                        var mapLength = GetMapLength(beatmap.Id);
                        var audioTimeSeconds = (float)generalData.AudioTime / 1000;

                        if (!_isDeafened)
                        {
                            if (audioTimeSeconds >= mapLength * _config.GetDeafenPoint())
                                DeafenOrUndeafen();
                        }
                        else
                        {
                            if (mapLength * _config.GetDeafenPoint() > audioTimeSeconds)
                                DeafenOrUndeafen();
                        }
                    }
                    else
                    {
                        if (_isDeafened)
                            DeafenOrUndeafen();
                    }

                    Thread.Sleep(500);
                }
            }
        }
    }
}