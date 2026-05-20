using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using WpfAnimatedGif;

namespace MascotApp;

public partial class MainWindow : Window
{
    private Point _dragOffset;
    private Point _mouseDownPos;

    private readonly Random _random = new();
    private TimeSpan _lastRenderTime;

    private CharacterState _state = CharacterState.Idle;
    private double _velocityX = 0;
    private double _velocityY = 0;
    private double _stateTimer = 0;

    // 현재 캐릭터가 좌측을 향하고 있는지 (좌우 반전 여부)
    private bool _isFacingLeft = false;

    private const double Gravity = 1.0;
    private const double PetMoveThreshold = 10;

    private DateTime _lastHeartTime = DateTime.MinValue;

    public double SpeedMultiplier = 1.0;

    private BitmapImage _idleGif = new(new Uri("pack://application:,,,/Assets/idle.gif"));
    private BitmapImage _walkGif = new(new Uri("pack://application:,,,/Assets/walk.gif"));
    private BitmapImage _petGif = new(new Uri("pack://application:,,,/Assets/pet.gif"));
    private BitmapImage _dragGif = new(new Uri("pack://application:,,,/Assets/drag.gif"));

    private readonly HeartOverlay _heartOverlay = new();

    public MainWindow()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        PositionOnTaskbar();

        CharacterImage.MouseLeftButtonDown += OnMouseDown;
        CharacterImage.MouseLeftButtonUp += OnDragEnd;
        CharacterImage.MouseMove += OnDragging;
        CharacterImage.MouseDown += OnMiddleMouseDown;
        CharacterImage.MouseUp += OnMiddleMouseUp;

        CompositionTarget.Rendering += OnTick;

        _heartOverlay.Show();

        // 설정 복원
        var settings = SettingsManager.Load();
        SpeedMultiplier = settings.Speed / 2.0;
        ApplyCustomGifs(
            settings.LastIdlePath,
            settings.LastWalkPath,
            settings.LastPetPath,
            settings.LastDragPath);

        EnterIdle();
    }

    // ── 상태 전환 ──────────────────────────────────

    private void EnterIdle()
    {
        _state = CharacterState.Idle;
        _velocityX = 0;
        _stateTimer = _random.NextDouble() * 3 + 1;
        SetGif(_idleGif);
    }

    private void EnterWalk()
    {
        _state = CharacterState.Walking;

        double speed = (_random.NextDouble() * 2 + 1) * SpeedMultiplier;
        _stateTimer = _random.NextDouble() * 3 + 1;

        if (Left <= 0)
            _velocityX = speed;
        else if (Left + Width >= SystemParameters.PrimaryScreenWidth)
            _velocityX = -speed;
        else
            _velocityX = _random.Next(2) == 0 ? speed : -speed;

        // 방향 추적 — 피벗 선택에 사용
        _isFacingLeft = _velocityX < 0;

        CharacterImage.RenderTransformOrigin = new Point(0.5, 0.5);
        CharacterImage.RenderTransform = new ScaleTransform(
            _velocityX > 0 ? 1 : -1, 1);

        SetGif(_walkGif);
    }

    private void EnterPet()
    {
        if (_state == CharacterState.Petting) return;
        _state = CharacterState.Petting;
        _velocityX = 0;
        SetGif(_petGif);
    }

    // ── 매 프레임 ──────────────────────────────────

    private void OnTick(object? sender, EventArgs e)
    {
        var renderArgs = (RenderingEventArgs)e;
        double dt = (renderArgs.RenderingTime - _lastRenderTime).TotalSeconds;
        _lastRenderTime = renderArgs.RenderingTime;

        if (dt <= 0 || dt > 0.1) return;

        if (_state == CharacterState.Falling)
        {
            _velocityY += Gravity * dt * 60;
            Top += _velocityY * dt * 60;

            double floorY = SystemParameters.WorkArea.Height - Height;
            if (Top >= floorY)
            {
                Top = floorY;
                _velocityY = 0;
                EnterIdle();
            }
            return;
        }

        if (_state == CharacterState.Dragging ||
            _state == CharacterState.Petting) return;

        _stateTimer -= dt;

        if (_state == CharacterState.Walking)
        {
            Left += _velocityX * dt * 60;

            if (Left < 0 || Left + Width >= SystemParameters.PrimaryScreenWidth)
            {
                Left = Math.Clamp(Left, 0, SystemParameters.PrimaryScreenWidth - Width);
                EnterIdle();
                return;
            }
        }

        if (_stateTimer <= 0)
        {
            if (_state == CharacterState.Idle)
                EnterWalk();
            else
                EnterIdle();
        }
    }

    // ── 마우스 이벤트 ──────────────────────────────

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        _mouseDownPos = e.GetPosition(this);
        CharacterImage.CaptureMouse();
    }

    private void OnDragging(object sender, MouseEventArgs e)
    {
        if (_state == CharacterState.Dragging)
        {
            var mouseScreen = PointToScreen(e.GetPosition(this));
            Left = mouseScreen.X - _dragOffset.X;
            Top = mouseScreen.Y - _dragOffset.Y;
            return;
        }

        if (e.LeftButton == MouseButtonState.Pressed)
        {
            double movedX = Math.Abs(e.GetPosition(this).X - _mouseDownPos.X);
            if (movedX >= PetMoveThreshold)
            {
                EnterPet();

                var settings = SettingsManager.Load();
                if ((DateTime.Now - _lastHeartTime).TotalSeconds > settings.HeartCooldown)
                {
                    ShowHeart();
                    _lastHeartTime = DateTime.Now;
                }
            }
        }
    }

    private void OnDragEnd(object sender, MouseButtonEventArgs e)
    {
        CharacterImage.ReleaseMouseCapture();

        if (_state == CharacterState.Petting)
        {
            EnterIdle();
            return;
        }
    }

    private void OnMiddleMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Middle) return;

        _dragOffset = e.GetPosition(this);
        _state = CharacterState.Dragging;
        _velocityX = 0;
        SetGif(_dragGif);
        CharacterImage.CaptureMouse();
    }

    private void OnMiddleMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton != MouseButton.Middle) return;
        if (_state != CharacterState.Dragging) return;

        CharacterImage.ReleaseMouseCapture();
        _velocityY = 0;
        _state = CharacterState.Falling;
    }

    // ── 하트 ──────────────────────────────────────

    private void ShowHeart()
    {
        var settings = SettingsManager.Load();

        // 현재 방향에 맞는 피벗 선택
        double pivotX = _isFacingLeft ? settings.PivotXMirrored : settings.PivotX;
        double pivotY = _isFacingLeft ? settings.PivotYMirrored : settings.PivotY;

        double pivotScreenX = Left + Width * pivotX;
        double pivotScreenY = Top + Height * pivotY;

        double spread = settings.HeartSpread;
        double spreadDir = _random.NextDouble() * 2 - 1;
        double startX = pivotScreenX + (_random.NextDouble() * spread - spread / 2);
        double startY = pivotScreenY;

        _heartOverlay.SpawnHeart(startX, startY, spreadDir, settings.HeartSpeed);
    }

    // ── 우클릭 메뉴 ────────────────────────────────

    private void OnContextSettings(object sender, RoutedEventArgs e)
    {
        var settings = new SettingsWindow(this);
        settings.Show();
    }

    private void OnContextExit(object sender, RoutedEventArgs e)
    {
        Application.Current.Shutdown();
    }

    // ── 유틸 ───────────────────────────────────────

    private void SetGif(BitmapImage gif)
    {
        ImageBehavior.SetAnimatedSource(CharacterImage, gif);
    }

    private void PositionOnTaskbar()
    {
        Left = 100;
        Top = SystemParameters.WorkArea.Height - Height;
    }

    public void ToggleVisibility()
    {
        if (IsVisible) Hide(); else Show();
    }

    public void ApplyCustomGifs(string? idle, string? walk, string? pet, string? drag)
    {
        BitmapImage Load(string path) => new(new Uri(path));

        if (idle != null && File.Exists(idle))
        { _idleGif = Load(idle); if (_state == CharacterState.Idle) SetGif(_idleGif); }
        if (walk != null && File.Exists(walk))
        { _walkGif = Load(walk); if (_state == CharacterState.Walking) SetGif(_walkGif); }
        if (pet != null && File.Exists(pet))
        { _petGif = Load(pet); if (_state == CharacterState.Petting) SetGif(_petGif); }
        if (drag != null && File.Exists(drag))
        { _dragGif = Load(drag); if (_state == CharacterState.Dragging) SetGif(_dragGif); }
    }
}