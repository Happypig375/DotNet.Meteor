using DotNet.Debugging.Common.Android;
using DotNet.Meteor.Workspace.Extensions;

namespace DotNet.Meteor.Workspace.Devices;

public static class AndroidDeviceTool {
    public static List<DeviceData> VirtualDevices() {
        var runningAvds = new Dictionary<string, string>();
        var avds = new List<DeviceData>();
        var avdHome = GetEmulatorsDirectory();

        foreach (var serial in AndroidEmulator.GetDevices()) {
            if (!serial.StartsWith("emulator-"))
                continue;
            runningAvds.Add(AndroidEmulator.GetEmulatorName(serial), serial);
        }

        if (Directory.Exists(avdHome)) {
            foreach (var file in Directory.GetFiles(avdHome, "*.ini")) {
                var ini = new IniFile(file);
                var name = Path.GetFileNameWithoutExtension(file);
                avds.Add(new DeviceData {
                    Name = name,
                    Serial = runningAvds.TryGetValue(name, out string? value) ? value : string.Empty,
                    Category = Categories.AndroidEmulator,
                    Platform = Platforms.Android,
                    OSVersion = ini.GetField("target") ?? "Unknown",
                    IsRunning = runningAvds.ContainsKey(name),
                    IsEmulator = true,
                    IsMobile = true
                });
                runningAvds.Remove(name);
                ini.Free();
            }
        }

        // Add all running AVDs that aren't in the AVD folder
        foreach (var avd in runningAvds) {
            avds.Add(new DeviceData {
                Name = avd.Key,
                Serial = avd.Value,
                Category = Categories.AndroidEmulator,
                Platform = Platforms.Android,
                OSVersion = $"android-{AndroidDebugBridge.Shell(avd.Value, "getprop", "ro.build.version.sdk")}",
                IsRunning = true,
                IsEmulator = true,
                IsMobile = true
            });
        }

        return avds;
    }
    public static List<DeviceData> PhysicalDevices() {
        var runningDevices = AndroidEmulator.GetDevices();
        var devices = new Dictionary<string, DeviceData>();

        foreach (var serial in runningDevices) {
            if (serial.StartsWith("emulator-"))
                continue;

            string hardwareId = AndroidDebugBridge.Shell(serial, "getprop", "ro.serialno").Trim();
            // WiFi debugging exposes the same phone under several serials, so
            // identify it by hardware id; fall back to the serial itself.
            if (string.IsNullOrEmpty(hardwareId))
                hardwareId = serial;

            devices.TryAdd(hardwareId, new DeviceData {
                Name = AndroidDebugBridge.Shell(serial, "getprop", "ro.product.model"),
                OSVersion = $"android-{AndroidDebugBridge.Shell(serial, "getprop", "ro.build.version.sdk")}",
                Platform = Platforms.Android,
                Category = Categories.AndroidDevice,
                IsEmulator = false,
                IsRunning = true,
                IsMobile = true,
                Serial = serial
            });
        }

        return devices.Values.ToList();
    }

    private static string GetEmulatorsDirectory() {
        var path = Environment.GetEnvironmentVariable("ANDROID_AVD_HOME");
        if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
            return path;

        return Path.Combine(RuntimeInfo.HomeDirectory, ".android", "avd");
    }
}