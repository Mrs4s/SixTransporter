using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SixTransporter.DownloadEngine
{
    public class DownloadTaskInfo
    {
        public string DownloadUrl { get; set; }

        public string DownloadPath { get; set; }

        public long ContentSize { get; set; }

        public long DownloadedSize { get; set; }

        public bool Downloaded { get; set; }

        public int Threads { get; set; }

        public long BlockSize { get; set; } = 1024 * 1024 * 250;

        public int MaxRetry { get; set; } = 10;

        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>()
            { ["User-Agent"] = "Six-Pan download engine" };

        public SpeedLimiter Limiter { get; set; } = new SpeedLimiter() { Limit = 0 };

        public List<DownloadBlock> BlockList { get; } = new List<DownloadBlock>();

        /// <summary>
        /// 初始化分块信息
        /// </summary>
        public void Init(string path)
        {
            Init();
            Save(path);
        }

        /// <summary>
        /// 初始化分块信息
        /// </summary>
        public void Init()
        {
            var temp = 0L;
            BlockList.Clear();
            while (temp + BlockSize < ContentSize)
            {
                BlockList.Add(new DownloadBlock
                {
                    BeginOffset = temp,
                    EndOffset = temp + BlockSize - 1
                });
                temp += BlockSize;
            }

            if (temp < ContentSize - 1)
            {
                BlockList.Add(new DownloadBlock
                {
                    BeginOffset = temp,
                    EndOffset = ContentSize - 1,
                });
            }
        }

        /// <summary>
        /// 保存
        /// </summary>
        /// <param name="path"></param>
        public void Save(string path)
        {
            File.WriteAllText(path, JObject.Parse(JsonConvert.SerializeObject(this)).ToString());
        }

        public static DownloadTaskInfo Load(string file)
        {
            return JsonConvert.DeserializeObject<DownloadTaskInfo>(File.ReadAllText(file));
        }
    }
    public class DownloadBlock
    {
        public long BeginOffset { get; set; }

        public long EndOffset { get; set; }

        public long DownloadedSize { get; set; }

        [JsonIgnore]
        public bool Downloading { get; set; }

        /// <summary>
        /// 是否完成
        /// </summary>
        public bool Downloaded { get; set; }
    }

    public class SpeedLimiter
    {
        public long Limit { get; set; }

        [JsonIgnore]
        public long Current { get; private set; }

        [JsonIgnore]
        public bool Running { get; private set; }

        public void Run()
        {
            if (Running || Limit <= 0) return;
            Running = true;
            new Thread(() =>
                {
                    while (Running)
                    {
                        Thread.Sleep(100);
                        if (Current - (Limit / 10) < 0)
                            Current = 0;
                        else
                            Current -= Limit / 10;
                    }
                })
                { IsBackground = true }.Start();
        }

        public void Stop()
        {
            Running = false;
        }

        public void Downloaded(long size)
        {
            if (Running)
            {
                Current += size;
                while (Current > Limit)
                    Thread.Sleep(10);
            }
        }
    }
}
