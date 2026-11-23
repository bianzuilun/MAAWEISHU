// <copyright file="VersionHelper.cs" company="MaaAssistantArknights">
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

using System.Linq;
using System.Text.RegularExpressions;
using MaaWpfGui.ViewModels.UserControl.Settings;
using Semver;

namespace MaaWpfGui.Helper;

/// <summary>
/// Helpers for classifying MAA version strings.
/// </summary>
public static class VersionHelper
{
    /// <summary>
    /// Determines whether the specified version is a debug / local-dev build.
    /// </summary>
    /// <param name="version">Version string. Defaults to the current Core version.</param>
    /// <returns><see langword="true"/> if the version is a debug build.</returns>
    public static bool IsDebugVersion(string? version = null)
    {
        version ??= VersionUpdateSettingsUserControlModel.CoreVersion;

        // match case 1: DEBUG_VERSION
        // match case 2: v{Major}.{Minor}.{Patch}-{CommitDistance}-g{CommitHash}
        // match case 3: {CommitHash}
        return Regex.IsMatch(version, @"^(.*DEBUG.*|v\d+(\.\d+){1,3}-\d+-g[0-9a-f]{6,}|[^v][0-9a-f]{6,})$");
    }

    /// <summary>
    /// Determines whether the specified version is a stable release.
    /// </summary>
    /// <param name="version">Version string. Defaults to the current Core version.</param>
    /// <returns><see langword="true"/> if the version is a stable release.</returns>
    public static bool IsStdVersion(string? version = null)
    {
        // 正式版：vX.X.X
        // DevBuild (CI)：yyyy-MM-dd-HH-mm-ss-{CommitHash[..7]}
        // DevBuild (Local)：yyyy-MM-dd-HH-mm-ss-{CommitHash[..7]}-Local
        // Release (Local Commit)：v.{CommitHash[..7]}-Local
        // Release (Local Tag)：{Tag}-Local
        // Debug (Local)：DEBUG_VERSION
        // Script Compiled：c{CommitHash[..7]}
        version ??= VersionUpdateSettingsUserControlModel.CoreVersion;

        if (IsDebugVersion(version))
        {
            return false;
        }

        if (version.StartsWith('c') || version.StartsWith("20") || version.Contains("Local"))
        {
            return false;
        }

        if (!SemVersion.TryParse(version, SemVersionStyles.AllowLowerV, out var semVersion))
        {
            return false;
        }

        return !semVersion.IsPrerelease;
    }

    /// <summary>
    /// Determines whether the specified version is a beta / prerelease (excluding nightly).
    /// </summary>
    /// <param name="version">Version string. Defaults to the current Core version.</param>
    /// <returns><see langword="true"/> if the version is a beta prerelease.</returns>
    public static bool IsBetaVersion(string? version = null)
    {
        version ??= VersionUpdateSettingsUserControlModel.CoreVersion;

        if (IsDebugVersion(version))
        {
            return false;
        }

        if (version.StartsWith('c') || version.StartsWith("20") || version.Contains("Local"))
        {
            return false;
        }

        if (!SemVersion.TryParse(version, SemVersionStyles.AllowLowerV, out var semVersion))
        {
            return false;
        }

        return semVersion.IsPrerelease && !IsNightlyVersion(semVersion);
    }

    /// <summary>
    /// Determines whether the specified semantic version is a nightly prerelease.
    /// </summary>
    /// <param name="version">Parsed semantic version.</param>
    /// <returns><see langword="true"/> if the version is nightly.</returns>
    public static bool IsNightlyVersion(SemVersion version)
    {
        if (!version.IsPrerelease)
        {
            return false;
        }

        // v{Major}.{Minor}.{Patch}-{Prerelease}.{CommitDistance}.g{CommitHash}
        // v4.6.7-beta.2.1.g1234567
        // v4.6.8-5.g1234567
        var lastId = version.PrereleaseIdentifiers.LastOrDefault().ToString();
        return lastId.StartsWith('g') && lastId.Length >= 7;
    }
}
