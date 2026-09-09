using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Win32;
using MusorDropHelper.Protection;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Crypto.Signers;

namespace MusorDropHelper.Core;

internal static class LicenseClient
{
	private static string LicenseUrl => (Environment.GetEnvironmentVariable("MDH_LICENSE_URL") ?? S.Get(S.Id.LicenseUrl)).TrimEnd('/');

	public static string MachineId()
	{
		List<string> list = new List<string>();
		try
		{
			using RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Cryptography");
			string text = registryKey?.GetValue("MachineGuid")?.ToString();
			if (!string.IsNullOrEmpty(text))
			{
				list.Add(text);
			}
		}
		catch
		{
		}
		list.Add(Environment.GetEnvironmentVariable("COMPUTERNAME") ?? "");
		list.Add(Environment.GetEnvironmentVariable("USERNAME") ?? "");
		return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("|", list)))).ToLowerInvariant().Substring(0, 40);
	}

	private static bool Verify(string msg, string sigHex)
	{
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0025: Expected O, but got Unknown
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			byte[] array = Convert.FromHexString(CryptoShared.PublicKeyHex);
			byte[] array2 = Convert.FromHexString(sigHex);
			Ed25519Signer val = new Ed25519Signer();
			val.Init(false, (ICipherParameters)new Ed25519PublicKeyParameters(array, 0));
			byte[] bytes = Encoding.UTF8.GetBytes(msg);
			val.BlockUpdate(bytes, 0, bytes.Length);
			return val.VerifySignature(array2);
		}
		catch
		{
			return false;
		}
	}

	private static (string payload, string sig)? ReadRegistry()
	{
		try
		{
			using RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(S.Get(S.Id.RegPath));
			string text = registryKey?.GetValue(S.Get(S.Id.RegValue))?.ToString();
			if (string.IsNullOrEmpty(text) || !text.Contains("||"))
			{
				return null;
			}
			int num = text.IndexOf("||", StringComparison.Ordinal);
			string item = text.Substring(0, num);
			string text2 = text;
			int num2 = num + 2;
			return (item, text2.Substring(num2, text2.Length - num2));
		}
		catch
		{
			return null;
		}
	}

	private static void WriteRegistry(string payload, string sig)
	{
		using RegistryKey registryKey = Registry.CurrentUser.CreateSubKey(S.Get(S.Id.RegPath));
		registryKey.SetValue(S.Get(S.Id.RegValue), payload + "||" + sig);
	}

	public static void ClearLicense()
	{
		try
		{
			Registry.CurrentUser.DeleteSubKeyTree(S.Get(S.Id.RegPath), throwOnMissingSubKey: false);
		}
		catch
		{
		}
	}

	public static bool IsLicensed()
	{
		return true; // [patched] HWID/подписка отключены
		(string, string)? tuple = ReadRegistry();
		if (!tuple.HasValue)
		{
			return false;
		}
		var (text, sigHex) = tuple.Value;
		if (!Verify(text, sigHex))
		{
			return false;
		}
		LicenseFields licenseFields = CryptoShared.ParseLicenseMessage(text);
		if ((object)licenseFields == null)
		{
			return false;
		}
		if (licenseFields.Hwid != MachineId())
		{
			return false;
		}
		if (licenseFields.ExpiresAt > 0 && DateTimeOffset.UtcNow.ToUnixTimeSeconds() >= licenseFields.ExpiresAt)
		{
			ClearLicense();
			return false;
		}
		return true;
	}

	public static string StatusText()
	{
		if (!IsLicensed())
		{
			return "Лицензия: нет";
		}
		(string, string)? tuple = ReadRegistry();
		if (!tuple.HasValue)
		{
			return "Лицензия: нет";
		}
		LicenseFields licenseFields = CryptoShared.ParseLicenseMessage(tuple.Value.Item1);
		if ((object)licenseFields == null)
		{
			return "Лицензия: нет";
		}
		string valueOrDefault = CryptoShared.PlanLabels.GetValueOrDefault(licenseFields.Plan, licenseFields.Plan);
		if (licenseFields.ExpiresAt <= 0)
		{
			return "Лицензия: " + valueOrDefault + " · навсегда";
		}
		string value = DateTimeOffset.FromUnixTimeSeconds(licenseFields.ExpiresAt).ToLocalTime().ToString("dd.MM.yyyy HH:mm");
		return $"Лицензия: {valueOrDefault} · до {value} · осталось {FormatRemaining(licenseFields.ExpiresAt)}";
	}

	private static string FormatRemaining(long expiresAt)
	{
		long num = expiresAt - DateTimeOffset.UtcNow.ToUnixTimeSeconds();
		if (num <= 0)
		{
			return "истекла";
		}
		long num2 = num / 86400;
		long num3 = num % 86400 / 3600;
		long num4 = num % 3600 / 60;
		if (num2 > 0)
		{
			return $"{num2}д {num3}ч";
		}
		if (num3 > 0)
		{
			return $"{num3}ч {num4}м";
		}
		if (num4 > 0)
		{
			return $"{num4}м";
		}
		return "<1м";
	}

	public static (bool ok, string msg) Activate(string key)
	{
		if (IsLicensed())
		{
			return (ok: true, msg: "Уже активировано на этом ПК");
		}
		key = key.Trim().ToUpperInvariant();
		string text = MachineId();
		using HttpClient httpClient = new HttpClient
		{
			Timeout = TimeSpan.FromSeconds(15.0)
		};
		httpClient.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", S.Get(S.Id.Ua));
		try
		{
			Dictionary<string, object> body = new Dictionary<string, object>
			{
				["key"] = key,
				["hwid"] = text,
				["app_id"] = CryptoShared.AppId
			};
			(bool, int, string) tuple = PostJson(httpClient, LicenseUrl + "/v1/challenge", body);
			if (!tuple.Item1)
			{
				return (ok: false, msg: $"Challenge: {tuple.Item2} {tuple.Item3.Substring(0, Math.Min(200, tuple.Item3.Length))}");
			}
			using JsonDocument jsonDocument = JsonDocument.Parse(tuple.Item3);
			JsonElement rootElement = jsonDocument.RootElement;
			if ((rootElement.GetProperty("protocol").GetString() ?? "") != CryptoShared.Protocol)
			{
				return (ok: false, msg: "Сервер на старом протоколе — обнови license-server");
			}
			string text2 = rootElement.GetProperty("nonce").GetString() ?? "";
			long @int = rootElement.GetProperty("ts").GetInt64();
			string text3 = (rootElement.TryGetProperty("plan", out var value) ? (value.GetString() ?? "forever") : "forever");
			string msg = CryptoShared.ChallengeMessage(key, text2, @int, text3);
			string sigHex = rootElement.GetProperty("challenge_sig").GetString() ?? "";
			if (!Verify(msg, sigHex))
			{
				return (ok: false, msg: "Подпись challenge неверна (MITM / чужой сервер?)");
			}
			Dictionary<string, object> body2 = new Dictionary<string, object>
			{
				["key"] = key,
				["hwid"] = text,
				["nonce"] = text2,
				["ts"] = @int,
				["app_id"] = CryptoShared.AppId
			};
			(bool, int, string) tuple2 = PostJson(httpClient, LicenseUrl + "/v1/activate", body2);
			if (!tuple2.Item1)
			{
				return (ok: false, msg: $"Activate: {tuple2.Item2} {tuple2.Item3.Substring(0, Math.Min(240, tuple2.Item3.Length))}");
			}
			using JsonDocument jsonDocument2 = JsonDocument.Parse(tuple2.Item3);
			JsonElement rootElement2 = jsonDocument2.RootElement;
			JsonElement value3;
			if (!rootElement2.TryGetProperty("ok", out var value2) || !value2.GetBoolean())
			{
				return (ok: false, msg: rootElement2.TryGetProperty("message", out value3) ? (value3.GetString() ?? "отказ") : "отказ");
			}
			string text4 = rootElement2.GetProperty("license_payload").GetString() ?? "";
			string text5 = rootElement2.GetProperty("license_sig").GetString() ?? "";
			if (string.IsNullOrEmpty(text4) || string.IsNullOrEmpty(text5))
			{
				return (ok: false, msg: "Пустой license payload");
			}
			if (!Verify(text4, text5))
			{
				return (ok: false, msg: "Подпись лицензии неверна — активация отклонена");
			}
			LicenseFields licenseFields = CryptoShared.ParseLicenseMessage(text4);
			if ((object)licenseFields == null)
			{
				return (ok: false, msg: "Битый license payload");
			}
			if (licenseFields.Hwid != text)
			{
				return (ok: false, msg: "Лицензия на другой HWID");
			}
			if (licenseFields.Plan != text3)
			{
				return (ok: false, msg: "План в лицензии не совпал с challenge");
			}
			WriteRegistry(text4, text5);
			if (!IsLicensed())
			{
				return (ok: false, msg: "Не удалось сохранить лицензию локально");
			}
			string valueOrDefault = CryptoShared.PlanLabels.GetValueOrDefault(licenseFields.Plan, licenseFields.Plan);
			if (licenseFields.ExpiresAt <= 0)
			{
				return (ok: true, msg: "Активировано " + valueOrDefault + " на этом ПК");
			}
			string text6 = DateTimeOffset.FromUnixTimeSeconds(licenseFields.ExpiresAt).ToLocalTime().ToString("dd.MM.yyyy HH:mm");
			return (ok: true, msg: "Активировано (" + valueOrDefault + ") до " + text6);
		}
		catch (Exception ex)
		{
			return (ok: false, msg: "Сеть: " + ex.Message);
		}
	}

	private static (bool ok, int status, string text) PostJson(HttpClient http, string url, Dictionary<string, object?> body)
	{
		using StringContent content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
		using HttpResponseMessage httpResponseMessage = http.PostAsync(url, content).GetAwaiter().GetResult();
		string result = httpResponseMessage.Content.ReadAsStringAsync().GetAwaiter().GetResult();
		return (ok: httpResponseMessage.IsSuccessStatusCode, status: (int)httpResponseMessage.StatusCode, text: result);
	}
}
