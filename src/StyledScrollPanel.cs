namespace relay;

internal sealed class StyledScrollPanel : Panel
{
	private const int BarWidth = 7;

	private const int BarMargin = 3;

	private bool thumbDragging;

	private int thumbDragOffset;

	private int ContentHeight
	{
		get
		{
			int num = 0;
			foreach (Control control in base.Controls)
			{
				num = Math.Max(num, control.Bottom - base.AutoScrollPosition.Y);
			}
			return Math.Max(num + base.Padding.Bottom, base.AutoScrollMinSize.Height);
		}
	}

	private bool NeedsVerticalScroll => ContentHeight > base.ClientSize.Height;

	public StyledScrollPanel()
	{
		AutoScroll = true;
		DoubleBuffered = true;
		BackColor = Color.FromArgb(15, 18, 24);
		base.Padding = new Padding(0, 0, 13, 0);
	}

	public void RefreshContentHeight()
	{
		int height = 0;
		foreach (Control control in Controls) height = Math.Max(height, control.PreferredSize.Height + control.Margin.Vertical + Padding.Bottom);
		if (AutoScrollMinSize.Height != height) AutoScrollMinSize = new Size(0, height);
		Invalidate();
	}

	private Rectangle GetThumbRect()
	{
		int height = base.ClientSize.Height;
		int num = Math.Max(ContentHeight, DisplayRectangle.Height);
		if (num <= height)
		{
			return Rectangle.Empty;
		}
		int num2 = height - 6;
		int num3 = Math.Max(28, (int)((double)height / (double)num * (double)num2));
		int num4 = num - height;
		int num5 = Math.Max(0, -base.AutoScrollPosition.Y);
		int y = ((num4 == 0) ? 3 : (3 + (int)((double)num5 / (double)num4 * (double)(num2 - num3))));
		return new Rectangle(base.ClientSize.Width - 7 - 3, y, 7, num3);
	}

	private void SyncChildWidths()
	{
		int width = base.ClientSize.Width;
		int width2 = Math.Max(0, width - base.Padding.Horizontal);
		foreach (Control control in base.Controls)
		{
			control.Width = width2;
		}
	}

	private void HideSystemScrollBars()
	{
		if (base.IsHandleCreated)
		{
			NativeMethods.ShowScrollBar(base.Handle, 3, bShow: false);
		}
	}

	protected override void OnControlAdded(ControlEventArgs e)
	{
		base.OnControlAdded(e);
		SyncChildWidths();
		RefreshContentHeight();
	}

	protected override void OnSizeChanged(EventArgs e)
	{
		base.OnSizeChanged(e);
		SyncChildWidths();
		HideSystemScrollBars();
		Invalidate();
	}

	protected override void OnScroll(ScrollEventArgs se)
	{
		base.OnScroll(se);
		if (base.AutoScrollPosition.X != 0)
		{
			base.AutoScrollPosition = new Point(0, base.AutoScrollPosition.Y);
		}
		HideSystemScrollBars();
		Invalidate();
	}

	protected override void OnMouseWheel(MouseEventArgs e)
	{
		base.OnMouseWheel(e);
		Invalidate();
	}

	protected override void WndProc(ref Message m)
	{
		base.WndProc(ref m);
		if (m.Msg == 277 || m.Msg == 276 || m.Msg == 522 || m.Msg == 5)
		{
			HideSystemScrollBars();
			Invalidate();
		}
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
		if (!NeedsVerticalScroll)
		{
			return;
		}
		Graphics graphics = e.Graphics;
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		int x = base.ClientSize.Width - 7 - 3;
		Rectangle rect = new Rectangle(x, 3, 7, base.ClientSize.Height - 6);
		using (SolidBrush solidBrush = new SolidBrush(Color.FromArgb(28, 33, 44)))
		{
			graphics.FillRectangle(solidBrush, rect);
		}
		Rectangle thumbRect = GetThumbRect();
		if (thumbRect.IsEmpty)
		{
			return;
		}
		using SolidBrush solidBrush2 = new SolidBrush(Color.FromArgb(88, 98, 118));
		graphics.FillRectangle(solidBrush2, thumbRect);
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		base.OnMouseDown(e);
		if (!NeedsVerticalScroll)
		{
			return;
		}
		Rectangle thumbRect = GetThumbRect();
		if (thumbRect.Contains(e.Location))
		{
			thumbDragging = true;
			thumbDragOffset = e.Y - thumbRect.Y;
			base.Capture = true;
			return;
		}
		int x = base.ClientSize.Width - 7 - 3;
		Rectangle rectangle = new Rectangle(x, 3, 7, base.ClientSize.Height - 6);
		if (rectangle.Contains(e.Location))
		{
			int num = Math.Max(ContentHeight, DisplayRectangle.Height);
			int height = base.ClientSize.Height;
			int num2 = height - 6;
			int num3 = Math.Max(28, (int)((double)height / (double)num * (double)num2));
			int num4 = e.Y - num3 / 2;
			int num5 = num2 - num3;
			int num6 = Math.Max(0, Math.Min(num5, num4 - 3));
			int num7 = num - height;
			int y = ((num5 > 0) ? ((int)((double)num6 / (double)num5 * (double)num7)) : 0);
			base.AutoScrollPosition = new Point(0, y);
			Invalidate();
		}
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		base.OnMouseMove(e);
		if (thumbDragging)
		{
			int num = Math.Max(ContentHeight, DisplayRectangle.Height);
			int height = base.ClientSize.Height;
			int num2 = height - 6;
			int num3 = Math.Max(28, (int)((double)height / (double)num * (double)num2));
			int num4 = num2 - num3;
			int val = e.Y - thumbDragOffset - 3;
			val = Math.Max(0, Math.Min(num4, val));
			int num5 = num - height;
			int y = ((num4 > 0) ? ((int)((double)val / (double)num4 * (double)num5)) : 0);
			base.AutoScrollPosition = new Point(0, y);
			Invalidate();
		}
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		base.OnMouseUp(e);
		thumbDragging = false;
		base.Capture = false;
	}
}
