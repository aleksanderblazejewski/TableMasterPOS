using System.Windows.Forms;

public class DoubleBufferedPanel : Panel
{
    public DoubleBufferedPanel()
    {
        this.DoubleBuffered = true;
        this.ResizeRedraw = true;
    }
}