using System;
using System.Collections.Generic;
using System.Text;

namespace MusorDropHelper.Protection;

internal static class S
{
	internal enum Id
	{
		StartMsg,
		WsUrl,
		Origin,
		Ua,
		LicenseUrl,
		RegPath,
		RegValue,
		Protocol,
		AppId,
		PubKeyHex
	}

	private static readonly byte[] K = Encoding.UTF8.GetBytes("mdh-cs-v1-guard-key!");

	private static readonly Lazy<Dictionary<Id, string>> Map = new Lazy<Dictionary<Id, string>>(() => new Dictionary<Id, string>
	{
		[Id.StartMsg] = Enc("Старт. Chromium WS (fingerprint) musor.best → SCORE."),
		[Id.WsUrl] = Enc("wss://musor.best/ws/"),
		[Id.Origin] = Enc("https://musor.best"),
		[Id.Ua] = Enc("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/131.0.0.0 Safari/537.36"),
		[Id.LicenseUrl] = Enc("http://89.110.108.79:8741"),
		[Id.RegPath] = Enc("Software\\MusorDropHelper"),
		[Id.RegValue] = Enc("License"),
		[Id.Protocol] = Enc("mdh2"),
		[Id.AppId] = Enc("musor-drop-helper"),
		[Id.PubKeyHex] = Enc("9fe08b001cd0443b8237f5d95d2e20a428a4674125e0fd79a8a849216b61ee06")
	});

	private static string D(string b64)
	{
		byte[] array = Convert.FromBase64String(b64);
		byte[] array2 = new byte[array.Length];
		for (int i = 0; i < array.Length; i++)
		{
			array2[i] = (byte)(array[i] ^ K[i % K.Length]);
		}
		return Encoding.UTF8.GetString(array2);
	}

	private static string Enc(string plain)
	{
		byte[] bytes = Encoding.UTF8.GetBytes(plain);
		byte[] array = new byte[bytes.Length];
		for (int i = 0; i < bytes.Length; i++)
		{
			array[i] = (byte)(bytes[i] ^ K[i % K.Length]);
		}
		return Convert.ToBase64String(array);
	}

	public static string Get(Id id)
	{
		return D(Map.Value[id]);
	}
}
