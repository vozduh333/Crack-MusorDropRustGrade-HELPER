using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using MusorDropHelper.Core;

namespace MusorDropHelper.Ui;

internal sealed class MainForm : Form
{
	private const int MaxLogLines = 140;

	private readonly Analyzer _analyzer;

	private readonly ConcurrentQueue<(string kind, object payload)> _q = new ConcurrentQueue<(string, object)>();

	private readonly ConcurrentDictionary<string, Image> _iconCache = new ConcurrentDictionary<string, Image>();

	private readonly LinkedList<Drop> _display = new LinkedList<Drop>();

	private readonly HttpClient _http = new HttpClient
	{
		Timeout = TimeSpan.FromSeconds(3.0)
	};

	private readonly HashSet<string> _pendingIcons = new HashSet<string>();

	private readonly object _iconLock = new object();

	private readonly FeedStripPanel _feed;

	private readonly Label _feedMeta;

	private readonly RichTextBox _log;

	private readonly Label _status;

	private readonly Label _license;

	private readonly Label _score;

	private readonly Label _level;

	private readonly Label _title;

	private readonly Label _detail;

	private readonly Label _stats;

	private readonly Panel _sigFrame;

	private readonly ScoreBarPanel _bar;

	private readonly Timer _drainTimer;

	private readonly Timer _licTimer;

	public MainForm(Analyzer analyzer)
	{
		_analyzer = analyzer;
		Text = "Musor Drop Helper";
		BackColor = UiTheme.Bg;
		base.ClientSize = new Size(1200, 760);
		MinimumSize = new Size(940, 640);
		base.StartPosition = FormStartPosition.CenterScreen;
		TrySetIcon();
		_http.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/128.0.0.0 Safari/537.36");
		_http.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "image/*,*/*;q=0.8");
		Panel top = new Panel
		{
			Dock = DockStyle.Top,
			Height = 44,
			BackColor = UiTheme.Bg
		};
		Label brandMusor = new Label
		{
			Text = "MUSOR",
			Font = new Font("Segoe UI", 22f, FontStyle.Bold),
			ForeColor = UiTheme.Accent,
			AutoSize = true,
			Location = new Point(18, 6),
			BackColor = UiTheme.Bg
		};
		Label brandDrop = new Label
		{
			Text = " DROP HELPER",
			Font = new Font("Segoe UI", 22f, FontStyle.Bold),
			ForeColor = UiTheme.Text,
			AutoSize = true,
			Location = new Point(brandMusor.Right - 4, 6),
			BackColor = UiTheme.Bg
		};
		brandMusor.SizeChanged += delegate
		{
			brandDrop.Left = brandMusor.Right - 2;
		};
		_status = new Label
		{
			ForeColor = UiTheme.Cyan,
			AutoSize = true,
			BackColor = UiTheme.Bg,
			Font = new Font("Segoe UI", 9f)
		};
		_license = new Label
		{
			Text = LicenseClient.StatusText(),
			ForeColor = UiTheme.Accent,
			AutoSize = true,
			BackColor = UiTheme.Bg,
			Font = new Font("Segoe UI", 9f)
		};
		top.Controls.Add(brandMusor);
		top.Controls.Add(brandDrop);
		top.Controls.Add(_license);
		top.Controls.Add(_status);
		top.Resize += delegate
		{
			_status.Location = new Point(Math.Max(400, top.Width - _status.PreferredWidth - 18), 14);
			_license.Location = new Point(Math.Max(300, _status.Left - _license.PreferredWidth - 16), 14);
		};
		FlowLayoutPanel flowLayoutPanel = new FlowLayoutPanel
		{
			Dock = DockStyle.Top,
			Height = 28,
			BackColor = UiTheme.Bg,
			Padding = new Padding(18, 2, 18, 4),
			WrapContents = false
		};
		(double, Color, Color, Color, string, string)[] priceTiers = UiTheme.PriceTiers;
		for (int num = 0; num < priceTiers.Length; num++)
		{
			(double, Color, Color, Color, string, string) tuple = priceTiers[num];
			Label value = new Label
			{
				Text = " " + tuple.Item6 + " ",
				ForeColor = tuple.Item4,
				BackColor = tuple.Item2,
				Font = new Font("Segoe UI", 8f, FontStyle.Bold),
				AutoSize = true,
				Margin = new Padding(3, 2, 3, 2),
				BorderStyle = BorderStyle.FixedSingle
			};
			flowLayoutPanel.Controls.Add(value);
		}
		Panel panel = new Panel
		{
			Dock = DockStyle.Fill,
			BackColor = UiTheme.Bg,
			Padding = new Padding(18, 4, 18, 12)
		};
		Panel right = new Panel
		{
			Dock = DockStyle.Right,
			Width = 310,
			BackColor = UiTheme.Panel,
			Padding = new Padding(0)
		};
		right.Paint += delegate(object? _, PaintEventArgs e)
		{
			using Pen pen = new Pen(UiTheme.Line);
			e.Graphics.DrawRectangle(pen, 0, 0, right.Width - 1, right.Height - 1);
		};
		(Color, Color, Color) tuple2 = UiTheme.LevelTheme["COLD"];
		_sigFrame = new Panel
		{
			Location = new Point(12, 12),
			Size = new Size(286, 210),
			BackColor = tuple2.Item1,
			Padding = new Padding(14)
		};
		_sigFrame.Paint += delegate(object? _, PaintEventArgs e)
		{
			using Pen pen = new Pen(UiTheme.Line);
			e.Graphics.DrawRectangle(pen, 0, 0, _sigFrame.Width - 1, _sigFrame.Height - 1);
		};
		Label label = new Label
		{
			Text = "SCORE",
			ForeColor = UiTheme.Dim,
			Font = new Font("Segoe UI", 9f, FontStyle.Bold),
			AutoSize = true,
			Location = new Point(14, 14),
			BackColor = tuple2.Item1
		};
		_score = new Label
		{
			Text = "—",
			ForeColor = tuple2.Item2,
			Font = new Font("Segoe UI", 42f, FontStyle.Bold),
			AutoSize = true,
			Location = new Point(14, 34),
			BackColor = tuple2.Item1
		};
		_level = new Label
		{
			Text = "COLD",
			ForeColor = tuple2.Item2,
			Font = new Font("Segoe UI", 16f, FontStyle.Bold),
			AutoSize = true,
			Location = new Point(14, 100),
			BackColor = tuple2.Item1
		};
		_bar = new ScoreBarPanel
		{
			Location = new Point(14, 138),
			Size = new Size(258, 10),
			BackColor = tuple2.Item1
		};
		_title = new Label
		{
			Text = "Ждём ленту",
			ForeColor = UiTheme.Muted,
			Font = new Font("Segoe UI", 10f),
			AutoSize = false,
			Size = new Size(258, 40),
			Location = new Point(14, 154),
			BackColor = tuple2.Item1
		};
		_sigFrame.Controls.AddRange(new Control[5] { label, _score, _level, _bar, _title });
		_detail = new Label
		{
			ForeColor = UiTheme.Muted,
			Font = new Font("Segoe UI", 9f),
			AutoSize = false,
			Size = new Size(270, 70),
			Location = new Point(14, 230),
			BackColor = UiTheme.Panel
		};
		_stats = new Label
		{
			ForeColor = UiTheme.Dim,
			Font = new Font("Consolas", 8f),
			AutoSize = false,
			Size = new Size(270, 80),
			Location = new Point(14, 310),
			BackColor = UiTheme.Panel
		};
		right.Controls.AddRange(new Control[3] { _sigFrame, _detail, _stats });
		Panel panel2 = new Panel
		{
			Dock = DockStyle.Fill,
			BackColor = UiTheme.Bg,
			Padding = new Padding(0, 0, 12, 0)
		};
		Panel feedWrap = new Panel
		{
			Dock = DockStyle.Top,
			Height = 220,
			BackColor = UiTheme.Panel
		};
		feedWrap.Paint += delegate(object? _, PaintEventArgs e)
		{
			using Pen pen = new Pen(UiTheme.Line);
			e.Graphics.DrawRectangle(pen, 0, 0, feedWrap.Width - 1, feedWrap.Height - 1);
		};
		Panel panel3 = new Panel
		{
			Dock = DockStyle.Top,
			Height = 28,
			BackColor = UiTheme.Panel,
			Padding = new Padding(12, 8, 12, 0)
		};
		Label value2 = new Label
		{
			Text = "LIVE DROPS",
			ForeColor = UiTheme.Dim,
			Font = new Font("Segoe UI", 9f, FontStyle.Bold),
			AutoSize = true,
			Dock = DockStyle.Left,
			BackColor = UiTheme.Panel
		};
		_feedMeta = new Label
		{
			Text = "",
			ForeColor = UiTheme.Dim,
			Font = new Font("Segoe UI", 8f),
			AutoSize = true,
			Dock = DockStyle.Right,
			BackColor = UiTheme.Panel
		};
		panel3.Controls.Add(_feedMeta);
		panel3.Controls.Add(value2);
		_feed = new FeedStripPanel(_iconCache)
		{
			Dock = DockStyle.Fill
		};
		feedWrap.Controls.Add(_feed);
		feedWrap.Controls.Add(panel3);
		Label value3 = new Label
		{
			Text = "LOG",
			ForeColor = UiTheme.Dim,
			Font = new Font("Segoe UI", 9f, FontStyle.Bold),
			Dock = DockStyle.Top,
			Height = 22,
			BackColor = UiTheme.Bg
		};
		Panel logWrap = new Panel
		{
			Dock = DockStyle.Fill,
			BackColor = UiTheme.Panel,
			Padding = new Padding(1)
		};
		logWrap.Paint += delegate(object? _, PaintEventArgs e)
		{
			using Pen pen = new Pen(UiTheme.Line);
			e.Graphics.DrawRectangle(pen, 0, 0, logWrap.Width - 1, logWrap.Height - 1);
		};
		_log = new RichTextBox
		{
			Dock = DockStyle.Fill,
			ReadOnly = true,
			BorderStyle = BorderStyle.None,
			BackColor = UiTheme.Panel,
			ForeColor = UiTheme.Muted,
			Font = new Font("Consolas", 9f),
			DetectUrls = false,
			ScrollBars = RichTextBoxScrollBars.Vertical
		};
		logWrap.Controls.Add(_log);
		panel2.Controls.Add(logWrap);
		panel2.Controls.Add(value3);
		panel2.Controls.Add(feedWrap);
		panel.Controls.Add(panel2);
		panel.Controls.Add(right);
		base.Controls.Add(panel);
		base.Controls.Add(flowLayoutPanel);
		base.Controls.Add(top);
		_drainTimer = new Timer
		{
			Interval = 35
		};
		_drainTimer.Tick += delegate
		{
			Drain();
		};
		_drainTimer.Start();
		_licTimer = new Timer
		{
			Interval = 30000
		};
		_licTimer.Tick += delegate
		{
			try
			{
				_license.Text = LicenseClient.StatusText();
				top.PerformLayout();
			}
			catch
			{
			}
		};
		_licTimer.Start();
		base.FormClosed += delegate
		{
			_drainTimer.Stop();
			_licTimer.Stop();
			_http.Dispose();
		};
		base.Shown += delegate
		{
			top.PerformLayout();
		};
	}

	private void TrySetIcon()
	{
		try
		{
			string text = Path.Combine(AppContext.BaseDirectory, "favicon.ico");
			if (File.Exists(text))
			{
				base.Icon = new Icon(text);
			}
		}
		catch
		{
		}
	}

	public void EnqueueDrop(Drop drop)
	{
		_q.Enqueue(("drop", drop));
	}

	public void EnqueueStatus(string msg)
	{
		_q.Enqueue(("status", msg));
	}

	public void EnqueueLog(string msg)
	{
		_q.Enqueue(("log", msg));
	}

	private void Drain()
	{
		List<Drop> list = new List<Drop>();
		(string, object) result;
		while (_q.TryDequeue(out result))
		{
			if (result.Item1 == "drop" && result.Item2 is Drop item)
			{
				list.Add(item);
			}
			else if (result.Item1 == "status" && result.Item2 is string text)
			{
				_status.Text = ((text.Length > 100) ? text.Substring(0, 100) : text);
			}
			else if (result.Item1 == "log" && result.Item2 is string msg)
			{
				AppendPlainLog(msg);
			}
		}
		if (list.Count == 0)
		{
			return;
		}
		Signal signal = _analyzer.PushMany(list);
		foreach (Drop item2 in list)
		{
			_display.AddFirst(item2);
			EnsureIcon(item2.IconUrl);
		}
		while (_display.Count > 14)
		{
			_display.RemoveLast();
		}
		_feed.SetDrops(_display);
		if (signal != null)
		{
			ApplySignal(signal);
			foreach (Drop item3 in list)
			{
				AppendDropLog(item3, signal);
			}
		}
		_feedMeta.Text = $"слотов {_feed.VisibleSlots} · +{list.Count} · кэш иконок live";
	}

	private void EnsureIcon(string url)
	{
		if (string.IsNullOrEmpty(url) || _iconCache.ContainsKey(url))
		{
			return;
		}
		lock (_iconLock)
		{
			if (!_pendingIcons.Add(url))
			{
				return;
			}
		}
		Task.Run(async delegate
		{
			_ = 1;
			try
			{
				string requestUri = FastIconUrl(url);
				await using MemoryStream stream = new MemoryStream(await _http.GetByteArrayAsync(requestUri));
				using Image src = Image.FromStream(stream);
				Image thumb = PrepIcon(src, 64);
				if (base.IsHandleCreated && !base.IsDisposed)
				{
					BeginInvoke(delegate
					{
						_iconCache[url] = thumb;
						_feed.Invalidate();
					});
				}
				else
				{
					thumb.Dispose();
				}
			}
			catch
			{
			}
			finally
			{
				lock (_iconLock)
				{
					_pendingIcons.Remove(url);
				}
			}
		});
	}

	private static Image PrepIcon(Image src, int size)
	{
		Bitmap bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb);
		using Graphics graphics = Graphics.FromImage(bitmap);
		graphics.Clear(Color.Transparent);
		graphics.InterpolationMode = InterpolationMode.HighQualityBilinear;
		float num = Math.Min((float)size / (float)src.Width, (float)size / (float)src.Height);
		int num2 = (int)((float)src.Width * num);
		int num3 = (int)((float)src.Height * num);
		graphics.DrawImage(src, (size - num2) / 2, (size - num3) / 2, num2, num3);
		return bitmap;
	}

	private static string FastIconUrl(string url)
	{
		try
		{
			Uri uri = new Uri(url);
			if (uri.AbsolutePath.Contains("/economy/image/", StringComparison.Ordinal))
			{
				string text = uri.AbsolutePath;
				int num = text.LastIndexOf('/');
				if (num > 0)
				{
					text = text.Substring(0, num) + "/96fx96f";
				}
				return new UriBuilder(uri)
				{
					Path = text,
					Query = ""
				}.Uri.ToString();
			}
		}
		catch
		{
		}
		return url;
	}

	private void ApplySignal(Signal sig)
	{
		(Color, Color, Color) valueOrDefault = UiTheme.LevelTheme.GetValueOrDefault(sig.Level, UiTheme.LevelTheme["COLD"]);
		Control[] array = new Control[5] { _sigFrame, _score, _level, _title, _bar };
		for (int i = 0; i < array.Length; i++)
		{
			array[i].BackColor = valueOrDefault.Item1;
		}
		foreach (Control control in _sigFrame.Controls)
		{
			control.BackColor = valueOrDefault.Item1;
		}
		_score.ForeColor = valueOrDefault.Item2;
		_level.ForeColor = valueOrDefault.Item2;
		_title.ForeColor = valueOrDefault.Item2;
		_score.Text = $"{sig.Score:0}";
		_level.Text = sig.Level;
		_title.Text = sig.Title;
		_detail.Text = sig.Detail;
		_bar.Set(sig.Score, valueOrDefault.Item3);
		Dictionary<string, double> dictionary = _analyzer.WindowStats();
		_stats.Text = $"window n={dictionary["n"]:0}   avg ${dictionary["avg_price"]:0.00}\nmomentum ×{dictionary["momentum"]:0.00}   heat {dictionary["heat_share"] * 100.0:0}%\nтемп ~{_analyzer.DropsPerMin():0}/мин   total {_analyzer.Total}";
	}

	private void AppendDropLog(Drop drop, Signal signal)
	{
		(Color, Color, Color, string) tuple = UiTheme.TierStyle(drop.Price);
		string text = $"${drop.Price,7:0.00}  [{signal.Score,3:0}]  {((drop.ItemName.Length > 68) ? drop.ItemName.Substring(0, 68) : drop.ItemName)}\n";
		AppendColored(text, tuple.Item3);
		TrimLog();
	}

	private void AppendPlainLog(string msg)
	{
		AppendColored(msg + "\n", UiTheme.Muted);
		TrimLog();
	}

	private void AppendColored(string text, Color color)
	{
		_log.SelectionStart = _log.TextLength;
		_log.SelectionLength = 0;
		_log.SelectionColor = color;
		_log.AppendText(text);
		_log.SelectionColor = _log.ForeColor;
		_log.ScrollToCaret();
	}

	private void TrimLog()
	{
		if (_log.Lines.Length > 140)
		{
			string text = string.Join("\n", _log.Lines.Skip(_log.Lines.Length - 140));
			_log.Clear();
			_log.ForeColor = UiTheme.Muted;
			_log.AppendText(text + "\n");
		}
	}
}
