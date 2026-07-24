using Microsoft.Win32;
using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Windows.Forms;

namespace StarConfig.Installer;

internal static class Program
{
    private const string AppName = "Starbind";
    private const string AppVersion = "0.7.1";
    private static readonly string InstallDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", AppName);
    private static readonly string LegacyDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Programs", "StarConfig");

    [STAThread]
    private static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        try
        {
            if (args.Any(x => x.Equals("/uninstall", StringComparison.OrdinalIgnoreCase))) { Uninstall(); return; }
            Install();
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, $"{AppName} Setup", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private static void Install()
    {
        var answer = MessageBox.Show($"Install {AppName} {AppVersion} for this Windows user?\n\nInstall location:\n{InstallDir}", $"{AppName} Setup", MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
        if (answer != DialogResult.OK) return;
        RemoveLegacyInstall();
        Directory.CreateDirectory(InstallDir);
        ExtractPayload(InstallDir);
        var currentInstaller = Environment.ProcessPath ?? throw new InvalidOperationException("Could not locate the installer executable.");
        var installedInstaller = Path.Combine(InstallDir, "Starbind-Setup.exe");
        File.Copy(currentInstaller, installedInstaller, true);
        var appExe = Path.Combine(InstallDir, "Starbind.exe");
        if (!File.Exists(appExe)) throw new FileNotFoundException("The installer payload did not contain Starbind.exe.", appExe);
        CreateShortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs", "Starbind.lnk"), appExe, InstallDir);
        CreateShortcut(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Starbind.lnk"), appExe, InstallDir);
        RegisterUninstaller(installedInstaller, appExe);
        var runNow = MessageBox.Show($"Starbind {AppVersion} was installed successfully.\n\nLaunch it now?", $"{AppName} Setup", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
        if (runNow == DialogResult.Yes) Process.Start(new ProcessStartInfo(appExe) { UseShellExecute = true, WorkingDirectory = InstallDir });
    }

    private static void ExtractPayload(string destination)
    {
        var resourceName = Assembly.GetExecutingAssembly().GetManifestResourceNames().FirstOrDefault(x => x.EndsWith("Payload.zip", StringComparison.OrdinalIgnoreCase));
        if (resourceName is null) throw new InvalidOperationException("The installer payload is missing.");
        using var payload = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName) ?? throw new InvalidOperationException("The installer payload could not be opened.");
        using var archive = new ZipArchive(payload, ZipArchiveMode.Read);
        foreach (var entry in archive.Entries)
        {
            var target = Path.GetFullPath(Path.Combine(destination, entry.FullName));
            var root = Path.GetFullPath(destination) + Path.DirectorySeparatorChar;
            if (!target.StartsWith(root, StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("The installer payload contains an invalid path.");
            if (string.IsNullOrEmpty(entry.Name)) { Directory.CreateDirectory(target); continue; }
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            entry.ExtractToFile(target, true);
        }
    }

    private static void RegisterUninstaller(string installerPath, string appExe)
    {
        using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\Starbind");
        key.SetValue("DisplayName", AppName);
        key.SetValue("DisplayVersion", AppVersion);
        key.SetValue("Publisher", "crazytaxzi");
        key.SetValue("InstallLocation", InstallDir);
        key.SetValue("DisplayIcon", appExe);
        key.SetValue("UninstallString", $"\"{installerPath}\" /uninstall");
        key.SetValue("NoModify", 1, RegistryValueKind.DWord);
        key.SetValue("NoRepair", 1, RegistryValueKind.DWord);
    }

    private static void RemoveLegacyInstall()
    {
        TryDelete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs", "StarConfig.lnk"));
        TryDelete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "StarConfig.lnk"));
        Registry.CurrentUser.DeleteSubKeyTree(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\StarConfig", false);
        try { if (Directory.Exists(LegacyDir)) Directory.Delete(LegacyDir, true); } catch { }
    }

    private static void Uninstall()
    {
        if (MessageBox.Show("Remove Starbind from this Windows account?", $"Uninstall {AppName}", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        TryDelete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu), "Programs", "Starbind.lnk"));
        TryDelete(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "Starbind.lnk"));
        Registry.CurrentUser.DeleteSubKeyTree(@"Software\Microsoft\Windows\CurrentVersion\Uninstall\Starbind", false);
        var cleanupScript = Path.Combine(Path.GetTempPath(), $"Starbind-Uninstall-{Guid.NewGuid():N}.cmd");
        File.WriteAllText(cleanupScript, "@echo off\r\ntimeout /t 2 /nobreak >nul\r\n" + $"rmdir /s /q \"{InstallDir}\"\r\n" + "del /q \"%~f0\"\r\n");
        Process.Start(new ProcessStartInfo("cmd.exe", $"/c \"{cleanupScript}\"") { CreateNoWindow = true, UseShellExecute = false });
    }

    private static void CreateShortcut(string shortcutPath, string targetPath, string workingDirectory)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(shortcutPath)!);
        var shellType = Type.GetTypeFromProgID("WScript.Shell") ?? throw new InvalidOperationException("Windows Script Host is unavailable, so shortcuts could not be created.");
        dynamic shell = Activator.CreateInstance(shellType)!;
        dynamic shortcut = shell.CreateShortcut(shortcutPath);
        shortcut.TargetPath = targetPath;
        shortcut.WorkingDirectory = workingDirectory;
        shortcut.Description = "Star Citizen control mapper";
        shortcut.Save();
    }

    private static void TryDelete(string path) { try { if (File.Exists(path)) File.Delete(path); } catch { } }
}
