using System.Runtime.InteropServices;
using Windows.Foundation;


//Code taken from https://stackoverflow.com/questions/1316681/getting-mouse-position-in-c-sharp#:~:text=a%20new%20Point%3A-,Cursor.,new%20Point(x%2C%20y)%3B
//god bless this person
//Someday Ill understand how this works but for now I shan't bother

public class MouseTracker
{
/// Struct representing a point.
[StructLayout(LayoutKind.Sequential)]
public struct POINT
{
    public int X;
    public int Y;

    public static implicit operator Point(POINT point)
    {
        return new Point(point.X, point.Y);
    }
}

/// Retrieves the cursor's position, in screen coordinates.
/// <see>See MSDN documentation for further information.</see>
[DllImport("user32.dll")]
public static extern bool GetCursorPos(out POINT lpPoint);

public Point GetCursorPosition()
{
    POINT lpPoint;
    GetCursorPos(out lpPoint);
    return lpPoint;
}

}
