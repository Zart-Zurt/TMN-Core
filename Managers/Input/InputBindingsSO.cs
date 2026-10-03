using System.Collections.Generic;
using UnityEngine;

namespace Core.Inputs
{
    [CreateAssetMenu(fileName = "InputBindings", menuName = "Core/Input/Input Bindings")]
    public class InputBindingsSO : ScriptableObject
    {
        [field: SerializeField] public List<string> DefaultBindings { get; private set; } = new();
    }
}
