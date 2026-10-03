using System.Linq;
using UnityEngine;

namespace Core.Utils
{
    public static class SortingLayerChanger
    {
        public static void SetOrderInLayerByNum(this SpriteRenderer spriteRenderer, int order)
        {
            spriteRenderer.sortingOrder = order;
        }

        public static void IncreaseOrderInLayerByOne(this SpriteRenderer spriteRenderer)
        {
            spriteRenderer.sortingOrder++;
        }

        public static void IncreaseOrderInLayer(this SpriteRenderer spriteRenderer, int order)
        {
            spriteRenderer.sortingOrder += order;
        }

        public static void SetSortingLayerByIndex(this SpriteRenderer spriteRenderer, int index)
        {
            spriteRenderer.sortingLayerID = SortingLayer.layers[index].id;
        }

        public static void SetSortingLayerByName(this SpriteRenderer spriteRenderer, string layerName)
        {
            spriteRenderer.sortingLayerID = SortingLayer.NameToID(layerName);
        }

        public static void SetSortingLayerBack(this SpriteRenderer spriteRenderer)
        {
            spriteRenderer.sortingLayerID = 0;
        }

        public static void SetSortingLayerFront(this SpriteRenderer spriteRenderer)
        {
            spriteRenderer.sortingLayerID = SortingLayer.layers.Last().id;
        }
    }
}