namespace relay;

internal sealed class StyledEventGridHost : Panel
{
	private const int BarWidth = 7;

	private const int BarMargin = 3;

	private static readonly Color TrackColor = Color.FromArgb(28, 33, 44);

	private static readonly Color ThumbColor = Color.FromArgb(88, 98, 118);

	private bool thumbDraggingV;

	private bool thumbDraggingH;

	private int thumbDragOffsetV;

	private int thumbDragOffsetH;

	public DataGridView Grid { get; private set; }

	private int Gutter => 13;

	private DataGridView GridView => Grid;

	private int TotalContentHeight => GridView.ColumnHeadersHeight + GridView.Rows.Count * GridView.RowTemplate.Height;

	private int TotalContentWidth
	{
		get
		{
			int num = 0;
			foreach (DataGridViewColumn column in GridView.Columns)
			{
				if (column.Visible)
				{
					num += column.Width;
				}
			}
			return num;
		}
	}

	private bool NeedsVerticalScroll => TotalContentHeight > GridView.ClientSize.Height;

	private bool NeedsHorizontalScroll => TotalContentWidth > GridView.ClientSize.Width;

	private int MaxFirstDisplayedRow
	{
		get
		{
			if (GridView.Rows.Count == 0)
			{
				return 0;
			}
			int num = GridView.DisplayedRowCount(includePartialRow: false);
			if (num <= 0)
			{
				num = Math.Max(1, (GridView.ClientSize.Height - GridView.ColumnHeadersHeight) / Math.Max(1, GridView.RowTemplate.Height));
			}
			return Math.Max(0, GridView.Rows.Count - num);
		}
	}

	private int MaxHorizontalOffset => Math.Max(0, TotalContentWidth - GridView.ClientSize.Width);

	private int VerticalTrackHeight
	{
		get
		{
			int num = base.ClientSize.Height - 6;
			if (NeedsHorizontalScroll)
			{
				num -= Gutter;
			}
			return Math.Max(0, num);
		}
	}

	private int HorizontalTrackWidth
	{
		get
		{
			int num = base.ClientSize.Width - 6;
			if (NeedsVerticalScroll)
			{
				num -= Gutter;
			}
			return Math.Max(0, num);
		}
	}

	public StyledEventGridHost()
	{
		DoubleBuffered = true;
		base.Padding = new Padding(0, 0, 13, 13);
		BackColor = Color.FromArgb(13, 16, 22);
		Grid = new DataGridView
		{
			Dock = DockStyle.Fill,
			ScrollBars = ScrollBars.None,
			BorderStyle = BorderStyle.None
		};
		base.Controls.Add(Grid);
		DataGridView grid = Grid;
		ScrollEventHandler value = delegate
		{
			InvalidateScrollbarArea();
		};
		grid.Scroll += value;
		Grid.RowsAdded += delegate
		{
			InvalidateScrollbarArea();
		};
		Grid.RowsRemoved += delegate
		{
			InvalidateScrollbarArea();
		};
		Grid.SizeChanged += delegate
		{
			InvalidateScrollbarArea();
		};
		Grid.ColumnWidthChanged += delegate
		{
			InvalidateScrollbarArea();
		};
		Grid.MouseWheel += Grid_MouseWheel;
	}

	private Rectangle GetVerticalTrackRect()
	{
		return new Rectangle(base.ClientSize.Width - 7 - 3, 3, 7, VerticalTrackHeight);
	}

	private Rectangle GetHorizontalTrackRect()
	{
		int y = base.ClientSize.Height - 7 - 3;
		return new Rectangle(3, y, HorizontalTrackWidth, 7);
	}

	private Rectangle GetVerticalThumbRect()
	{
		if (!NeedsVerticalScroll || VerticalTrackHeight <= 0)
		{
			return Rectangle.Empty;
		}
		int num = Math.Max(28, (int)((double)GridView.ClientSize.Height / (double)TotalContentHeight * (double)VerticalTrackHeight));
		int maxFirstDisplayedRow = MaxFirstDisplayedRow;
		int num2 = Math.Max(0, GridView.FirstDisplayedScrollingRowIndex);
		int y = ((maxFirstDisplayedRow == 0) ? 3 : (3 + (int)((double)num2 / (double)maxFirstDisplayedRow * (double)(VerticalTrackHeight - num))));
		return new Rectangle(base.ClientSize.Width - 7 - 3, y, 7, num);
	}

	private Rectangle GetHorizontalThumbRect()
	{
		if (!NeedsHorizontalScroll || HorizontalTrackWidth <= 0)
		{
			return Rectangle.Empty;
		}
		int num = Math.Max(28, (int)((double)GridView.ClientSize.Width / (double)TotalContentWidth * (double)HorizontalTrackWidth));
		int maxHorizontalOffset = MaxHorizontalOffset;
		int num2 = Math.Max(0, GridView.HorizontalScrollingOffset);
		int x = ((maxHorizontalOffset == 0) ? 3 : (3 + (int)((double)num2 / (double)maxHorizontalOffset * (double)(HorizontalTrackWidth - num))));
		int y = base.ClientSize.Height - 7 - 3;
		return new Rectangle(x, y, num, 7);
	}

	private void InvalidateScrollbarArea()
	{
		if (base.IsHandleCreated)
		{
			int gutter = Gutter;
			Invalidate(new Rectangle(base.ClientSize.Width - gutter, 0, gutter, base.ClientSize.Height));
			Invalidate(new Rectangle(0, base.ClientSize.Height - gutter, base.ClientSize.Width, gutter));
		}
	}

	private void Grid_MouseWheel(object sender, MouseEventArgs e)
	{
		if (HandleMouseWheel(e.Delta, (Control.ModifierKeys & Keys.Shift) == Keys.Shift))
		{
			InvalidateScrollbarArea();
		}
	}

	private bool HandleMouseWheel(int delta, bool horizontal)
	{
		if (delta == 0)
		{
			return false;
		}
		int num = Math.Max(1, Math.Abs(delta) / 120);
		if (horizontal && NeedsHorizontalScroll)
		{
			int num2 = ((delta <= 0) ? 1 : (-1));
			int val = GridView.HorizontalScrollingOffset + num2 * 40 * num;
			GridView.HorizontalScrollingOffset = Math.Max(0, Math.Min(MaxHorizontalOffset, val));
			return true;
		}
		if (!horizontal && NeedsVerticalScroll)
		{
			int num3 = ((delta <= 0) ? 1 : (-1));
			int val2 = GridView.FirstDisplayedScrollingRowIndex + num3 * num;
			GridView.FirstDisplayedScrollingRowIndex = Math.Max(0, Math.Min(MaxFirstDisplayedRow, val2));
			return true;
		}
		return false;
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
		Graphics graphics = e.Graphics;
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		if (NeedsVerticalScroll)
		{
			using (SolidBrush solidBrush = new SolidBrush(TrackColor))
			{
				graphics.FillRectangle(solidBrush, GetVerticalTrackRect());
			}
			Rectangle verticalThumbRect = GetVerticalThumbRect();
			if (!verticalThumbRect.IsEmpty)
			{
				using SolidBrush solidBrush2 = new SolidBrush(ThumbColor);
				graphics.FillRectangle(solidBrush2, verticalThumbRect);
			}
		}
		if (!NeedsHorizontalScroll)
		{
			return;
		}
		using (SolidBrush solidBrush3 = new SolidBrush(TrackColor))
		{
			graphics.FillRectangle(solidBrush3, GetHorizontalTrackRect());
		}
		Rectangle horizontalThumbRect = GetHorizontalThumbRect();
		if (horizontalThumbRect.IsEmpty)
		{
			return;
		}
		using SolidBrush solidBrush4 = new SolidBrush(ThumbColor);
		graphics.FillRectangle(solidBrush4, horizontalThumbRect);
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		if (!TryHandleScrollbarMouseDown(e.Location))
		{
			base.OnMouseDown(e);
		}
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		if (thumbDraggingV)
		{
			int num = Math.Max(28, (int)((double)GridView.ClientSize.Height / (double)TotalContentHeight * (double)VerticalTrackHeight));
			int num2 = VerticalTrackHeight - num;
			int val = e.Y - thumbDragOffsetV - 3;
			val = Math.Max(0, Math.Min(num2, val));
			int maxFirstDisplayedRow = MaxFirstDisplayedRow;
			GridView.FirstDisplayedScrollingRowIndex = ((num2 > 0) ? ((int)((double)val / (double)num2 * (double)maxFirstDisplayedRow)) : 0);
			InvalidateScrollbarArea();
		}
		else if (thumbDraggingH)
		{
			int num3 = Math.Max(28, (int)((double)GridView.ClientSize.Width / (double)TotalContentWidth * (double)HorizontalTrackWidth));
			int num4 = HorizontalTrackWidth - num3;
			int val2 = e.X - thumbDragOffsetH - 3;
			val2 = Math.Max(0, Math.Min(num4, val2));
			int maxHorizontalOffset = MaxHorizontalOffset;
			GridView.HorizontalScrollingOffset = ((num4 > 0) ? ((int)((double)val2 / (double)num4 * (double)maxHorizontalOffset)) : 0);
			InvalidateScrollbarArea();
		}
		else
		{
			base.OnMouseMove(e);
		}
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		base.OnMouseUp(e);
		thumbDraggingV = false;
		thumbDraggingH = false;
		base.Capture = false;
	}

	private bool TryHandleScrollbarMouseDown(Point location)
	{
		if (NeedsVerticalScroll)
		{
			Rectangle verticalThumbRect = GetVerticalThumbRect();
			if (verticalThumbRect.Contains(location))
			{
				thumbDraggingV = true;
				thumbDragOffsetV = location.Y - verticalThumbRect.Y;
				base.Capture = true;
				return true;
			}
			if (GetVerticalTrackRect().Contains(location))
			{
				int num = Math.Max(28, (int)((double)GridView.ClientSize.Height / (double)TotalContentHeight * (double)VerticalTrackHeight));
				int num2 = location.Y - num / 2;
				int num3 = VerticalTrackHeight - num;
				int num4 = Math.Max(0, Math.Min(num3, num2 - 3));
				int maxFirstDisplayedRow = MaxFirstDisplayedRow;
				GridView.FirstDisplayedScrollingRowIndex = ((num3 > 0) ? ((int)((double)num4 / (double)num3 * (double)maxFirstDisplayedRow)) : 0);
				InvalidateScrollbarArea();
				return true;
			}
		}
		if (NeedsHorizontalScroll)
		{
			Rectangle horizontalThumbRect = GetHorizontalThumbRect();
			if (horizontalThumbRect.Contains(location))
			{
				thumbDraggingH = true;
				thumbDragOffsetH = location.X - horizontalThumbRect.X;
				base.Capture = true;
				return true;
			}
			if (GetHorizontalTrackRect().Contains(location))
			{
				int num5 = Math.Max(28, (int)((double)GridView.ClientSize.Width / (double)TotalContentWidth * (double)HorizontalTrackWidth));
				int num6 = location.X - num5 / 2;
				int num7 = HorizontalTrackWidth - num5;
				int num8 = Math.Max(0, Math.Min(num7, num6 - 3));
				int maxHorizontalOffset = MaxHorizontalOffset;
				GridView.HorizontalScrollingOffset = ((num7 > 0) ? ((int)((double)num8 / (double)num7 * (double)maxHorizontalOffset)) : 0);
				InvalidateScrollbarArea();
				return true;
			}
		}
		return false;
	}
}
