using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace SixTransporter.UploadEngine
{
    public class UploadTaskInfo
    {
        public string FilePath { get; set; }

        public string UploadUrl { get; set; }

        public long FileSize { get; set; }

        public string Token { get; set; }

        public string FileHash { get; set; }

        public int Threads { get; set; } = 1;

        public string Uuid { get; set; } = Guid.NewGuid().ToString();

        public List<UploadBlock> BlockList { get; set; } = new List<UploadBlock>();

        public void Init()
        {
            BlockList.Clear();
            var info = new FileInfo(FilePath);
            FileSize = info.Length;
            //16MB
            const long blockSize = 16 * 1024 * 1024L;
            if (FileSize <= blockSize)
            {
                BlockList.Add(new UploadBlock()
                {
                    BeginOffset = 0,
                    EndOffset = FileSize,
                    Id = 0,
                    BlockSize = FileSize
                });
                return;
            }
            var temp = 0L;
            while (temp + blockSize < FileSize)
            {
                BlockList.Add(new UploadBlock()
                {
                    BeginOffset = temp,
                    EndOffset = temp + blockSize,
                    Id = BlockList.Count,
                    BlockSize = blockSize
                });
                temp += blockSize;
            }

            BlockList.Add(new UploadBlock()
            {
                BeginOffset = temp,
                EndOffset = FileSize,
                Id = BlockList.Count,
                BlockSize = FileSize - temp
            });
        }
        public void Save(string path)
        {
            File.WriteAllText(path, JsonConvert.SerializeObject(this, Formatting.Indented));
        }
        public static UploadTaskInfo Load(string path)
        {
            return JsonConvert.DeserializeObject<UploadTaskInfo>(File.ReadAllText(path));
        }
    }
    public class UploadBlock
    {
        public long BeginOffset { get; set; }

        public long EndOffset { get; set; }

        public int Id { get; set; }

        public string Ctx { get; set; }

        public bool Uploaded { get; set; }

        public long BlockSize { get; set; }

        [JsonIgnore]
        public bool Uploading { get; set; }

    }
}
