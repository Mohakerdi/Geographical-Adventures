using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class RebindUI : MonoBehaviour
{
	public event System.Action<RebindUI> onRebindRequested;

	[HideInInspector] public int bindingIndex;
	public InputActionReference inputActionReference;

	[Header("UI fields")]
	[SerializeField]
	Button rebindButton;
	[SerializeField]
	TMP_Text rebindText;
	[SerializeField]
	Button resetButton;

	void Start()
	{
		rebindButton.onClick.AddListener(RequestRebind);
		resetButton.onClick.AddListener(ResetBinding);
		UpdateUI();
	}

	void OnDisable()
	{
		if (RebindManager.Instance != null)
		{
			RebindManager.Instance.Cancel();
		}
	}

	void RequestRebind()
	{
		rebindText.text = GeoGame.Localization.LocalizationManager.IsRightToLeftWritingSystem
			? GeoGame.Localization.Arabic.ArabicFixer.Fix("اضغط على أي مفتاح")
			: "press any key";
		rebindText.isRightToLeftText = false;
		onRebindRequested?.Invoke(this);
	}


	public void UpdateUI()
	{

		rebindText.text = action.GetBindingDisplayString(bindingIndex);

	}

	void ResetBinding()
	{
		action.RemoveBindingOverride(bindingIndex);
		UpdateUI();
	}

	string actionName
	{
		get
		{
			return inputActionReference.action.name;
		}
	}

	public InputAction action
	{
		get
		{
			return inputActionReference.action;
		}
	}
}
