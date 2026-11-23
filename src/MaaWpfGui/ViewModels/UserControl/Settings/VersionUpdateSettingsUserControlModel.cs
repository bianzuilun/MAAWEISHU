// <copyright file="VersionUpdateSettingsUserControlModel.cs" company="MaaAssistantArknights">
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
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using MaaWpfGui.Configuration.Factory;
using MaaWpfGui.Constants.Enums;
using MaaWpfGui.Extensions;
using MaaWpfGui.Helper;
using MaaWpfGui.Properties;
using MaaWpfGui.Services;
using MaaWpfGui.Utilities.ValueType;
using MaaWpfGui.ViewModels.UI;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Stylet;

namespace MaaWpfGui.ViewModels.UserControl.Settings;

/// <summary>
/// 版本信息与网络代理设置。
/// </summary>
public class VersionUpdateSettingsUserControlModel : PropertyChangedBase
{
    static VersionUpdateSettingsUserControlModel()
    {
        Instance = new();
    }

    public static VersionUpdateSettingsUserControlModel Instance { get; }

    /// <summary>
    /// Gets the core version. UI 预览模式下 Core 未加载，不触碰 native，降级为占位值。
    /// </summary>
    private static readonly string _coreVersion = Main.Bootstrapper.IsCoreInitSkipped
        ? "N/A"
        : Marshal.PtrToStringAnsi(MaaService.AsstGetVersion()) ?? "0.0.1";

    public static string CoreVersion => _coreVersion;

    public static string CoreVersionDisplay => string.Join("\u200B", CoreVersion.ToCharArray());

    private static readonly string _uiVersion = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion.Split('+')[0] ?? "0.0.1";

    /// <summary>
    /// Gets the UI version.
    /// </summary>
    public static string UiVersion => _uiVersion == "0.0.1"
        ? "DEBUG_VERSION"
        : _uiVersion;

    public static string UiVersionDisplay => string.Join("\u200B", UiVersion.ToCharArray());

    public static DateTimeOffset BuildDateTime { get; } = Assembly.GetExecutingAssembly().GetCustomAttribute<BuildDateTimeAttribute>()?.BuildTime ?? DateTimeOffset.MinValue;

    public static string BuildDateTimeCurrentCultureString => BuildDateTime.ToLocalTimeString();

    private static (DateTimeOffset DateTime, string VersionName) _resourceInfo = GetResourceVersionByClientType(ConfigFactory.CurrentConfig.Gui.RuntimeSettings.ClientType);

    public (DateTimeOffset DateTime, string VersionName) ResourceInfo
    {
        get => _resourceInfo;
        set => SetAndNotify(ref _resourceInfo, value);
    }

    private static string _resourceVersion = _resourceInfo.VersionName;

    /// <summary>
    /// Gets or sets the resource version.
    /// </summary>
    public string ResourceVersion
    {
        get => _resourceVersion;
        set => SetAndNotify(ref _resourceVersion, value);
    }

    private static DateTimeOffset _resourceDateTime = _resourceInfo.DateTime;

    public DateTimeOffset ResourceDateTime
    {
        get => _resourceDateTime;
        set => SetAndNotify(ref _resourceDateTime, value);
    }

    private string _resourceDateTimeCurrentCultureString = _resourceDateTime.ToLocalTimeString();

    public string ResourceDateTimeCurrentCultureString
    {
        get => _resourceDateTimeCurrentCultureString;
        set => SetAndNotify(ref _resourceDateTimeCurrentCultureString, value);
    }

    public void ResourceInfoUpdate()
    {
        ResourceInfo = GetResourceVersionByClientType(SettingsViewModel.GameSettings.ClientType);
        ResourceVersion = ResourceInfo.VersionName;
        ResourceDateTime = ResourceInfo.DateTime;
        ResourceDateTimeCurrentCultureString = ResourceDateTime.ToLocalTimeString();
        Instances.SettingsViewModel.UpdateWindowTitle();
    }

    public static (DateTimeOffset DateTime, string VersionName) GetResourceVersionByClientType(ClientType clientType)
    {
        bool isDefaultClient = clientType is ClientType.Official or ClientType.Bilibili;

        string defaultJsonPath = Path.Combine(PathsHelper.ResourceDir, "version.json");
        var jsonPath = isDefaultClient
            ? defaultJsonPath
            : Path.Combine(PathsHelper.ResourceDir, $"global/{clientType.ToCustomString()}/resource/version.json");

        string versionName;
        if (!File.Exists(defaultJsonPath) || (!isDefaultClient && !File.Exists(jsonPath)))
        {
            return (DateTimeOffset.MinValue, string.Empty);
        }

        var versionJson = LoadJson(jsonPath);
        var currentTime = (ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var poolTime = (ulong?)versionJson?["gacha"]?["time"]; // 卡池的开始时间
        var activityTime = (ulong?)versionJson?["activity"]?["time"]; // 活动的开始时间
        var lastUpdated = isDefaultClient
            ? (string?)versionJson?["last_updated"]
            : (string?)LoadJson(defaultJsonPath)?["last_updated"];

        var dateTime = lastUpdated == null
            ? DateTimeOffset.MinValue
            : DateTimeOffset.ParseExact(lastUpdated, "yyyy-MM-dd HH:mm:ss.fff", null, System.Globalization.DateTimeStyles.AssumeUniversal);

        if (currentTime < poolTime && currentTime < activityTime)
        {
            versionName = string.Empty;
        }
        else if (currentTime >= poolTime && currentTime < activityTime)
        {
            versionName = versionJson?["gacha"]?["pool"]?.ToString() ?? string.Empty;
        }
        else if (currentTime < poolTime && currentTime >= activityTime)
        {
            versionName = versionJson?["activity"]?["name"]?.ToString() ?? string.Empty;
        }
        else if (poolTime > activityTime)
        {
            versionName = versionJson?["gacha"]?["pool"]?.ToString() ?? string.Empty;
        }
        else
        {
            versionName = versionJson?["activity"]?["name"]?.ToString() ?? string.Empty;
        }

        return (dateTime, versionName);

        static JObject? LoadJson(string path)
        {
            return JsonConvert.DeserializeObject<JObject>(File.ReadAllText(path));
        }
    }

    /// <summary>
    /// Gets or sets the proxy settings.
    /// </summary>
    public string Proxy
    {
        get; set {
            SetAndNotify(ref field, value);
            ConfigFactory.Root.Update.Proxy = value;
        }
    } = ConfigFactory.Root.Update.Proxy;

    public List<CombinedData> ProxyTypeList { get; } =
        [
            new() { Display = "HTTP Proxy", Value = "Http" },
            new() { Display = "SOCKS5 Proxy", Value = "Socks5" },
        ];

    public string ProxyType
    {
        get; set {
            SetAndNotify(ref field, value);
            ConfigFactory.Root.Update.ProxyType = value;
        }
    } = ConfigFactory.Root.Update.ProxyType;
}
