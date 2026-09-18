#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace GeoGame.BuildEditor
{
	/// <summary>
	/// Preprocesses Android builds to enforce IL2CPP scripting backend and
	/// universal 64-bit ARM64 + 32-bit ARMv7 architectures, ensuring maximum
	/// compatibility with both modern 64-bit phones (Pixel, Galaxy, Xiaomi)
	/// and older devices.
	/// </summary>
	public class AndroidBuildPreprocessor : IPreprocessBuildWithReport
	{
		public int callbackOrder => 0;

		public void OnPreprocessBuild(BuildReport report)
		{
			if (report.summary.platform == BuildTarget.Android)
			{
				Debug.Log("[AndroidBuildPreprocessor] Enforcing Android IL2CPP backend and ARM64 + ARMv7 architectures...");
				PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
				PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
				PlayerSettings.SetIl2CppCompilerConfiguration(BuildTargetGroup.Android, Il2CppCompilerConfiguration.Release);
				PlayerSettings.SetManagedStrippingLevel(BuildTargetGroup.Android, ManagedStrippingLevel.Minimal);

				Debug.Log($"[AndroidBuildPreprocessor] Configured -> Scripting Backend: {PlayerSettings.GetScriptingBackend(BuildTargetGroup.Android)}, Target Architectures: {PlayerSettings.Android.targetArchitectures}");
			}
		}
	}
}
#endif
