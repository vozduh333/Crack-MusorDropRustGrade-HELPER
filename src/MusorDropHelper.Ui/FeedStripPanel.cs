using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using MusorDropHelper.Core;

namespace MusorDropHelper.Ui;

internal sealed class FeedStripPanel : Panel
{
	public const int CardW = 132;

	public const int CardH = 168;

	public const int IconSize = 64;

	public const int Gap = 8;

	public const int MaxSlots = 14;

	private readonly List<Drop> _newestFirst = new List<Drop>();

	private readonly ConcurrentDictionary<string, Image> _icons;

	private int _visible;

	public int VisibleSlots => _visible;

	public FeedStripPanel(ConcurrentDictionary<string, Image> icons)
	{
		_icons = icons;
		DoubleBuffered = true;
		BackColor = UiTheme.Panel;
		base.Height = 196;
		base.Resize += delegate
		{
			Invalidate();
		};
	}

	public void SetDrops(IEnumerable<Drop> newestFirst)
	{
		_newestFirst.Clear();
		_newestFirst.AddRange(newestFirst.Take(14));
		Invalidate();
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);
		Graphics graphics = e.Graphics;
		graphics.SmoothingMode = SmoothingMode.AntiAlias;
		graphics.InterpolationMode = InterpolationMode.HighQualityBilinear;
		graphics.Clear(UiTheme.Panel);
		_visible = Math.Max(4, Math.Min(14, (base.Width - 8) / 140));
		for (int i = 0; i < _visible && i < _newestFirst.Count; i++)
		{
			Drop drop = _newestFirst[i];
			(Color, Color, Color, string) tuple = UiTheme.TierStyle(drop.Price);
			int num = 8 + i * 140;
			int num2 = 10;
			using (Pen pen = new Pen(tuple.Item2, 1f))
			{
				graphics.DrawRectangle(pen, num - 1, num2 - 1, 134, 170);
			}
			using (SolidBrush brush = new SolidBrush(tuple.Item3))
			{
				graphics.FillRectangle(brush, num, num2, 4, 168);
			}
			using (SolidBrush brush2 = new SolidBrush(tuple.Item1))
			{
				using Pen pen2 = new Pen(tuple.Item2, 2f);
				graphics.FillRectangle(brush2, num, num2, 132, 168);
				graphics.DrawRectangle(pen2, num, num2, 132, 168);
			}
			if (!string.IsNullOrEmpty(drop.IconUrl) && _icons.TryGetValue(drop.IconUrl, out Image value))
			{
				int x = num + 34;
				int y = num2 + 18;
				graphics.DrawImage(value, x, y, 64, 64);
			}
			string s = ((drop.ItemName.Length <= 36) ? drop.ItemName : (drop.ItemName.Substring(0, 34) + "…"));
			using (Font font = new Font("Segoe UI", 8f))
			{
				using SolidBrush brush3 = new SolidBrush(UiTheme.Text);
				RectangleF layoutRectangle = new RectangleF(num + 7, num2 + 100, 118f, 36f);
				graphics.DrawString(s, font, brush3, layoutRectangle);
			}
			using (Font font2 = new Font("Segoe UI", 11f, FontStyle.Bold))
			{
				using SolidBrush brush4 = new SolidBrush(tuple.Item3);
				string text = $"${drop.Price:0.00}";
				graphics.DrawString(text, font2, brush4, (float)num + (132f - graphics.MeasureString(text, font2).Width) / 2f, num2 + 138);
			}
			string text2 = (drop.IsStatTrak ? "ST" : (drop.IsExpensive ? "EXP" : ""));
			if (text2.Length <= 0)
			{
				continue;
			}
			using Font font3 = new Font("Segoe UI", 7f, FontStyle.Bold);
			using SolidBrush brush5 = new SolidBrush(tuple.Item3);
			graphics.DrawString(text2, font3, brush5, num + 132 - 28, num2 + 8);
		}
	}
}
