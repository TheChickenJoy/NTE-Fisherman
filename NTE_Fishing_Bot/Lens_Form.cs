using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace NTE_Fishing_Bot;

public class Lens_Form : Form
{
	private readonly Timer timer;

	private Bitmap scrBmp;

	private Graphics scrGrp;

	private bool mouseDown;

	public int ZoomFactor { get; set; } = 2;

	public bool HideCursor { get; set; } = true;

	public bool AutoClose { get; set; } = true;

	public bool NearestNeighborInterpolation { get; set; }

	public Lens_Form()
	{
		SetStyle(ControlStyles.UserPaint | ControlStyles.Opaque | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, value: true);
		UpdateStyles();
		base.FormBorderStyle = FormBorderStyle.None;
		base.ShowInTaskbar = false;
		base.TopMost = true;
		base.Width = 150;
		base.Height = 150;
		timer = new Timer
		{
			Interval = 55,
			Enabled = true
		};
		timer.Tick += delegate
		{
			Invalidate();
		};
	}

	protected override void OnShown(EventArgs e)
	{
		base.OnShown(e);
		GraphicsPath gp = new GraphicsPath();
		gp.AddEllipse(0, 0, base.Width, base.Height);
		base.Region = new Region(gp);
		CopyScreen();
		SetLocation();
		base.Capture = true;
		mouseDown = true;
		Cursor = Cursors.Cross;
		if (HideCursor)
		{
			Cursor.Hide();
		}
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		base.OnMouseDown(e);
		if (e.Button == MouseButtons.Left)
		{
			mouseDown = true;
			Cursor = Cursors.Default;
			if (HideCursor)
			{
				Cursor.Hide();
			}
		}
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		base.OnMouseMove(e);
		Invalidate();
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		base.OnMouseUp(e);
		mouseDown = false;
		if (HideCursor)
		{
			Cursor.Show();
		}
		if (AutoClose)
		{
			Dispose();
		}
	}

	protected override void OnKeyDown(KeyEventArgs e)
	{
		base.OnKeyDown(e);
		if (e.KeyCode == Keys.Escape)
		{
			Dispose();
		}
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		if (mouseDown)
		{
			SetLocation();
		}
		else
		{
			CopyScreen();
		}
		Point pos = Cursor.Position;
		Rectangle cr = RectangleToScreen(base.ClientRectangle);
		int dY = cr.Top - base.Top;
		int dX = cr.Left - base.Left;
		e.Graphics.TranslateTransform(base.Width / 2, base.Height / 2);
		e.Graphics.ScaleTransform(ZoomFactor, ZoomFactor);
		e.Graphics.TranslateTransform(-pos.X - dX, -pos.Y - dY);
		e.Graphics.Clear(BackColor);
		if (NearestNeighborInterpolation)
		{
			e.Graphics.InterpolationMode = InterpolationMode.NearestNeighbor;
			e.Graphics.PixelOffsetMode = PixelOffsetMode.Half;
		}
		if (scrBmp != null)
		{
			e.Graphics.DrawImage(scrBmp, 0, 0);
		}
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			timer.Dispose();
			scrBmp?.Dispose();
			scrGrp?.Dispose();
		}
		base.Dispose(disposing);
	}

	private void CopyScreen()
	{
		if (scrBmp == null)
		{
			Size sz = Screen.FromControl(this).Bounds.Size;
			scrBmp = new Bitmap(sz.Width, sz.Height);
			scrGrp = Graphics.FromImage(scrBmp);
		}
		scrGrp.CopyFromScreen(Point.Empty, Point.Empty, scrBmp.Size);
	}

	private void SetLocation()
	{
		Point p = Cursor.Position;
		base.Left = p.X - base.Width / 2;
		base.Top = p.Y - base.Height / 2;
	}
}
