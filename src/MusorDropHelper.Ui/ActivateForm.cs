using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using MusorDropHelper.Core;

namespace MusorDropHelper.Ui;

internal sealed class ActivateForm : Form
{
	private readonly TextBox _entry = new TextBox();

	private readonly Label _err = new Label();

	private bool _busy;

	public ActivateForm()
	{
		Text = "Активация — Musor Drop Helper";
		base.FormBorderStyle = FormBorderStyle.FixedDialog;
		base.MaximizeBox = false;
		base.MinimizeBox = false;
		base.StartPosition = FormStartPosition.CenterScreen;
		BackColor = Color.FromArgb(11, 10, 18);
		base.ClientSize = new Size(420, 210);
		TrySetIcon();
		Label label = new Label
		{
			Text = "MUSOR DROP HELPER",
			ForeColor = Color.FromArgb(181, 107, 255),
			Font = new Font("Segoe UI", 14f, FontStyle.Bold),
			AutoSize = true,
			Location = new Point(22, 18)
		};
		Label label2 = new Label
		{
			Text = "Введите ключ",
			ForeColor = Color.FromArgb(154, 144, 181),
			Font = new Font("Segoe UI", 9f),
			AutoSize = true,
			Location = new Point(22, 52)
		};
		_entry.Font = new Font("Consolas", 12f);
		_entry.BackColor = Color.FromArgb(20, 18, 28);
		_entry.ForeColor = Color.FromArgb(240, 233, 255);
		_entry.BorderStyle = BorderStyle.FixedSingle;
		_entry.Location = new Point(22, 80);
		_entry.Width = 376;
		_entry.KeyDown += delegate(object? _, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Return)
			{
				e.SuppressKeyPress = true;
				Submit();
			}
		};
		_err.ForeColor = Color.FromArgb(255, 107, 154);
		_err.BackColor = BackColor;
		_err.Font = new Font("Segoe UI", 9f);
		_err.AutoSize = false;
		_err.Size = new Size(376, 36);
		_err.Location = new Point(22, 112);
		Button button = new Button
		{
			Text = "Активировать",
			BackColor = Color.FromArgb(181, 107, 255),
			ForeColor = Color.FromArgb(18, 8, 24),
			FlatStyle = FlatStyle.Flat,
			Size = new Size(130, 32),
			Location = new Point(22, 158),
			Font = new Font("Segoe UI", 9f, FontStyle.Bold)
		};
		button.FlatAppearance.BorderSize = 0;
		button.Click += delegate
		{
			Submit();
		};
		Button button2 = new Button
		{
			Text = "Выход",
			BackColor = Color.FromArgb(27, 24, 38),
			ForeColor = Color.FromArgb(240, 233, 255),
			FlatStyle = FlatStyle.Flat,
			Size = new Size(90, 32),
			Location = new Point(160, 158),
			DialogResult = DialogResult.Cancel
		};
		button2.FlatAppearance.BorderSize = 0;
		base.Controls.AddRange(new Control[6] { label, label2, _entry, _err, button, button2 });
		base.AcceptButton = button;
		base.CancelButton = button2;
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

	private void Submit()
	{
		if (!_busy)
		{
			_busy = true;
			_err.Text = "Проверка…";
			Application.DoEvents();
			(bool ok, string msg) tuple = LicenseClient.Activate(_entry.Text);
			bool item = tuple.ok;
			string item2 = tuple.msg;
			_busy = false;
			if (item)
			{
				base.DialogResult = DialogResult.OK;
				Close();
			}
			else
			{
				_err.Text = item2;
			}
		}
	}
}
