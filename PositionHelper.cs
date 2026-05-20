using System;
using System.Runtime.InteropServices;

namespace MascotApp;

public static class PositionHelper
{
    [DllImport("user32.dll")]
    private static extern bool SystemParametersInfo(
        uint uiAction, uint uiParam, ref RECT pvParam, uint fWinIni);

    [DllImport("shell32.dll")]
    private static extern IntPtr SHAppBarMessage(uint dwMessage, ref APPBARDATA pData);

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT { public int Left, Top, Right, Bottom; }

    [StructLayout(LayoutKind.Sequential)]
    private struct APPBARDATA
    {
        public uint cbSize, hWnd, uCallbackMessage, uEdge;
        public RECT rc;
        public IntPtr lParam;
    }

    public enum TaskbarEdge { Bottom, Top, Left, Right, Unknown }

    /// <summary>작업 표시줄이 화면 어느 쪽에 붙어있는지 반환</summary>
    public static TaskbarEdge GetTaskbarEdge()
    {
        var data = new APPBARDATA { cbSize = (uint)Marshal.SizeOf<APPBARDATA>() };
        SHAppBarMessage(5 /* ABM_GETTASKBARPOS */, ref data);

        return data.uEdge switch
        {
            0 => TaskbarEdge.Left,
            1 => TaskbarEdge.Top,
            2 => TaskbarEdge.Right,
            3 => TaskbarEdge.Bottom,
            _ => TaskbarEdge.Unknown
        };
    }

    /// <summary>캐릭터가 서있을 Y 좌표 반환 (작업 표시줄 바로 위)</summary>
    public static double GetFloorY(double characterHeight)
    {
        return System.Windows.SystemParameters.WorkArea.Height - characterHeight;
    }
}