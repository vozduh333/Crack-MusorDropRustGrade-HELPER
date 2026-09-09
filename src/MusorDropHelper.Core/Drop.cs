using System;
using System.Text.Json;

namespace MusorDropHelper.Core;

internal sealed class Drop
{
	public string Id { get; init; } = "";

	public string ItemName { get; init; } = "";

	public double Price { get; init; }

	public string IconUrl { get; init; } = "";

	public string Rarity { get; init; } = "";

	public bool IsExpensive { get; init; }

	public bool IsStatTrak { get; init; }

	public double PriceRub { get; init; }

	public double Ts { get; init; }

	public static Drop FromSkin(JsonElement skin)
	{
		double price = (skin.TryGetProperty("price_usd", out var value) ? value.GetDouble() : 0.0) / 100.0;
		string itemName = (skin.TryGetProperty("market_name", out var value2) ? (value2.GetString() ?? "") : "");
		if (skin.TryGetProperty("name", out var value3) && value3.ValueKind == JsonValueKind.Object)
		{
			if (value3.TryGetProperty("ru", out var value4))
			{
				string text = value4.GetString();
				if (text != null && text.Length > 0)
				{
					itemName = text;
					goto IL_00c5;
				}
			}
			if (value3.TryGetProperty("en", out var value5))
			{
				string text2 = value5.GetString();
				if (text2 != null && text2.Length > 0)
				{
					itemName = text2;
				}
			}
		}
		goto IL_00c5;
		IL_00c5:
		string id = (skin.TryGetProperty("uuid", out var value6) ? (value6.GetString() ?? "") : (skin.TryGetProperty("unique_id", out var value7) ? (value7.GetString() ?? "") : ""));
		JsonElement value8;
		JsonElement value9;
		JsonElement value10;
		return new Drop
		{
			Id = id,
			ItemName = itemName,
			Price = price,
			IconUrl = (skin.TryGetProperty("image_url", out value8) ? (value8.GetString() ?? "") : ""),
			Rarity = (skin.TryGetProperty("rarity", out value9) ? (value9.GetString() ?? "") : ""),
			IsExpensive = Truthy(skin, "is_expensive"),
			IsStatTrak = Truthy(skin, "is_stattrak"),
			PriceRub = (skin.TryGetProperty("price_rub", out value10) ? value10.GetDouble() : 0.0) / 100.0,
			Ts = (double)DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0
		};
	}

	private static bool Truthy(JsonElement skin, string name)
	{
		if (!skin.TryGetProperty(name, out var value))
		{
			return false;
		}
		switch (value.ValueKind)
		{
		case JsonValueKind.True:
			return true;
		case JsonValueKind.Number:
		{
			int value2;
			return value.TryGetInt32(out value2) ? (value2 != 0) : (value.GetDouble() != 0.0);
		}
		case JsonValueKind.String:
		{
			bool result;
			switch (value.GetString())
			{
			case "1":
			case "true":
			case "True":
				result = true;
				break;
			default:
				result = false;
				break;
			}
			return result;
		}
		default:
			return false;
		}
	}
}
