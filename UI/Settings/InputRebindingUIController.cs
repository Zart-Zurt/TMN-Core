using System;
using Core.Events;
using Core.Inputs;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Core.UI.Settings
{
    [AddComponentMenu("Core/UI/Input Rebinding UI Controller")]
    [TypeInfoBox("Manages the input rebinding interface. Displays prompt overlays during rebinding, remaps action bindings at runtime, and synchronizes display text across action rows.")]
    public class InputRebindingUIController : MonoBehaviour
    {
        [SerializeField] private GameObject rebindOverlay;
        [SerializeField] private TMP_Text overlayPromptText;
        [SerializeField] private Button resetAllButton;
        [SerializeField] private RebindActionRow[] actionRows;

        public static bool IsAnyRebindingActive { get; private set; }

        private void OnEnable()
        {
            EventBus.Subscribe<InputBindingChangedEvent>(OnInputBindingChanged);

            if (resetAllButton != null)
            {
                resetAllButton.onClick.AddListener(OnResetAllClicked);
            }

            HookRowListeners();
            RefreshAllBindingDisplays();

            if (rebindOverlay != null)
            {
                rebindOverlay.SetActive(false);
            }

            IsAnyRebindingActive = false;
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<InputBindingChangedEvent>(OnInputBindingChanged);

            if (resetAllButton != null)
            {
                resetAllButton.onClick.RemoveListener(OnResetAllClicked);
            }

            UnhookRowListeners();

            if (rebindOverlay != null)
            {
                rebindOverlay.SetActive(false);
            }

            IsAnyRebindingActive = false;
        }

        public void StartRebind(string actionName, int bindingIndex, TMP_Text targetDisplay)
        {
            if (rebindOverlay != null)
            {
                rebindOverlay.SetActive(true);
            }

            IsAnyRebindingActive = true;

            if (overlayPromptText != null)
            {
                overlayPromptText.text = $"Press a key for '{actionName}'... (ESC to cancel)";
            }

            if (InputManager.Instance == null)
            {
                if (rebindOverlay != null)
                {
                    rebindOverlay.SetActive(false);
                }
                IsAnyRebindingActive = false;
                return;
            }

            InputManager.Instance.StartRebind(
                actionName,
                bindingIndex,
                onComplete: () =>
                {
                    if (rebindOverlay != null)
                    {
                        rebindOverlay.SetActive(false);
                    }

                    IsAnyRebindingActive = false;

                    if (targetDisplay != null)
                    {
                        targetDisplay.text = InputManager.Instance.GetBindingDisplayString(actionName, bindingIndex);
                    }
                },
                onCancel: () =>
                {
                    if (rebindOverlay != null)
                    {
                        rebindOverlay.SetActive(false);
                    }

                    IsAnyRebindingActive = false;
                }
            );
        }

        private void OnResetAllClicked()
        {
            if (InputManager.Instance != null)
            {
                InputManager.Instance.ResetAllBindings();
            }

            RefreshAllBindingDisplays();
        }

        private void OnInputBindingChanged(InputBindingChangedEvent evt)
        {
            if (actionRows == null)
            {
                return;
            }

            for (var i = 0; i < actionRows.Length; i++)
            {
                var row = actionRows[i];
                if (row.ActionName == evt.ActionName && row.BindingDisplayText != null)
                {
                    row.BindingDisplayText.text = evt.DisplayString;
                }
            }
        }

        private void RefreshAllBindingDisplays()
        {
            if (actionRows == null || InputManager.Instance == null)
            {
                return;
            }

            for (var i = 0; i < actionRows.Length; i++)
            {
                var row = actionRows[i];
                if (row.BindingDisplayText != null)
                {
                    row.BindingDisplayText.text = InputManager.Instance.GetBindingDisplayString(row.ActionName, row.BindingIndex);
                }
            }
        }

        private void HookRowListeners()
        {
            if (actionRows == null)
            {
                return;
            }

            for (var i = 0; i < actionRows.Length; i++)
            {
                var row = actionRows[i];
                if (row.RebindButton != null)
                {
                    var actionName = row.ActionName;
                    var bindingIndex = row.BindingIndex;
                    var targetDisplay = row.BindingDisplayText;

                    row.RebindButton.onClick.RemoveAllListeners();
                    row.RebindButton.onClick.AddListener(() => StartRebind(actionName, bindingIndex, targetDisplay));
                }
            }
        }

        private void UnhookRowListeners()
        {
            if (actionRows == null)
            {
                return;
            }

            for (var i = 0; i < actionRows.Length; i++)
            {
                var row = actionRows[i];
                if (row.RebindButton != null)
                {
                    row.RebindButton.onClick.RemoveAllListeners();
                }
            }
        }
    }
}
