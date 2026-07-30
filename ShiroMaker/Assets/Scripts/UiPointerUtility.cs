using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public static class UiPointerUtility
{
    private static readonly List<RaycastResult> RaycastResults = new List<RaycastResult>();

    /// <summary>
    /// PointerEventData の位置に UI があるか判定
    /// </summary>
    public static bool IsOverUi(PointerEventData eventData)
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        RaycastResults.Clear();
        EventSystem.current.RaycastAll(eventData, RaycastResults);
        return RaycastResults.Count > 0;
    }

    /// <summary>
    /// 画面座標の位置に UI があるか判定
    /// </summary>
    public static bool IsOverUi(Vector2 screenPosition)
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            position = screenPosition
        };

        return IsOverUi(eventData);
    }
}
