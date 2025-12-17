namespace Youtube_Downloader;

/// <summary>
/// PictureBox that handles disposed image errors gracefully during paint.
/// This prevents JIT crashes when an image is disposed while the control is repainting.
/// </summary>
public class SafePictureBox : PictureBox
{
    protected override void OnPaint(PaintEventArgs pe)
    {
        try
        {
            base.OnPaint(pe);
        }
        catch (ArgumentException)
        {
            // Image was disposed - clear it and continue
            Image = null;
        }
    }
}
