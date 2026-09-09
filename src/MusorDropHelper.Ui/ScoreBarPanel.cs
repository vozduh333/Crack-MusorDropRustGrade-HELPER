using System;
using System.Drawing;
using System.Windows.Forms;

namespace MusorDropHelper.Ui;

internal sealed class ScoreBarPanel : Panel
{
	private double _score;

	private Color _bar = UiTheme.LevelTheme["COLD"].bar;

	public ScoreBarPanel()
	{
		base.Height = 10;
		DoubleBuffered = true;
	}

	public void Set(double score, Color bar)
	{
		_score = score;
		_bar = bar;
		Invalidate();
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
		Graphics graphics = e.Graphics;
		using (SolidBrush brush = new SolidBrush(Color.FromArgb(8, 6, 12)))
		{
			graphics.FillRectangle(brush, 0, 0, base.Width, base.Height);
		}
		int width = (int)((double)base.Width * Math.Clamp(_score, 0.0, 100.0) / 100.0);
		using SolidBrush brush2 = new SolidBrush(_bar);
		graphics.FillRectangle(brush2, 0, 0, width, base.Height);
	}
}
