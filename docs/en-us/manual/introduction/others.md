---
order: 13
icon: icon-park-solid:other
---

# Others

::: info UI-Only Feature
Most features on this page are implemented by the UI layer. See [Getting Started](../newbie.md) for details.
:::

## GPU-Accelerated Inference

Uses DirectML to call the GPU for recognition inference acceleration<sup>[PR](https://github.com/MaaAssistantArknights/MaaAssistantArknights/pull/9236)</sup>, reducing CPU usage significantly with minimal GPU usage. Recommended to enable.

Testing has shown that some graphics cards may experience recognition issues when using this feature due to missing functionality or lower performance. MAA has built-in a blacklist for certain GPUs<sup>[PR1](https://github.com/MaaAssistantArknights/MaaAssistantArknights/pull/9990)[PR2](https://github.com/MaaAssistantArknights/MaaAssistantArknights/pull/12134)</sup>. If you encounter recognition issues with GPUs not on the blacklist after enabling this feature, please submit an Issue.

## One-Time Only

Configuration changes in the main interface and settings are usually saved automatically, but the following will reset after MAA restarts:

- Options marked with an `*`
- Options marked with `(One-time only)`
- Half-selected checkboxes obtained by right-clicking

Many checkboxes support right-clicking for a half-selected state (one-time only, resets after MAA restart). Common use cases include:

- **Main task list**: Right-click a task to half-select it — it will run this time but revert after restart. By default, right-click means "apply once"; this can be switched to "skip once" (i.e., the half-selected task is skipped this time) in `Settings` - `GUI` - `Reverse right-click effect for main task`.
- **Combat settings**: `Use Sanity Potion`, `Use Originium` and similar options become one-time only when half-selected via right-click.

## Auto-Switch Configuration at Startup

MAA supports automatically switching configurations via launch parameters by adding `--config <configuration name>` after the MAA process name. Example: `./MAA/MAA.exe --config Official`.

Some characters need to be escaped according to JSON conventions. Example: when the configuration name is `"Official"`, the parameter should be `--config \"Official\"`.

## Start/End Scripts

Since v4.13.0, MAA supports setting scripts to run before starting and after finishing tasks. Enter the absolute or relative path to a batch file (`.bat`) in the input field.

## Configuration Migration

In the Windows version, all MAA configurations are stored in the `config` folder. Migrating this folder transfers all MAA settings.

## Scheduled Execution

In `Settings` - `Scheduled Execution`, you can set up to 8 timed tasks. Each includes an enable toggle, time (hour:minute), and a corresponding configuration.

- When the scheduled time is reached, MAA will automatically connect to the emulator and start executing the tasks checked in the current configuration.
- Each timer's checkbox supports right-click half-select (one-time only), effective only for the current session and resetting after MAA restart.

### Force Scheduled Start

When checked, MAA will **stop the currently running task**, restart the game, and start a new task. Useful for handling the daily 04:00 (server local time) flash update — after the flash update, it forcibly restarts the game and re-executes tasks to prevent them from getting stuck.

After a disconnection or flash update, MAA stops the tasks instead of reconnecting automatically. For overnight runs, also enable the `Start Up` task and schedule the timer after 4:10 AM each day (the flash update window is usually 4:00-4:10 AM).

### Custom Configuration Selection

When checked, each timer item can specify a different configuration. MAA will restart and switch to the corresponding configuration **two minutes before** the scheduled trigger, then start executing tasks.

## Update

MAA updates come in two types of content:

- **Software version**: Updates to the main program's UI and features.
- **Resource version**: Material data updates, including new stage maps, drop item icons, recruitment operator tags, etc. Software releases typically include the latest resources at the time of release.

::: tip Navigation Hot Update
There is also a "navigation hot update" that triggers automatically to update the main interface prompts and event stage entries. A notification appears in the top-right corner upon success.
:::

### Release Types

GitHub Releases ships three kinds of builds:

- **Stable**: Fewer bugs, slower support for new features and themes. New features may take weeks to be supported.
- **Beta**: May have minor bugs, faster support for new features and themes.
- **Nightly**: Contains unstable features, for testing only. Do not discuss nightly issues elsewhere.

::: tip
If the interface prompts that your version is too low and the minimum required version is beta, the feature is currently only available in the beta build. Wait for the stable release or install the beta full package.
:::

### Getting a New Version

MAA no longer provides in-app automatic updates. Download the full package from [GitHub Releases](https://github.com/MaaAssistantArknights/MaaAssistantArknights/releases) (`MAA-<version>-win-<arch>.zip`, e.g. `MAA-v5.20.0-win-x64.zip`), extract it into a **new** folder, then copy the `config`, `data`, and `debug` folders from the old directory to keep your settings and logs (`cache` and `achievement` are optional). ⚠️ Do not overwrite the old folder directly.

Resource files ship with the full package. You can also download [MaaResource](https://github.com/MaaAssistantArknights/MaaResource/archive/refs/heads/main.zip) and overwrite the `resource` folder (incremental content — do not delete the original folder).

## Additional Notes

- Tasks on the left side of the main page can be dragged to change their order, as can facilities in the base management settings.
- All click operations target random positions within buttons, following a normal distribution (higher probability at the center, decreasing with distance from center).
- The core algorithms are developed in C++ with multi-level caching to minimize CPU and memory usage.
- We recommend non-critical users try the beta version, which typically updates faster and has fewer bugs. (What MIUI? (╯‵□′)╯︵┻━┻)
