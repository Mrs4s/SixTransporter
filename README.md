# SixTransporter
CSharp 6盘上传下载SDK

## 上传
```
var taskInfo = new UploadTaskInfo()
{
  FilePath = "xx", // 本地文件路径
  Threads = 4, // 线程数
  Token = "xxx", // 上传Token
  UploadUrl = "xxx" // 上传Url
};
taskInfo.Init(); // 初始化，计算分块信息等.
taskInfo.Save("xx"); // 将分块和进度信息保存到文件
var task = new SixUploader(taskInfo);
task.UploadStatusChangedEvent += (oldStatus,newStatus,sender) => // 传输状态改变事件
{
  Console.WriteLine($"Upload task {sender.Info.FilePath} status changed {oldStatus}->{newStatus}");
  if(newStatus == UploadTaskStatusEnum.Paused)
    sender.Info.Save("xx"); // 暂停后保存进度
};
Console.WriteLine("Upload speed: "+ task.Speed); //获取上传速度
Console.WriteLine("Upload progress rate: "+ (task.UploadedSize / (float) task.Info.FileSize * 100).ToString("F") +"%"); //进度需要自己计算
task.StartUpload();
```
## 恢复上传
```
var taskInfo = UploadTaskInfo.Load("xxx"); // 从文件恢复进度
var task = new SixUploader(taskInfo);
task.StartUpload();
```

## 下载
```
var taskInfo = new DownloadTaskInfo()
{
  DownloadUrl = "xx", // 下载链接，可以为null，任务开始前再赋值初始化
  DownloadPath = "xx.file",
  Threads = 4,
  Limiter = new SpeedLimiter { Limit = 1024 } // 下载速度限制器，单位byte
};
var task = new HttpDownloader(taskInfo); // 下载默认会在StartDownload函数初始化, 保存下载进度文件到file.downloading文件
task.StartDownload();
```
