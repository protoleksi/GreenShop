using System;
using System.Collections.Generic;
using UnityEngine;

namespace GreenShop
{
    public enum TreeBranch
    {
        Growth = 0,   // РОСТ (зеленый регион слева)
        Comfort = 1,  // УЮТ (серый регион по центру-вверху)
        Trade = 2     // ТОРГОВЛЯ (серый регион справа-снизу)
    }

    /// <summary>
    /// Конфигурация отдельного узла на карте Дерева улучшений.
    /// Хранит идентификатор, координаты в долях карты (0..1), зависимости и стоимость.
    /// </summary>
    [Serializable]
    public class TreeNodeData
    {
        public string id;
        public string title;
        [TextArea(2, 4)] public string description;
        public int cost;
        public TreeBranch branch;
        public Vector2 mapPosition; // доли ширины и высоты видимой карты (0..1 от левого верхнего угла)
        public List<string> prerequisiteIds = new List<string>();
        public bool isPlaceholder;
        public Sprite customIcon;
    }
}
