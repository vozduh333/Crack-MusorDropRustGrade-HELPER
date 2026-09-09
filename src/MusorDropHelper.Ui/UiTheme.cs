using System.Collections.Generic;
using System.Drawing;

namespace MusorDropHelper.Ui;

internal static class UiTheme
{
	public static readonly Color Bg = Color.FromArgb(7, 6, 12);

	public static readonly Color Panel = Color.FromArgb(16, 14, 24);

	public static readonly Color Panel2 = Color.FromArgb(23, 19, 34);

	public static readonly Color Line = Color.FromArgb(50, 42, 72);

	public static readonly Color Text = Color.FromArgb(246, 240, 255);

	public static readonly Color Muted = Color.FromArgb(163, 152, 188);

	public static readonly Color Dim = Color.FromArgb(110, 100, 136);

	public static readonly Color Accent = Color.FromArgb(200, 134, 255);

	public static readonly Color Cyan = Color.FromArgb(120, 217, 255);

	public static readonly (double lim, Color fill, Color outline, Color price, string name, string label)[] PriceTiers = new(double, Color, Color, Color, string, string)[6]
	{
		(0.5, Color.FromArgb(20, 24, 32), Color.FromArgb(58, 68, 88), Color.FromArgb(154, 166, 186), "trash", "<$0.50"),
		(2.0, Color.FromArgb(16, 26, 22), Color.FromArgb(63, 112, 84), Color.FromArgb(125, 212, 164), "cheap", "$0.5–2"),
		(8.0, Color.FromArgb(23, 20, 38), Color.FromArgb(111, 82, 168), Color.FromArgb(200, 160, 255), "mid", "$2–8"),
		(25.0, Color.FromArgb(28, 18, 34), Color.FromArgb(168, 85, 152), Color.FromArgb(255, 154, 214), "good", "$8–25"),
		(80.0, Color.FromArgb(36, 16, 24), Color.FromArgb(212, 101, 85), Color.FromArgb(255, 143, 112), "fat", "$25–80"),
		(1000000000.0, Color.FromArgb(42, 26, 16), Color.FromArgb(232, 168, 40), Color.FromArgb(255, 211, 77), "god", "$80+")
	};

	public static readonly Dictionary<string, (Color bg, Color fg, Color bar)> LevelTheme = new Dictionary<string, (Color, Color, Color)>
	{
		["COLD"] = (Color.FromArgb(17, 19, 27), Color.FromArgb(154, 164, 191), Color.FromArgb(93, 103, 134)),
		["WARM"] = (Color.FromArgb(25, 20, 40), Color.FromArgb(217, 176, 255), Color.FromArgb(168, 102, 240)),
		["HOT"] = (Color.FromArgb(37, 19, 42), Color.FromArgb(255, 159, 219), Color.FromArgb(232, 88, 192)),
		["FIRE"] = (Color.FromArgb(46, 16, 52), Color.FromArgb(255, 124, 255), Color.FromArgb(216, 72, 255))
	};

	public static (Color fill, Color outline, Color price, string name) TierStyle(double price)
	{
		(double, Color, Color, Color, string, string)[] priceTiers = PriceTiers;
		for (int i = 0; i < priceTiers.Length; i++)
		{
			(double, Color, Color, Color, string, string) tuple = priceTiers[i];
			if (price < tuple.Item1)
			{
				return (fill: tuple.Item2, outline: tuple.Item3, price: tuple.Item4, name: tuple.Item5);
			}
		}
		(double, Color, Color, Color, string, string) tuple2 = PriceTiers[^1];
		return (fill: tuple2.Item2, outline: tuple2.Item3, price: tuple2.Item4, name: tuple2.Item5);
	}
}
