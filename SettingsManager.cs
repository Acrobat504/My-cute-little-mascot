using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace MascotApp;

public class PresetData
{
    public string Name { get; set; } = "";
    public string? Idle { get; set; }
    public string? Walk { get; set; }
    public string? Pet { get; set; }
    public string? Drag { get; set; }
}

public class AppSettings
{
    public double Speed { get; set; } = 3;
    public string? LastIdlePath { get; set; }
    public string? LastWalkPath { get; set; }
    public string? LastPetPath { get; set; }
    public string? LastDragPath { get; set; }

    // 하트 설정
    public double HeartSpread { get; set; } = 30;
    public double HeartSpeed { get; set; } = 80;
    public double HeartCooldown { get; set; } = 0.6;

    // 피벗 — 일반 방향 (캐릭터 창 기준 0.0~1.0 비율)
    public double PivotX { get; set; } = 0.5;   // 가로 중앙
    public double PivotY { get; set; } = 0.2;   // 위쪽 (머리)

    // 피벗 — 반전 방향 (좌우 반전 시 사용)
    public double PivotXMirrored { get; set; } = 0.5;
    public double PivotYMirrored { get; set; } = 0.2;

    public List<PresetData> Presets { get; set; } = new();
}

public static class SettingsManager
{
    private static readonly string SettingsDir =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MascotApp");

    private static readonly string SettingsPath =
        Path.Combine(SettingsDir, "settings.json");

    private static readonly string PresetsDir =
        Path.Combine(SettingsDir, "Presets");

    public static AppSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return new AppSettings();
            var json = File.ReadAllText(SettingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch { return new AppSettings(); }
    }

    public static void Save(AppSettings settings)
    {
        Directory.CreateDirectory(SettingsDir);
        var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(SettingsPath, json);
    }

    public static PresetData SavePreset(string name, string? idle, string? walk, string? pet, string? drag)
    {
        var presetDir = Path.Combine(PresetsDir, name);
        Directory.CreateDirectory(presetDir);

        string? CopyFile(string? src)
        {
            if (src == null || !File.Exists(src)) return null;
            var dest = Path.Combine(presetDir, Path.GetFileName(src));
            try { File.Copy(src, dest, overwrite: true); }
            catch (IOException) { return src; }
            return dest;
        }

        return new PresetData
        {
            Name = name,
            Idle = CopyFile(idle),
            Walk = CopyFile(walk),
            Pet = CopyFile(pet),
            Drag = CopyFile(drag)
        };
    }

    public static void DeletePreset(string name)
    {
        var presetDir = Path.Combine(PresetsDir, name);
        if (!Directory.Exists(presetDir)) return;

        foreach (var file in Directory.GetFiles(presetDir))
        {
            try { File.Delete(file); }
            catch (IOException) { }
        }

        try { Directory.Delete(presetDir, recursive: false); }
        catch (IOException) { }
    }
}