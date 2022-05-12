# SelfKeeper
## 1. Intro

A library that lets applications run as their own watchdog. 

允许应用程序作为自己的看门狗运行的库。

--------

 - 支持部分信号传递到工作进程，可以在某些情况下执行优雅关闭；
 - 原始的控制台输出颜色会被保留；

--------

### 工作原理
通过命令行参数判断当前是否是工作进程，如果不是，则复制当前进程的启动设置，添加相关命令行参数后启动工作进程，并监控其运行状态。

关键词
 - 工作进程(Worker Process): 运行程序实际逻辑的进程
 - 主进程(Host Process): 启动并监控工作进程的进程

## 2. 如何使用

### 2.1 安装`Nuget`包

```shell
dotnet add package SelfKeeper --prerelease
```

### 2.2 启用SelfKeeper
在程序入口处添加处理代码
```C#
KeepSelf.Handle(args);
```
或进行更多配置
```C#
KeepSelf.Handle(args, options =>
{
    options.RemoveFlag(KeepSelfFeatureFlag.SkipWhenDebuggerAttached | KeepSelfFeatureFlag.DisableForceKillByHost);   //配置功能
    options.StartFailRetryDelay = TimeSpan.FromSeconds(1);  //配置启动失败的重试延时
    options.RestartDelay = TimeSpan.FromSeconds(1); //进程退出后的重启延时
    options.Logger = null;  //配置日志记录器
    options.WorkerProcessOptionsCommandArgumentName = "--worker-process-options"; //自定义工作进程选项的参数名
    options.NoKeepSelfCommandArgumentName = "--no-keep-self"; //自定义不启用 KeepSelf 的参数名
    options.NoKeepSelfEnvironmentVariableName = "NoWatchDog"; //自定义不启用 KeepSelf 的环境变量名
});
```
 - 主进程会在 `KeepSelf.Handle` 阻塞，启动并监控工作进程；
 - 主进程在退出时会在 `KeepSelf.Handle` 内调用 `Environment.Exit` 退出，不会执行后面的代码；
