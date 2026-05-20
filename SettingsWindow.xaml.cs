using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MascotApp;

public partial class SettingsWindow : Window
{
    private readonly MainWindow _mainWindow;
    private readonly AppSettings _settings;

    public SettingsWindow(MainWindow mainWindow)
    {
        InitializeComponent();
        _mainWindow = mainWindow;
        _settings = SettingsManager.Load();

        // 이동 속도 복원
        SpeedSlider.Value = _settings.Speed;

        // 이미지 경로 복원
        IdlePath.Text = _settings.LastIdlePath ?? "";
        WalkPath.Text = _settings.LastWalkPath ?? "";
        PetPath.Text = _settings.LastPetPath ?? "";
        DragPath.Text = _settings.LastDragPath ?? "";

        // 하트 설정 복원
        HeartSpreadSlider.Value = _settings.HeartSpread;
        HeartSpeedSlider.Value = _settings.HeartSpeed;
        HeartCooldownSlider.Value = _settings.HeartCooldown * 10;

        // 피벗 마커 복원 (일반 + 반전 모두)
        UpdatePivotMarker(_settings.PivotX, _settings.PivotY);
        UpdatePivotMarkerMirrored(_settings.PivotXMirrored, _settings.PivotYMirrored);
        UpdatePivotPreview();

        RefreshPresetList();
    }

    // ── 이동 속도 ──────────────────────────────────

    private void OnSpeedChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (SpeedLabel == null) return;
        SpeedLabel.Text = ((int)SpeedSlider.Value).ToString();
    }

    // ── 이미지 불러오기 ────────────────────────────

    private void OnLoadIdle(object sender, RoutedEventArgs e)
        => PickAndApply(path => {
            IdlePath.Text = path;
            _settings.LastIdlePath = path;
            _mainWindow.ApplyCustomGifs(path, null, null, null);
            SettingsManager.Save(_settings);
            UpdatePivotPreview();
        });

    private void OnLoadWalk(object sender, RoutedEventArgs e)
        => PickAndApply(path => {
            WalkPath.Text = path;
            _settings.LastWalkPath = path;
            _mainWindow.ApplyCustomGifs(null, path, null, null);
            SettingsManager.Save(_settings);
        });

    private void OnLoadPet(object sender, RoutedEventArgs e)
        => PickAndApply(path => {
            PetPath.Text = path;
            _settings.LastPetPath = path;
            _mainWindow.ApplyCustomGifs(null, null, path, null);
            SettingsManager.Save(_settings);
        });

    private void OnLoadDrag(object sender, RoutedEventArgs e)
        => PickAndApply(path => {
            DragPath.Text = path;
            _settings.LastDragPath = path;
            _mainWindow.ApplyCustomGifs(null, null, null, path);
            SettingsManager.Save(_settings);
        });

    private void PickAndApply(Action<string> onSelected)
    {
        var dialog = new OpenFileDialog
        {
            Filter = "이미지 파일 (*.gif;*.png;*.apng;*.webp;*.jpg;*.jpeg;*.bmp)|*.gif;*.png;*.apng;*.webp;*.jpg;*.jpeg;*.bmp|모든 파일 (*.*)|*.*"
        };
        if (dialog.ShowDialog() == true)
            onSelected(dialog.FileName);
    }

    // ── 하트 설정 ──────────────────────────────────

    private void OnHeartSpreadChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (HeartSpreadLabel == null) return;
        HeartSpreadLabel.Text = ((int)HeartSpreadSlider.Value).ToString();
        _settings.HeartSpread = HeartSpreadSlider.Value;
        SettingsManager.Save(_settings);
    }

    private void OnHeartSpeedChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (HeartSpeedLabel == null) return;
        HeartSpeedLabel.Text = ((int)HeartSpeedSlider.Value).ToString();
        _settings.HeartSpeed = HeartSpeedSlider.Value;
        SettingsManager.Save(_settings);
    }

    private void OnHeartCooldownChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (HeartCooldownLabel == null) return;
        double cooldown = HeartCooldownSlider.Value / 10.0;
        HeartCooldownLabel.Text = cooldown.ToString("F1");
        _settings.HeartCooldown = cooldown;
        SettingsManager.Save(_settings);
    }

    // ── 피벗 ───────────────────────────────────────

    /// <summary>토글 변경 시 — 캔버스 자체는 그대로, 어느 마커를 움직일지만 바뀜</summary>
    private void OnPivotModeChanged(object sender, RoutedEventArgs e)
    {
        if (PivotMarker == null || PivotMarkerMirrored == null) return;

        bool isNormal = PivotNormalBtn.IsChecked == true;

        // 마커 투명도
        PivotMarker.Opacity = isNormal ? 1.0 : 0.35;
        PivotMarkerMirrored.Opacity = isNormal ? 0.35 : 1.0;

        // 미리보기 이미지 좌우 반전
        PivotPreviewImage.RenderTransformOrigin = new Point(0.5, 0.5);
        PivotPreviewImage.RenderTransform = new ScaleTransform(isNormal ? 1 : -1, 1);
    }

    /// <summary>캔버스 클릭 — 현재 선택된 모드의 피벗을 업데이트</summary>
    private void OnPivotCanvasClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        var pos = e.GetPosition(PivotCanvas);
        double pivotX = Math.Clamp(pos.X / PivotCanvas.Width, 0, 1);
        double pivotY = Math.Clamp(pos.Y / PivotCanvas.Height, 0, 1);

        if (PivotNormalBtn.IsChecked == true)
        {
            _settings.PivotX = pivotX;
            _settings.PivotY = pivotY;
            UpdatePivotMarker(pivotX, pivotY);
        }
        else
        {
            _settings.PivotXMirrored = pivotX;
            _settings.PivotYMirrored = pivotY;
            UpdatePivotMarkerMirrored(pivotX, pivotY);
        }

        SettingsManager.Save(_settings);
    }

    /// <summary>일반 방향 마커 (빨강) 위치 업데이트</summary>
    private void UpdatePivotMarker(double pivotX, double pivotY)
    {
        Canvas.SetLeft(PivotMarker, pivotX * PivotCanvas.Width - 6);
        Canvas.SetTop(PivotMarker, pivotY * PivotCanvas.Height - 6);
    }

    /// <summary>반전 방향 마커 (파랑) 위치 업데이트</summary>
    private void UpdatePivotMarkerMirrored(double pivotX, double pivotY)
    {
        Canvas.SetLeft(PivotMarkerMirrored, pivotX * PivotCanvas.Width - 6);
        Canvas.SetTop(PivotMarkerMirrored, pivotY * PivotCanvas.Height - 6);
    }

    private void UpdatePivotPreview()
    {
        try
        {
            var path = _settings.LastIdlePath;
            PivotPreviewImage.Source = (path != null && File.Exists(path))
                ? new BitmapImage(new Uri(path))
                : new BitmapImage(new Uri("pack://application:,,,/Assets/idle.gif"));
        }
        catch { }
    }

    // ── 프리셋 ─────────────────────────────────────

    private void OnSavePreset(object sender, RoutedEventArgs e)
    {
        var name = PresetNameBox.Text.Trim();
        if (string.IsNullOrEmpty(name))
        {
            MessageBox.Show("프리셋 이름을 입력해주세요.");
            return;
        }

        var existing = _settings.Presets.FirstOrDefault(p => p.Name == name);

        if (existing != null)
        {
            var result = MessageBox.Show(
                $"'{name}' 프리셋이 이미 있어요. 덮어쓸까요?",
                "프리셋 저장",
                MessageBoxButton.YesNo);

            if (result == MessageBoxResult.Yes)
            {
                SettingsManager.DeletePreset(name);
                _settings.Presets.Remove(existing);
            }
            else
            {
                name = GetUniqueName(name);
                PresetNameBox.Text = name;
            }
        }

        var preset = SettingsManager.SavePreset(
            name,
            string.IsNullOrEmpty(IdlePath.Text) ? null : IdlePath.Text,
            string.IsNullOrEmpty(WalkPath.Text) ? null : WalkPath.Text,
            string.IsNullOrEmpty(PetPath.Text) ? null : PetPath.Text,
            string.IsNullOrEmpty(DragPath.Text) ? null : DragPath.Text);

        _settings.Presets.Add(preset);
        SettingsManager.Save(_settings);
        RefreshPresetList();
    }

    private void OnDeletePreset(object sender, RoutedEventArgs e)
    {
        if (PresetList.SelectedItem is not string name) return;

        SettingsManager.DeletePreset(name);
        _settings.Presets.RemoveAll(p => p.Name == name);
        SettingsManager.Save(_settings);
        RefreshPresetList();
    }

    private void OnPresetSelected(object sender, SelectionChangedEventArgs e)
    {
        if (PresetList.SelectedItem is not string name) return;

        var preset = _settings.Presets.FirstOrDefault(p => p.Name == name);
        if (preset == null) return;

        IdlePath.Text = preset.Idle ?? "";
        WalkPath.Text = preset.Walk ?? "";
        PetPath.Text = preset.Pet ?? "";
        DragPath.Text = preset.Drag ?? "";

        _mainWindow.ApplyCustomGifs(preset.Idle, preset.Walk, preset.Pet, preset.Drag);

        _settings.LastIdlePath = preset.Idle;
        _settings.LastWalkPath = preset.Walk;
        _settings.LastPetPath = preset.Pet;
        _settings.LastDragPath = preset.Drag;
        SettingsManager.Save(_settings);

        UpdatePivotPreview();
    }

    private void RefreshPresetList()
    {
        PresetList.Items.Clear();
        foreach (var preset in _settings.Presets)
            PresetList.Items.Add(preset.Name);
    }

    private string GetUniqueName(string baseName)
    {
        var cleanBase = Regex.Replace(baseName, @"\s*\(\d+\)$", "").Trim();
        int counter = 1;
        string candidate;
        do { candidate = $"{cleanBase} ({counter++})"; }
        while (_settings.Presets.Any(p => p.Name == candidate));
        return candidate;
    }

    // ── 적용 ───────────────────────────────────────

    private void OnApply(object sender, RoutedEventArgs e)
    {
        _mainWindow.SpeedMultiplier = SpeedSlider.Value / 2.0;
        _settings.Speed = SpeedSlider.Value;
        SettingsManager.Save(_settings);
        Close();
    }
}