using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using MusorDropHelper.Protection;

namespace MusorDropHelper.Core;

internal sealed class FeedClient : IDisposable
{
	private readonly Action<Drop> _onDrop;

	private readonly Action<string> _onStatus;

	private readonly Action<string> _onLog;

	private Control? _host;

	private WebView2? _wv;

	private Panel? _challengeBar;

	private Label? _challengeLbl;

	private bool _started;

	private bool _stopping;

	private bool _challengeUi;

	private bool _wsLive;

	private int _navTries;

	private Timer? _watchdog;

	private Timer? _challengePoll;

	public FeedClient(Action<Drop> onDrop, Action<string> onStatus, Action<string> onLog)
	{
		_onDrop = onDrop;
		_onStatus = onStatus;
		_onLog = onLog;
	}

	public void Start(Control uiHost)
	{
		if (_started)
		{
			return;
		}
		_started = true;
		_host = uiHost;
		if (uiHost.IsHandleCreated)
		{
			uiHost.BeginInvoke(InitUi);
			return;
		}
		uiHost.HandleCreated += delegate
		{
			uiHost.BeginInvoke(InitUi);
		};
	}

	public void Stop()
	{
		_stopping = true;
		try
		{
			_watchdog?.Stop();
		}
		catch
		{
		}
		try
		{
			_challengePoll?.Stop();
		}
		catch
		{
		}
		try
		{
			Control host = _host;
			if (host != null && host.IsHandleCreated && !_host.IsDisposed)
			{
				_host.BeginInvoke(DisposeUi);
			}
			else
			{
				DisposeUi();
			}
		}
		catch
		{
			DisposeUi();
		}
	}

	public void Dispose()
	{
		Stop();
	}

	private async void InitUi()
	{
		if (_stopping || _host == null || _host.IsDisposed)
		{
			return;
		}
		try
		{
			_onStatus("Chromium boot…");
			EnsureChallengeBar();
			FeedClient feedClient = this;
			WebView2 val = new WebView2();
			((Control)val).Visible = false;
			((Control)val).Width = 2;
			((Control)val).Height = 2;
			((Control)val).Left = -4000;
			((Control)val).Top = -4000;
			((Control)val).TabStop = true;
			feedClient._wv = val;
			_host.Controls.Add((Control?)(object)_wv);
			string text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MusorDropHelper", "wv2-profile");
			Directory.CreateDirectory(text);
			CoreWebView2EnvironmentOptions val2 = new CoreWebView2EnvironmentOptions("--lang=ru-RU,ru,en-US,en", (string)null, (string)null, false, (List<CoreWebView2CustomSchemeRegistration>)null);
			CoreWebView2Environment val3 = await CoreWebView2Environment.CreateAsync((string)null, text, val2);
			await _wv.EnsureCoreWebView2Async(val3);
			CoreWebView2 coreWebView = _wv.CoreWebView2;
			coreWebView.Settings.AreDefaultContextMenusEnabled = true;
			coreWebView.Settings.AreDevToolsEnabled = false;
			coreWebView.Settings.IsStatusBarEnabled = false;
			coreWebView.Settings.IsZoomControlEnabled = false;
			coreWebView.Settings.UserAgent = S.Get(S.Id.Ua);
			coreWebView.Settings.IsWebMessageEnabled = true;
			coreWebView.AddWebResourceRequestedFilter("*://musor.best/*", (CoreWebView2WebResourceContext)0);
			coreWebView.AddWebResourceRequestedFilter("*://*.ddos-guard.net/*", (CoreWebView2WebResourceContext)0);
			coreWebView.WebResourceRequested += delegate(object? _, CoreWebView2WebResourceRequestedEventArgs e)
			{
				try
				{
					CoreWebView2WebResourceRequest request = e.Request;
					request.Headers.SetHeader("Accept-Language", "ru-RU,ru;q=0.9,en-US;q=0.8,en;q=0.7");
					request.Headers.SetHeader("sec-ch-ua", "\"Google Chrome\";v=\"131\", \"Chromium\";v=\"131\", \"Not_A Brand\";v=\"24\"");
					request.Headers.SetHeader("sec-ch-ua-mobile", "?0");
					request.Headers.SetHeader("sec-ch-ua-platform", "\"Windows\"");
				}
				catch
				{
				}
			};
			coreWebView.WebMessageReceived += OnWebMessage;
			coreWebView.NavigationCompleted += OnNavCompleted;
			coreWebView.DocumentTitleChanged += delegate
			{
				CheckChallengeAsync();
			};
			coreWebView.ProcessFailed += delegate(object? _, CoreWebView2ProcessFailedEventArgs e)
			{
				//IL_001f: Unknown result type (might be due to invalid IL or missing references)
				_onLog($"Chromium process: {e.ProcessFailedKind}");
				_onStatus("Chromium crash — restart…");
				if (!_challengeUi)
				{
					ScheduleReload(2500);
				}
			};
			_watchdog = new Timer
			{
				Interval = 40000
			};
			_watchdog.Tick += delegate
			{
				if (!_stopping && !_challengeUi)
				{
					ProbeAndReconnectAsync();
				}
			};
			_watchdog.Start();
			_challengePoll = new Timer
			{
				Interval = 900
			};
			_challengePoll.Tick += delegate
			{
				if (!_stopping)
				{
					CheckChallengeAsync();
				}
			};
			_challengePoll.Start();
			ShowChallengeUi("Проверка браузера… Если вылезет капча — пройди её здесь.");
			NavigateHome();
		}
		catch (Exception ex)
		{
			_onStatus("Нет WebView2 Runtime");
			_onLog("Chromium init fail: " + ex.Message);
			_onLog("Поставь Evergreen WebView2 Runtime (Edge).");
		}
	}

	private void EnsureChallengeBar()
	{
		if (_host != null && _challengeBar == null)
		{
			_challengeBar = new Panel
			{
				Dock = DockStyle.Top,
				Height = 44,
				BackColor = Color.FromArgb(26, 18, 40),
				Visible = false
			};
			_challengeLbl = new Label
			{
				Dock = DockStyle.Fill,
				ForeColor = Color.FromArgb(240, 230, 255),
				Font = new Font("Segoe UI", 10f, FontStyle.Bold),
				TextAlign = ContentAlignment.MiddleLeft,
				Padding = new Padding(14, 0, 14, 0),
				Text = "DDoS-Guard: пройди проверку / капчу в окне ниже. После этого лента подключится сама."
			};
			Label value = new Label
			{
				Dock = DockStyle.Right,
				AutoSize = false,
				Width = 220,
				ForeColor = Color.FromArgb(163, 152, 188),
				Font = new Font("Segoe UI", 8.5f),
				TextAlign = ContentAlignment.MiddleRight,
				Padding = new Padding(8, 0, 14, 0),
				Text = "после капчи окно свернётся"
			};
			_challengeBar.Controls.Add(_challengeLbl);
			_challengeBar.Controls.Add(value);
			_host.Controls.Add(_challengeBar);
		}
	}

	private void ShowChallengeUi(string text)
	{
		if (_host != null && _wv != null)
		{
			if (_challengeLbl != null)
			{
				_challengeLbl.Text = text;
			}
			if (!_challengeUi)
			{
				_challengeUi = true;
				_onLog("Капча/защита: WebView на весь экран — пройди проверку.");
			}
			_onStatus("Капча — пройди проверку в окне");
			if (_challengeBar != null)
			{
				_challengeBar.Visible = true;
				_challengeBar.BringToFront();
			}
			((Control)(object)_wv).Dock = DockStyle.Fill;
			((Control)(object)_wv).Visible = true;
			((Control)(object)_wv).BringToFront();
			if (_challengeBar != null)
			{
				_challengeBar.BringToFront();
			}
		}
	}

	private void HideChallengeUi()
	{
		if (_wv != null)
		{
			if (_challengeUi)
			{
				_challengeUi = false;
				_onLog("Защита пройдена — WebView свёрнут, слушаем ленту.");
			}
			if (_challengeBar != null)
			{
				_challengeBar.Visible = false;
			}
			((Control)(object)_wv).Dock = DockStyle.None;
			((Control)(object)_wv).Visible = false;
			((Control)(object)_wv).Width = 2;
			((Control)(object)_wv).Height = 2;
			((Control)(object)_wv).Left = -4000;
			((Control)(object)_wv).Top = -4000;
			((Control)(object)_wv).SendToBack();
		}
	}

	private void NavigateHome()
	{
		WebView2? wv = _wv;
		if (((wv != null) ? wv.CoreWebView2 : null) == null || _stopping)
		{
			return;
		}
		_navTries++;
		_wsLive = false;
		if (!_challengeUi)
		{
			ShowChallengeUi("Загрузка musor.best…");
		}
		_onStatus((_navTries <= 1) ? "Отпечаток: musor.best…" : $"Отпечаток: retry #{_navTries}…");
		try
		{
			_wv.CoreWebView2.Navigate(S.Get(S.Id.Origin) + "/");
		}
		catch (Exception ex)
		{
			_onLog("Navigate: " + ex.Message);
			ScheduleReload(3000);
		}
	}

	private async void OnNavCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
	{
		if (_stopping)
		{
			return;
		}
		WebView2? wv = _wv;
		if (((wv != null) ? wv.CoreWebView2 : null) == null)
		{
			return;
		}
		if (!e.IsSuccess)
		{
			_onLog($"Nav fail status={e.HttpStatusCode}");
			ShowChallengeUi($"Ошибка загрузки ({e.HttpStatusCode}). Обновится само…");
			ScheduleReload(4000);
			return;
		}
		await Task.Delay(900);
		if (_stopping)
		{
			return;
		}
		WebView2? wv2 = _wv;
		if (((wv2 != null) ? wv2.CoreWebView2 : null) == null)
		{
			return;
		}
		if (await IsChallengeAsync())
		{
			ShowChallengeUi("DDoS-Guard / капча — пройди проверку в этом окне. Лента подключится сама.");
			return;
		}
		try
		{
			await _wv.CoreWebView2.ExecuteScriptAsync(BridgeScript());
			_onStatus("Chromium WS…");
		}
		catch (Exception ex)
		{
			_onLog("Inject: " + ex.Message);
			ScheduleReload(4000);
		}
	}

	private async Task CheckChallengeAsync()
	{
		if (_stopping)
		{
			return;
		}
		WebView2? wv = _wv;
		if (((wv != null) ? wv.CoreWebView2 : null) == null)
		{
			return;
		}
		try
		{
			if (await IsChallengeAsync())
			{
				ShowChallengeUi("DDoS-Guard / капча — пройди проверку в этом окне. Лента подключится сама.");
			}
			else if (!_wsLive)
			{
				string text = await GetWsStateAsync();
				if ((!(text == "open") && !(text == "connecting")) || 1 == 0)
				{
					await _wv.CoreWebView2.ExecuteScriptAsync(BridgeScript());
				}
			}
			else if (_challengeUi)
			{
				HideChallengeUi();
			}
		}
		catch
		{
		}
	}

	private async Task<bool> IsChallengeAsync()
	{
		WebView2? wv = _wv;
		if (((wv != null) ? wv.CoreWebView2 : null) == null)
		{
			return false;
		}
		return (await _wv.CoreWebView2.ExecuteScriptAsync("(function(){\n  const t = (document.title||'').toLowerCase();\n  const b = (document.body && (document.body.innerText||'')) || '';\n  const low = b.toLowerCase();\n  if (t.includes('ddos') || t.includes('guard')) return true;\n  if (low.includes('checking your browser')) return true;\n  if (low.includes('ddos-guard')) return true;\n  if (low.includes('could not verify')) return true;\n  if (low.includes('manual check')) return true;\n  if (low.includes('looks too much like a bot')) return true;\n  if (low.includes('complete the manual')) return true;\n  if (document.querySelector('#ddg-iframe, iframe[src*=\"ddos\"], .ddos-guard, .cf-challenge, #challenge-form')) return true;\n  return false;\n})()")).Trim() == "true";
	}

	private async Task<string> GetWsStateAsync()
	{
		WebView2? wv = _wv;
		if (((wv != null) ? wv.CoreWebView2 : null) == null)
		{
			return "dead";
		}
		return JsonSerializer.Deserialize<string>(await _wv.CoreWebView2.ExecuteScriptAsync("(function(){try{return window.__mdhState||'dead';}catch(e){return 'dead';}})()")) ?? "dead";
	}

	private async Task ProbeAndReconnectAsync()
	{
		if (_stopping || _challengeUi)
		{
			return;
		}
		WebView2? wv = _wv;
		if (((wv != null) ? wv.CoreWebView2 : null) == null)
		{
			return;
		}
		try
		{
			if (await IsChallengeAsync())
			{
				ShowChallengeUi("Снова капча — пройди проверку в окне.");
				return;
			}
			string text = await GetWsStateAsync();
			if ((!(text == "open") && !(text == "connecting")) || 1 == 0)
			{
				_onLog("WS watchdog: reconnect");
				await _wv.CoreWebView2.ExecuteScriptAsync(BridgeScript());
			}
		}
		catch
		{
			ScheduleReload(2500);
		}
	}

	private void OnWebMessage(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
	{
		string text;
		try
		{
			text = e.TryGetWebMessageAsString();
		}
		catch
		{
			return;
		}
		if (string.IsNullOrWhiteSpace(text))
		{
			return;
		}
		try
		{
			using JsonDocument jsonDocument = JsonDocument.Parse(text);
			JsonElement rootElement = jsonDocument.RootElement;
			JsonElement value;
			switch (rootElement.TryGetProperty("t", out value) ? value.GetString() : null)
			{
			case "open":
				_wsLive = true;
				_navTries = 0;
				HideChallengeUi();
				_onStatus("WS live (Chromium)");
				_onLog("Подключено к wss://musor.best/ws/ [Chromium]");
				break;
			case "close":
				_wsLive = false;
				_onStatus("WS closed — reconnect…");
				break;
			case "error":
				_onStatus("WS error — wait…");
				break;
			case "challenge":
				ShowChallengeUi("DDoS-Guard / капча — пройди проверку в этом окне.");
				break;
			case "log":
			{
				if (rootElement.TryGetProperty("m", out var value3))
				{
					string text2 = value3.GetString();
					if (text2 != null && text2.Length > 0)
					{
						_onLog(text2);
					}
				}
				break;
			}
			case "msg":
			{
				if (rootElement.TryGetProperty("d", out var value2))
				{
					HandleWsPayload(value2.GetString() ?? "");
				}
				break;
			}
			}
		}
		catch
		{
		}
	}

	private void HandleWsPayload(string raw)
	{
		if (string.IsNullOrWhiteSpace(raw))
		{
			return;
		}
		try
		{
			using JsonDocument jsonDocument = JsonDocument.Parse(raw);
			JsonElement rootElement = jsonDocument.RootElement;
			if (rootElement.TryGetProperty("event", out var value) && value.GetString() == "new_live_skin" && rootElement.TryGetProperty("skin", out var value2) && value2.ValueKind == JsonValueKind.Object)
			{
				_onDrop(Drop.FromSkin(value2));
			}
		}
		catch
		{
		}
	}

	private void ScheduleReload(int ms)
	{
		if (_stopping || _host == null || _challengeUi)
		{
			return;
		}
		Timer t = new Timer
		{
			Interval = Math.Max(800, ms)
		};
		t.Tick += delegate
		{
			t.Stop();
			t.Dispose();
			if (!_stopping && !_challengeUi)
			{
				NavigateHome();
			}
		};
		t.Start();
	}

	private void DisposeUi()
	{
		try
		{
			_watchdog?.Dispose();
		}
		catch
		{
		}
		try
		{
			_challengePoll?.Dispose();
		}
		catch
		{
		}
		_watchdog = null;
		_challengePoll = null;
		try
		{
			if (_wv != null)
			{
				try
				{
					CoreWebView2 coreWebView = _wv.CoreWebView2;
					if (coreWebView != null)
					{
						coreWebView.Stop();
					}
				}
				catch
				{
				}
				((Component)(object)_wv).Dispose();
			}
		}
		catch
		{
		}
		_wv = null;
		try
		{
			_challengeBar?.Dispose();
		}
		catch
		{
		}
		_challengeBar = null;
		_challengeLbl = null;
	}

	private static string BridgeScript()
	{
		return "(() => {\n  const post = (o) => { try { chrome.webview.postMessage(JSON.stringify(o)); } catch (e) {} };\n  if (window.__mdhWs) { try { window.__mdhWs.close(); } catch (e) {} window.__mdhWs = null; }\n  window.__mdhState = 'boot';\n\n  const challengeLike = () => {\n    const t = (document.title || '').toLowerCase();\n    const b = (document.body && (document.body.innerText || '')) || '';\n    const low = b.toLowerCase();\n    return t.includes('ddos') || t.includes('guard')\n      || low.includes('checking your browser') || low.includes('ddos-guard')\n      || low.includes('could not verify') || low.includes('manual check')\n      || low.includes('looks too much like a bot') || low.includes('complete the manual')\n      || !!document.querySelector('#ddg-iframe, iframe[src*=\"ddos\"], .ddos-guard, .cf-challenge, #challenge-form');\n  };\n\n  const connect = () => {\n    if (window.__mdhState === 'open' || window.__mdhState === 'connecting') return;\n    if (challengeLike()) { post({ t: 'challenge' }); setTimeout(arm, 800); return; }\n    window.__mdhState = 'connecting';\n    try {\n      const ws = new WebSocket('wss://musor.best/ws/');\n      window.__mdhWs = ws;\n      ws.onopen = () => { window.__mdhState = 'open'; post({ t: 'open' }); };\n      ws.onerror = () => { post({ t: 'error' }); };\n      ws.onclose = () => {\n        window.__mdhState = 'closed';\n        window.__mdhWs = null;\n        post({ t: 'close' });\n        setTimeout(connect, 2200);\n      };\n      ws.onmessage = (ev) => {\n        try { post({ t: 'msg', d: typeof ev.data === 'string' ? ev.data : '' }); } catch (e) {}\n      };\n    } catch (e) {\n      window.__mdhState = 'dead';\n      post({ t: 'log', m: 'WS create: ' + (e && e.message ? e.message : e) });\n      setTimeout(connect, 3000);\n    }\n  };\n\n  const arm = () => {\n    if (challengeLike()) {\n      post({ t: 'challenge' });\n      setTimeout(arm, 800);\n      return;\n    }\n    connect();\n  };\n\n  if (document.readyState === 'complete') setTimeout(arm, 400);\n  else window.addEventListener('load', () => setTimeout(arm, 400));\n})();";
	}
}
