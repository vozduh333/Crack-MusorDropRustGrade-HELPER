using System;
using System.Collections.Generic;
using System.Linq;

namespace MusorDropHelper.Core;

internal sealed class Analyzer
{
	private const double TierTrash = 0.5;

	private const double TierCheap = 2.0;

	private const double TierDecent = 8.0;

	private const double TierGood = 25.0;

	private const double TierFat = 80.0;

	private static readonly HashSet<string> HeatRarity = new HashSet<string>(StringComparer.Ordinal) { "rarity_legendary_weapon", "rarity_ancient_weapon", "rarity_ancient", "rarity_legendary", "rarity_contraband", "rarity_unusual" };

	private readonly int _window = 50;

	private readonly int _shortN = 12;

	private readonly LinkedList<Drop> _drops = new LinkedList<Drop>();

	private readonly Queue<double> _recentTs = new Queue<double>();

	private double _emaScore = 52.0;

	public int CheapStreak { get; private set; }

	public int HeatStreak { get; private set; }

	public int DroughtBeforeHeat { get; private set; }

	public int Total { get; private set; }

	public Signal? LastSignal { get; private set; }

	private bool IsCheap(Drop d)
	{
		return d.Price < 2.0;
	}

	private bool IsHeat(Drop d)
	{
		if (d.IsExpensive || d.Price >= 8.0)
		{
			return true;
		}
		if (HeatRarity.Contains(d.Rarity))
		{
			return true;
		}
		if (!d.ItemName.ToLowerInvariant().Contains("knife"))
		{
			return d.ItemName.Contains('★');
		}
		return true;
	}

	private void Ingest(Drop drop)
	{
		_drops.AddLast(drop);
		while (_drops.Count > 800)
		{
			_drops.RemoveFirst();
		}
		_recentTs.Enqueue(drop.Ts);
		while (_recentTs.Count > 50)
		{
			_recentTs.Dequeue();
		}
		Total++;
		if (IsCheap(drop) && !IsHeat(drop))
		{
			CheapStreak++;
			HeatStreak = 0;
			return;
		}
		if (HeatStreak == 0 && CheapStreak > 0)
		{
			DroughtBeforeHeat = CheapStreak;
		}
		HeatStreak = (IsHeat(drop) ? (HeatStreak + 1) : 0);
		CheapStreak = 0;
	}

	public Signal Push(Drop drop)
	{
		Ingest(drop);
		LastSignal = ComputeSignal();
		return LastSignal;
	}

	public Signal? PushMany(IEnumerable<Drop> drops)
	{
		bool flag = false;
		foreach (Drop drop in drops)
		{
			Ingest(drop);
			flag = true;
		}
		if (!flag)
		{
			return LastSignal;
		}
		LastSignal = ComputeSignal();
		return LastSignal;
	}

	private List<Drop> Recent(int n)
	{
		return _drops.TakeLast(n).ToList();
	}

	public double DropsPerMin()
	{
		List<double> list = _recentTs.ToList();
		if (list.Count < 2)
		{
			return 0.0;
		}
		double num = list[list.Count - 1] - list[0];
		if (!(num <= 0.0))
		{
			return (double)(list.Count - 1) / num * 60.0;
		}
		return 0.0;
	}

	public Dictionary<string, double> WindowStats()
	{
		List<Drop> list = Recent(_window);
		List<Drop> list2 = Recent(_shortN);
		if (list.Count == 0)
		{
			return new Dictionary<string, double>
			{
				["n"] = 0.0,
				["avg_price"] = 0.0,
				["momentum"] = 1.0,
				["heat_share"] = 0.0,
				["cheap_share"] = 0.0,
				["good_share"] = 0.0,
				["short_heat"] = 0.0,
				["exp_share"] = 0.0
			};
		}
		int count = list.Count;
		double num = list.Average((Drop d) => d.Price);
		double num2 = ((list2.Count > 0) ? list2.Average((Drop d) => d.Price) : num);
		return new Dictionary<string, double>
		{
			["n"] = count,
			["avg_price"] = num,
			["momentum"] = ((num > 0.05) ? (num2 / num) : 1.0),
			["heat_share"] = (double)list.Count(IsHeat) / (double)count,
			["cheap_share"] = (double)list.Count((Drop d) => IsCheap(d) && !IsHeat(d)) / (double)count,
			["good_share"] = (double)list.Count((Drop d) => d.Price >= 25.0) / (double)count,
			["short_heat"] = ((list2.Count > 0) ? ((double)list2.Count(IsHeat) / (double)list2.Count) : 0.0),
			["exp_share"] = (double)list.Count((Drop d) => d.IsExpensive || d.Price >= 80.0) / (double)count
		};
	}

	public Signal ComputeSignal()
	{
		Dictionary<string, double> dictionary = WindowStats();
		if (dictionary["n"] < 5.0)
		{
			return new Signal("COLD", 48.0, "Мало данных", "Ждём ленту musor.best…");
		}
		double num = 54.0;
		List<string> list = new List<string>();
		Drop value = _drops.Last.Value;
		double num2 = dictionary["momentum"];
		if (num2 >= 2.0)
		{
			num += 14.0;
			list.Add($"spike ×{num2:0.0}");
		}
		else if (num2 >= 1.45)
		{
			num += 10.0;
			list.Add($"рост ×{num2:0.0}");
		}
		else if (num2 >= 1.15)
		{
			num += 5.0;
		}
		else if (num2 <= 0.7)
		{
			num -= 12.0;
			list.Add($"просадка ×{num2:0.0}");
		}
		else if (num2 <= 0.92)
		{
			num -= 5.0;
		}
		if (dictionary["heat_share"] >= 0.38)
		{
			num += 12.0;
			list.Add($"heat {dictionary["heat_share"] * 100.0:0}%");
		}
		else if (dictionary["heat_share"] >= 0.22)
		{
			num += 7.0;
		}
		else if (dictionary["heat_share"] >= 0.1)
		{
			num += 4.0;
		}
		else if (dictionary["cheap_share"] >= 0.85)
		{
			num -= 7.0;
			list.Add($"суши {dictionary["cheap_share"] * 100.0:0}%");
		}
		else if (dictionary["cheap_share"] >= 0.72)
		{
			num -= 4.0;
		}
		if (dictionary["short_heat"] >= 0.45)
		{
			num += 7.0;
		}
		else if (dictionary["short_heat"] >= 0.28)
		{
			num += 4.0;
		}
		if (dictionary["good_share"] >= 0.1)
		{
			num += 7.0;
			list.Add($"good+ {dictionary["good_share"] * 100.0:0}%");
		}
		if (dictionary["exp_share"] >= 0.05)
		{
			num += 6.0;
			list.Add($"expensive {dictionary["exp_share"] * 100.0:0}%");
		}
		if (DroughtBeforeHeat >= 8 && HeatStreak >= 2)
		{
			num += (double)Math.Min(14, 5 + DroughtBeforeHeat / 3);
			list.Add($"после суши {DroughtBeforeHeat}");
		}
		else if (CheapStreak >= 12)
		{
			num -= 5.0;
			list.Add($"суши ×{CheapStreak}");
		}
		if (HeatStreak >= 4)
		{
			num += 9.0;
			list.Add($"heat×{HeatStreak}");
		}
		else if (HeatStreak >= 3)
		{
			num += 6.0;
		}
		else if (HeatStreak >= 2)
		{
			num += 3.0;
		}
		if (value.Price >= 80.0)
		{
			num += 9.0;
			list.Add($"FAT ${value.Price:0.00}");
		}
		else if (value.Price >= 25.0)
		{
			num += 6.0;
		}
		else if (value.Price >= 8.0)
		{
			num += 3.0;
		}
		else if (value.IsExpensive)
		{
			num += 4.0;
		}
		if (dictionary["avg_price"] >= 8.0)
		{
			num += 5.0;
		}
		else if (dictionary["avg_price"] >= 3.0)
		{
			num += 2.0;
		}
		else if (dictionary["avg_price"] < 0.8 && num2 < 1.1)
		{
			num -= 8.0;
			list.Add($"avg ${dictionary["avg_price"]:0.00}");
		}
		if (DropsPerMin() >= 40.0)
		{
			num += 2.0;
		}
		double num3 = Math.Clamp(num, 0.0, 100.0);
		double num4 = ((num3 >= _emaScore) ? 0.36 : 0.28);
		_emaScore = (1.0 - num4) * _emaScore + num4 * num3;
		num = Math.Clamp(_emaScore, 0.0, 100.0);
		string level;
		string title;
		string text;
		if (num >= 80.0)
		{
			level = "FIRE";
			title = "КРУТИТЬ СЕЙЧАС";
			text = "Окно раздачи: жирные/дорогие скины после суши.";
		}
		else if (num >= 65.0)
		{
			level = "HOT";
			title = "Пора пробовать";
			text = "Лента разогрелась — decent+ и momentum вверх.";
		}
		else if (num >= 42.0)
		{
			level = "WARM";
			title = "Разогрев";
			text = "Обычная лента / лёгкий разогрев.";
		}
		else
		{
			level = "COLD";
			title = "Жди / копи";
			text = "Фаза сбора: лента дешёвая.";
		}
		string text2 = text;
		if (list.Count > 0)
		{
			text2 = text2 + " · " + string.Join("; ", list.Take(6));
		}
		return new Signal(level, num, title, text2);
	}

	public IReadOnlyList<Drop> Latest(int n)
	{
		return Recent(n);
	}
}
