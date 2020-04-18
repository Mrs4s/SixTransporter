using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace SixTransporter.UploadEngine
{
    public class SixUploader
    {
        public UploadTaskInfo Info { get; }

        public long UploadedSize => _uploadedSize > Info.FileSize ? Info.FileSize : _uploadedSize;

        public long Speed { get; private set; }

        public UploadTaskStatusEnum Status
        {
            get => _status;
            set
            {
                if (value != _status)
                {
                    UploadStatusChangedEvent?.Invoke(_status, value, this);
                    _status = value;
                }
            }
        }

        private UploadTaskStatusEnum _status = UploadTaskStatusEnum.Waiting;

        public event Action<UploadTaskStatusEnum, UploadTaskStatusEnum, SixUploader> UploadStatusChangedEvent;

        private long _startSize;
        private DateTime _startTime;
        private long _uploadedSize = 0;

        public List<UploadThread> Threads { get; private set; }

        public SixUploader(UploadTaskInfo info)
        {
            Info = info;
        }

        public void StartUpload()
        {
            if (Info == null)
            {
                Status = UploadTaskStatusEnum.Faulted;
                return;
            }
            if (Status == UploadTaskStatusEnum.Uploading) return;
            Status = UploadTaskStatusEnum.Uploading;
            if (Info.Threads > Info.BlockList.Count(v => !v.Uploaded && !v.Uploading))
                Info.Threads = Info.BlockList.Count(v => !v.Uploaded && !v.Uploading);
            Threads?.ForEach(v => v.Stop());
            Threads = new List<UploadThread>();
            for (var i = 0; i < Info.Threads; i++)
            {
                var thread = new UploadThread(Info, Info.BlockList.First(v => !v.Uploaded && !v.Uploading).Id);
                thread.BlockUploadCompletedEvent += BlockUploadCompletedEvent;
                thread.ChunkUploadCompletedEvent += ChunkUploadCompletedEvent;
                thread.StartUpload();
                Threads.Add(thread);
            }
            _startTime = DateTime.Now;
            _uploadedSize = Info.BlockList.Count(v => v.Uploaded) * 16 * 1024 * 1024;
            _startSize = UploadedSize;
            new Thread(ReportUploadSpeed) { IsBackground = true }.Start();
        }

        private void ReportUploadSpeed()
        {
            while (Status == UploadTaskStatusEnum.Uploading)
            {
                Thread.Sleep(1000);
                if (Status == UploadTaskStatusEnum.Uploading)
                {
                    var uploadedSize = UploadedSize - _startSize;
                    if (uploadedSize / (DateTime.Now - _startTime).TotalSeconds > 0)
                    {
                        Speed = (long)(uploadedSize / (DateTime.Now - _startTime).TotalSeconds);
                    }
                }
            }
            Speed = 0L;
        }

        private async void BlockUploadCompletedEvent(UploadThread sender)
        {
            lock (this)
            {
                Threads.Remove(sender);
                Info.BlockList[sender.BlockId].Uploading = false;
                if (Info.BlockList.Any(v => !v.Uploaded && !v.Uploading))
                {
                    var thread = new UploadThread(Info, Info.BlockList.First(v => !v.Uploaded && !v.Uploading).Id);
                    thread.BlockUploadCompletedEvent += BlockUploadCompletedEvent;
                    thread.ChunkUploadCompletedEvent += ChunkUploadCompletedEvent;
                    thread.StartUpload();
                    Threads.Add(thread);
                    return;
                }

                if (Info.BlockList.Any(v => v.Uploading))
                    return;
            }
            using (var client = new HttpClient())
            {
                client.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", Info.Token);
                client.DefaultRequestHeaders.Add("UploadBatch", Info.Uuid);
                var body = string.Join(",", Info.BlockList.Select(v => v.Ctx));
                //Console.WriteLine(body);
                var result = await client.PostAsync($"{Info.UploadUrl}/mkfile/{Info.FileSize}", new StringContent(body));
                try
                {
                    var json = JObject.Parse(await result.Content.ReadAsStringAsync());
                    //LogHelper.Debug(json.ToString());
                    if (json["code"] != null)
                    {
                        Status = UploadTaskStatusEnum.Faulted;
                        return;
                    }
                    Status = UploadTaskStatusEnum.Completed;
                }
                catch (Exception)
                {
                    //LogHelper.Error("MKFile error", ex);
                    Status = UploadTaskStatusEnum.Faulted;
                }
            }
        }

        private void ChunkUploadCompletedEvent(UploadThread arg1, long arg2)
        {
            _uploadedSize += arg2;
        }
    }
    public enum UploadTaskStatusEnum
    {
        Waiting,
        Uploading,
        Paused,
        Completed,
        Hashing,
        Faulted
    }
}
