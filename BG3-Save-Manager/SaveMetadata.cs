using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace BG3_Save_Manager
{
    public class SaveMetadata
    {
        public enum SaveType
        {
            MANUAL,
            QUICK,
            AUTO
        }
        public enum DifficultyType
        {
            EXPLORER,
            BALANCED,
            TACTICIAN
        }

        public string LeaderName { get; }
        public SaveType SaveGameType { get; }
        public UInt64 SaveTime { get; }
        public UInt32 Timestamp { get; }
        public int Seed { get; }
        public string GameVersion { get; }
        public DifficultyType Difficulty { get; }
        public string GameSessionId { get; }
        public string FolderName { get; set; }
        public string FileName { get; set; }

        public BitmapImage ThumbnailUri { get
            {
                var image = new BitmapImage();
                image.BeginInit();
                image.UriSource = new Uri(Path.Combine(Properties.Settings.Default.saveFolderPath, FolderName, FileName + ".WebP"));
                image.DecodePixelHeight = 150;
                image.EndInit();
                return image;
            }
        }

        public BitmapImage ImageUri
        {
            get
            {
                var image = new BitmapImage(new Uri(Path.Combine(Properties.Settings.Default.saveFolderPath, FolderName, FileName + ".WebP")));
                return image;
            }
        }

        public DateTime ConvertedTime { 
            get
            {
                DateTime time = new(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                time = time.AddSeconds(SaveTime).ToLocalTime();
                return time;
            } 
        }

        public SaveMetadata(string gameSessionId, string leaderName, ulong saveTime, DifficultyType difficulty, SaveType saveType, uint timePlayed, int seed, string gameVersion)
        {
            GameSessionId = gameSessionId ?? throw new ArgumentNullException(nameof(gameSessionId));
            LeaderName = leaderName ?? throw new ArgumentNullException(nameof(leaderName));
            SaveTime = saveTime;
            Difficulty = difficulty;
            SaveGameType = saveType;
            Timestamp = timePlayed;
            Seed = seed;
            GameVersion = gameVersion;
        }
    }
}
