// <copyright file="Update.cs" company="MaaAssistantArknights">
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
using MaaWpfGui.Models;

namespace MaaWpfGui.Configuration.Global;

/// <summary>
/// 网络代理设置（全局）。JSON 节点名保持 ｢Update｣ 以兼容既有配置。
/// </summary>
public partial class Update : NotifyPropertyChangedWithValue
{
    public string Proxy { get; set; } = string.Empty;

    public string ProxyType { get; set; } = "Http";
}
