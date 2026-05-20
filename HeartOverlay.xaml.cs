using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace MascotApp;

public partial class HeartOverlay : Window
{
    private class HeartParticle
    {
        public Image Image;
        public double X, Y;
        public double VelocityX, VelocityY;
        public double StartTime;
        public double Duration;

        public HeartParticle(Image img, double x, double y, double vx, double vy, double duration, double now)
        {
            Image = img;
            X = x; Y = y;
            VelocityX = vx; VelocityY = vy;
            Duration = duration;
            StartTime = now;
        }
    }

    private readonly List<HeartParticle> _particles = new();
    private readonly DispatcherTimer _timer = new();
    private readonly BitmapImage _heartBitmap;
    private double _elapsed = 0;

    public HeartOverlay()
    {
        InitializeComponent();

        // 전체 화면 크기로
        Left = 0;
        Top = 0;
        Width = SystemParameters.PrimaryScreenWidth;
        Height = SystemParameters.PrimaryScreenHeight;

        _heartBitmap = new BitmapImage(new Uri("pack://application:,,,/Assets/heart.png"));

        _timer.Interval = TimeSpan.FromMilliseconds(16);
        _timer.Tick += OnTick;
        _timer.Start();
    }

    public void SpawnHeart(double screenX, double screenY, double spreadDir, double speed = 80)
    {
        var random = new Random();
        double vy = -speed;
        double vx = spreadDir * (random.NextDouble() * 30 + 10);

        var img = new Image
        {
            Source = _heartBitmap,
            Width = 36,
            Height = 36,
            Opacity = 1.0
        };

        Canvas.SetLeft(img, screenX - 18);
        Canvas.SetTop(img, screenY - 18);
        HeartCanvas.Children.Add(img);

        _particles.Add(new HeartParticle(
            img, screenX, screenY, vx, vy,
            duration: random.NextDouble() * 0.4 + 0.8,
            now: _elapsed));
    }

    private void OnTick(object? sender, EventArgs e)
    {
        _elapsed += 0.016;

        for (int i = _particles.Count - 1; i >= 0; i--)
        {
            var p = _particles[i];
            double t = _elapsed - p.StartTime;

            if (t >= p.Duration)
            {
                HeartCanvas.Children.Remove(p.Image);
                _particles.RemoveAt(i);
                continue;
            }

            double progress = t / p.Duration;

            // 위로 올라가면서 중력 적용
            p.X += p.VelocityX * 0.016;
            p.Y += p.VelocityY * 0.016;
            p.VelocityY += 30 * 0.016; // 중력으로 서서히 감속

            Canvas.SetLeft(p.Image, p.X - 18);
            Canvas.SetTop(p.Image, p.Y - 18);

            // 후반 40%에서 페이드
            p.Image.Opacity = progress < 0.6 ? 1.0 : 1.0 - (progress - 0.6) / 0.4;
        }
    }
}