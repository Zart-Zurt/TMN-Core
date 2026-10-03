using System;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Core.UI.Settings
{
    [Serializable]
    [TypeInfoBox("Defines an input action binding entry, pairing an action name and binding index with its trigger button and display text.")]
    public struct RebindActionRow
    {
        [SerializeField] private string actionName;
        [SerializeField] private int bindingIndex;
        [SerializeField] private Button rebindButton;
        [SerializeField] private TMP_Text bindingDisplayText;

        public string ActionName => actionName;
        public int BindingIndex => bindingIndex;
        public Button RebindButton => rebindButton;
        public TMP_Text BindingDisplayText => bindingDisplayText;
    }
}
