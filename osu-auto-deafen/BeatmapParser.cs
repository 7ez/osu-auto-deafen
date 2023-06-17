using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using OsuMemoryDataProvider.OsuMemoryModels.Direct;
using OsuParsers.Decoders;

namespace osu_auto_deafen
{
    public class BeatmapParser
    {
        // in order to save up on I/O operations.
        private Dictionary<int, float> _lengthCache = new Dictionary<int, float>();
        private string _songsPath;
        
        public BeatmapParser(Process osuProcess)
        {
            var osuPath = Path.GetDirectoryName(osuProcess.MainModule.FileName);
            var osuCfg = Path.Combine(osuPath, $"osu!.{Environment.UserName}.cfg");

            foreach (var line in File.ReadAllText(osuCfg).Split('\n'))
            {
                if (line.StartsWith("BeatmapDirectory"))
                {
                    var songsFolder = line.Split('=')[1].Trim();
                    _songsPath = Path.IsPathRooted(songsFolder) ? songsFolder : Path.Combine(osuPath, songsFolder)
                    break;
                }
            }

            if (_songsPath == null)
                throw new DirectoryNotFoundException("osu! Songs folder not found.");
        }

        public float GetBeatmapLength(CurrentBeatmap currentBeatmap)
        {
            if (_lengthCache.TryGetValue(currentBeatmap.Id, out var length))
                return length;
            
            var bmap = BeatmapDecoder.Decode(Path.Combine(_songsPath, currentBeatmap.FolderName,
                currentBeatmap.OsuFileName));

            // not sure if we need this?
            if (bmap == null)
                throw new Exception("Beatmap parse error.");

            var mapLength = bmap.GeneralSection.Length / 1000f;
            _lengthCache.Add(currentBeatmap.Id, mapLength);
            return mapLength;
        }
    }
}
