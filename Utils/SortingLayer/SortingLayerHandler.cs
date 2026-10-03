using System;
using System.Collections.Generic;
using System.Linq;
using TMNLibrary.Singleton;
using UnityEngine;

namespace Core.Utils
{
    public class SortingLayerHandler : MonoSingleton<SortingLayerHandler>
    {
        public void ChangeOrderInLayerByValue(List<SpriteRenderer> spriteRenderers, int value = 0)
        {
            foreach (var renderer in spriteRenderers)
            {
                renderer.sortingOrder += value;
            }
        }
    }
}
