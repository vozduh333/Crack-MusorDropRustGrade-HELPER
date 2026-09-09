using System.Collections.Generic;
using MusorDropHelper.Protection;

namespace MusorDropHelper.Core;

internal static class CryptoShared
{
	public static readonly Dictionary<string, string> PlanLabels = new Dictionary<string, string>
	{
		["3d"] = "3 дня",
		["7d"] = "7 дней",
		["forever"] = "навсегда"
	};

	public static string Protocol => S.Get(S.Id.Protocol);

	public static string AppId => S.Get(S.Id.AppId);

	public static string PublicKeyHex => S.Get(S.Id.PubKeyHex);

	public static string ChallengeMessage(string key, string nonce, long ts, string plan)
	{
		return $"{Protocol}|chal|{AppId}|{key}|{nonce}|{ts}|{plan}";
	}

	public static LicenseFields? ParseLicenseMessage(string payload)
	{
		string[] array = payload.Split('|');
		if (array.Length != 8)
		{
			return null;
		}
		if (array[0] != Protocol || array[1] != "lic" || array[2] != AppId)
		{
			return null;
		}
		if (!long.TryParse(array[5], out var result))
		{
			return null;
		}
		if (!long.TryParse(array[7], out var result2))
		{
			return null;
		}
		return new LicenseFields(array[3], array[4], result, array[6], result2);
	}
}
