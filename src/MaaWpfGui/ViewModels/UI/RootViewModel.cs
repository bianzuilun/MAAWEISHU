// <copyright file="RootViewModel.cs" company="MaaAssistantArknights">
// Part of the MaaWpfGui project, maintained by the MaaAssistantArknights team (Maa Team)
// Copyright (C) 2021-2026 MaaAssistantArknights Contributors
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU Affero General Public License v3.0 only as published by
// the Free Software Foundation, either version 3 of the License, or
// any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY
// </copyright>

#nullable enable

using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using HandyControl.Data;
using JetBrains.Annotations;
using MaaWpfGui.Configuration.Factory;
using MaaWpfGui.Helper;
using MaaWpfGui.Main;
using MaaWpfGui.Services;
using MaaWpfGui.ViewModels.UserControl.Settings;
using Microsoft.WindowsAPICodePack.Taskbar;
using Serilog;
using Stylet;
using Point = System.Windows.Point;

namespace MaaWpfGui.ViewModels.UI;

/// <summary>
/// The root view model.
/// </summary>
public class RootViewModel : Conductor<Screen>.Collection.OneActive
{
    private static readonly ILogger _logger = Log.ForContext<RootViewModel>();

    /// <inheritdoc/>
    protected override void OnViewLoaded()
    {
        InitViewModels();
        _ = InitProxy();

        // 宿醉彩蛋弹窗依赖 RootView 已渲染完毕的 Dialog 容器，
        // 必须在主窗口显示之后再弹出，否则弹不出
        // 必须在其他内容初始化之前执行，否则其他内容语言可能已经被初始化为非宿醉语言
        Instances.SettingsViewModel.HangoverEnd();

        if (Bootstrapper.IsDemoMode)
        {
            // README 截图演示模式：跳过版本不匹配提示、公告下载、
            // 完整性检查与配置损坏等弹窗，避免联网与弹窗干扰自动截图
            return;
        }

        if (Bootstrapper.IsCoreInitSkipped)
        {
            // UI 预览模式：Core 未加载时版本号比对必然触发版本不匹配错误弹窗，
            // 公告/完整性检查等联网弹窗同样干扰界面预览，一并跳过
            return;
        }

        ShowVersionMismatchWarningOnStartup();

        Task.Run(async () => {
            await Instances.AnnouncementDialogViewModel.CheckAndDownloadAnnouncement();
            if (Instances.AnnouncementDialogViewModel.DoNotRemindThisAnnouncementAgain)
            {
                return;
            }

            if (Instances.AnnouncementDialogViewModel.DoNotShowAnnouncement)
            {
                return;
            }

            if (Instances.AnnouncementDialogViewModel.AnnouncementInfo != string.Empty)
            {
                _ = Execute.OnUIThreadAsync(() => Instances.WindowManager.ShowWindow(Instances.AnnouncementDialogViewModel));
            }
        });

        _ = StartupIntegrityCheckAsync();

        // 主窗口已显示，此时弹窗不会导致 WPF 因无窗口而退出
        Task.Run(ConfigBrokenCheck);
        Task.Run(ToastNotificationCheck);
    }

    /// <summary>
    /// 启动时的完整性检查。
    /// 对照 filelist.txt 检查安装文件是否缺失，缺失时弹窗提示重新下载完整安装包。
    /// 资源已标记损坏时本检查整体跳过（扫描前后各检测一次标志），由资源损坏弹窗统一提示。
    /// 必须在主窗口显示之后执行，否则弹窗会成为唯一窗口，关闭时触发 WPF 退出。
    /// </summary>
    private static async Task StartupIntegrityCheckAsync()
    {
        if (Bootstrapper.IsResourceBroken)
        {
            _logger.Information("Skip startup integrity check, resource-broken dialog takes over");
            return;
        }

        var missingFiles = await Task.Run(ResourceIntegrityChecker.GetMissingFiles);

        // 后检测：资源在扫描期间才标记损坏（与 Init 并发）时也不再叠任何弹窗
        if (Bootstrapper.IsResourceBroken)
        {
            _logger.Information("Skip integrity check, resource-broken dialog takes over, {Count} file(s) missing", missingFiles.Count);
            return;
        }

        if (missingFiles.Count == 0)
        {
            return;
        }

        var shownFiles = string.Join(", ", missingFiles.Take(5));
        if (missingFiles.Count > 5)
        {
            shownFiles += ", …";
        }

        _logger.Warning("Installation is missing {Count} file(s)", missingFiles.Count);
        MessageBoxHelper.Show(
            LocalizationHelper.GetStringFormat("ResourceIntegrityCheckDesc", missingFiles.Count, shownFiles),
            LocalizationHelper.GetString("ResourceIntegrityCheckTitle"),
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    private static void ConfigBrokenCheck()
    {
        var recoveryMessage = ConfigFactory.ConsumePendingRecoveryMessage();
        if (recoveryMessage is not null)
        {
            MessageBoxHelper.Show(recoveryMessage, LocalizationHelper.GetString("ConfigurationBrokenCaption"), MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private static void ToastNotificationCheck()
    {
        if (!SettingsViewModel.NotificationSettings.UseNotify)
        {
            return;
        }

        var (isAvailable, detail) = ToastNotification.ToastNotificationCheck();
        if (!isAvailable)
        {
            GrowlHelper.Error(LocalizationHelper.GetStringFormat("ToastNotificationUnavailable", detail));
            _logger.Error(LocalizationHelper.GetStringFormat("ToastNotificationUnavailable", detail));
        }
    }

    private static void ShowVersionMismatchWarningOnStartup()
    {
        var uiVersion = VersionUpdateSettingsUserControlModel.UiVersion;
        var coreVersion = VersionUpdateSettingsUserControlModel.CoreVersion;
        if (!VersionHelper.IsDebugVersion() && uiVersion != coreVersion)
        {
            MessageBoxHelper.Show(
                LocalizationHelper.GetStringFormat("VersionMismatch", uiVersion, coreVersion),
                LocalizationHelper.GetString("Error"),
                iconKey: ResourceToken.FatalGeometry,
                iconBrushKey: ResourceToken.DangerBrush);
        }
    }

    private static async Task InitProxy()
    {
        try
        {
            await Task.Run(Instances.AsstProxy.Init);
        }
        catch (Exception e)
        {
            // 初始化失败除 Link Start 保持置灰外无任何表现，不落日志则无从定位（如 Core 与资源版本不匹配）
            _logger.Error(e, "Failed to init AsstProxy");
        }
    }

    private void InitViewModels()
    {
        Items.Add(Instances.TaskQueueViewModel);
        Items.Add(Instances.CopilotViewModel);
        Items.Add(Instances.ToolboxViewModel);
        Items.Add(Instances.SettingsViewModel);

        Instances.SettingsViewModel.UpdateWindowTitle(); // 在标题栏上显示模拟器和IP端口 必须在 Items.Add(settings)之后执行。
        ActiveItem = Instances.TaskQueueViewModel;
    }

    private string _windowTitle = "MAA";

    /// <summary>
    /// Gets or sets the window title.
    /// </summary>
    public string WindowTitle
    {
        get => _windowTitle;
        set => SetAndNotify(ref _windowTitle, value);
    }

    private (int Current, int Max)? _taskProgress;

    /// <summary>
    /// Gets or sets the TaskProgress.
    /// 0.0 to 1.0.
    /// 置 0 以隐藏进度条.
    /// </summary>
    public (int Current, int Max)? TaskProgress
    {
        get => _taskProgress;
        set {
            SetAndNotify(ref _taskProgress, value);

            Execute.OnUIThreadAsync(() => {
                if (Application.Current.MainWindow == null || !Application.Current.MainWindow.IsVisible)
                {
                    return;
                }

                try
                {
                    if (value is null)
                    {
                        TaskbarManager.Instance.SetProgressValue(0, 0, Application.Current.MainWindow);
                    }
                    else
                    {
                        TaskbarManager.Instance.SetProgressValue(value.Value.Current, value.Value.Max, Application.Current.MainWindow);
                    }
                }
                catch (Exception e)
                {
                    // 不知道会不会有异常，先捕获一下
                    Logger.Warning("TaskbarManager Exception: " + e.Message);
                }
            });
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether to scroll the window title.
    /// </summary>
    public bool WindowTitleScrollable { get; set => SetAndNotify(ref field, value); } = ConfigFactory.Root.Gui.WindowTitleScrollable;

    /// <summary>
    /// Gets or sets a value indicating whether to show close button.
    /// </summary>
    public bool ShowCloseButton { get; set => SetAndNotify(ref field, value); } = !ConfigFactory.Root.Gui.HideCloseButton;

    private bool _isWindowTopMost;

    public bool IsWindowTopMost
    {
        get => _isWindowTopMost;
        set {
            if (_isWindowTopMost == value)
            {
                return;
            }

            SetAndNotify(ref _isWindowTopMost, value);
        }
    }

    // UI 绑定的方法
    [UsedImplicitly]
    public void ToggleTopMostCommand()
    {
        IsWindowTopMost = !IsWindowTopMost;
    }

    /// <inheritdoc/>
    protected override void OnClose()
    {
        Bootstrapper.Shutdown();
    }

    private static readonly string[] _gitList =
    [
        "/Res/Img/EasterEgg/1.gif",
        "/Res/Img/EasterEgg/2.gif",
        "/Res/Img/EasterEgg/3.gif",
    ];

    private static int _gifIndex = -1;

    private static string _gifPath = string.Empty;

    public string GifPath
    {
        get => _gifPath;
        set => SetAndNotify(ref _gifPath, value);
    }

    private bool _gifVisibility = true;

    public bool GifVisibility
    {
        get => _gifVisibility;
        set => SetAndNotify(ref _gifVisibility, value);
    }

    public void ChangeGif()
    {
        if (++_gifIndex >= _gitList.Length)
        {
            _gifIndex = 0;
        }

        GifPath = _gitList[_gifIndex];
    }

    private static bool _isDragging = false;
    private static Point _offset;

    // UI 绑定的方法
    [UsedImplicitly]
    public void DraggableElementMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (sender is not HandyControl.Controls.GifImage childElement)
        {
            return;
        }

        _isDragging = true;
        _offset = e.GetPosition(childElement);
        childElement.CaptureMouse();
    }

    // UI 绑定的方法
    [UsedImplicitly]
    public void DraggableElementMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (sender is not HandyControl.Controls.GifImage childElement)
        {
            return;
        }

        _isDragging = false;
        childElement.ReleaseMouseCapture();
    }

    // UI 绑定的方法
    [UsedImplicitly]
    public void DraggableElementMouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging || sender is not HandyControl.Controls.GifImage { Parent: Grid parentElement } childElement)
        {
            return;
        }

        Point currentPosition = e.GetPosition(parentElement);

        // 计算偏移量
        double newX = currentPosition.X - _offset.X;
        double newY = currentPosition.Y - _offset.Y;

        // 确保元素在父元素范围内
        newX = Math.Max(10, Math.Min(newX, parentElement.ActualWidth - childElement.ActualWidth - 10));
        newY = Math.Max(10, Math.Min(newY, parentElement.ActualHeight - childElement.ActualHeight - 10));

        childElement.HorizontalAlignment = HorizontalAlignment.Left;
        childElement.VerticalAlignment = VerticalAlignment.Top;
        childElement.Margin = new(newX, newY, 10, 10);
    }
}
