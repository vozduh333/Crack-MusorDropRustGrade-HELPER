using System.Windows.Forms;
using MusorDropHelper.Core;

namespace MusorDropHelper.Ui;

internal static class ActivationGate
{
	public static bool EnsureActivated()
	{
		if (LicenseClient.IsLicensed())
		{
			return true;
		}
		using ActivateForm activateForm = new ActivateForm();
		return activateForm.ShowDialog() == DialogResult.OK;
	}
}
