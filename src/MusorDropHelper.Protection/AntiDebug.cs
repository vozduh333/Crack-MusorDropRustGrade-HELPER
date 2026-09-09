namespace MusorDropHelper.Protection;

// [patched] Анти-отладка полностью удалена (были IsDebuggerPresent /
// CheckRemoteDebuggerPresent / Environment.Exit — их флагали AV-эвристики).
internal static class AntiDebug
{
	public static void GuardOrExit()
	{
	}
}
