using System;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class OpenCLI
{
    const string k_LaunchAttemptedKey = "OpenCLI.LaunchAttempted";
    const string k_ProcessIdKey = "OpenCLI.ProcessId";
    const string k_ProcessStartTicksKey = "OpenCLI.ProcessStartTicks";

    static OpenCLI()
    {
        EditorApplication.quitting -= CloseCliWindow;
        EditorApplication.quitting += CloseCliWindow;

        if (Application.isBatchMode || SessionState.GetBool(k_LaunchAttemptedKey, false))
            return;

        SessionState.SetBool(k_LaunchAttemptedKey, true);
        EditorApplication.delayCall += OpenPowerShellCLI;
    }

    static void OpenPowerShellCLI()
    {
        EditorApplication.delayCall -= OpenPowerShellCLI;

        var projectPath = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        var cliPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Unity",
            "bin",
            "unity.exe");

        if (!File.Exists(cliPath))
        {
            UnityEngine.Debug.LogError($"Unity CLI를 찾을 수 없습니다: {cliPath}");
            return;
        }

        var powerShellPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.System),
            "WindowsPowerShell",
            "v1.0",
            "powershell.exe");
        var projectName = new DirectoryInfo(projectPath).Name;
        var command =
            $"$Host.UI.RawUI.WindowTitle = 'Unity CLI - {EscapePowerShell(projectName)}'; " +
            $"Set-Location -LiteralPath '{EscapePowerShell(projectPath)}'; " +
            $"& '{EscapePowerShell(cliPath)}' shell";

        var startInfo = new ProcessStartInfo
        {
            FileName = powerShellPath,
            Arguments = $"-NoLogo -NoExit -Command \"{command}\"",
            WorkingDirectory = projectPath,
            UseShellExecute = true,
            CreateNoWindow = false,
            WindowStyle = ProcessWindowStyle.Normal
        };

        try
        {
            var process = Process.Start(startInfo);
            if (process == null)
                throw new InvalidOperationException("PowerShell 프로세스를 시작하지 못했습니다.");

            SessionState.SetInt(k_ProcessIdKey, process.Id);
            SessionState.SetString(
                k_ProcessStartTicksKey,
                process.StartTime.ToUniversalTime().Ticks.ToString());
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError("Unity CLI PowerShell 실행 실패: " + e.Message);
        }
    }

    static void CloseCliWindow()
    {
        EditorApplication.delayCall -= OpenPowerShellCLI;

        if (!TryGetTrackedProcess(out var process))
        {
            ClearProcessTracking();
            return;
        }

        try
        {
            process.CloseMainWindow();
            if (!process.WaitForExit(1000))
                KillProcessTree(process.Id);
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogWarning("Unity CLI PowerShell 종료 실패: " + e.Message);
        }
        finally
        {
            process.Dispose();
            ClearProcessTracking();
        }
    }

    static bool TryGetTrackedProcess(out Process process)
    {
        process = null;
        var processId = SessionState.GetInt(k_ProcessIdKey, 0);
        var startTicksText = SessionState.GetString(k_ProcessStartTicksKey, string.Empty);

        if (processId == 0 || !long.TryParse(startTicksText, out var startTicks))
            return false;

        try
        {
            var candidate = Process.GetProcessById(processId);
            if (candidate.HasExited ||
                !string.Equals(candidate.ProcessName, "powershell", StringComparison.OrdinalIgnoreCase) ||
                candidate.StartTime.ToUniversalTime().Ticks != startTicks)
            {
                candidate.Dispose();
                return false;
            }

            process = candidate;
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }

    static void KillProcessTree(int processId)
    {
        var taskKillPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.System),
            "taskkill.exe");
        var startInfo = new ProcessStartInfo
        {
            FileName = taskKillPath,
            Arguments = $"/PID {processId} /T /F",
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var taskKill = Process.Start(startInfo);
        taskKill?.WaitForExit(3000);
    }

    static void ClearProcessTracking()
    {
        SessionState.SetInt(k_ProcessIdKey, 0);
        SessionState.SetString(k_ProcessStartTicksKey, string.Empty);
    }

    static string EscapePowerShell(string value)
    {
        return value.Replace("'", "''");
    }
}
