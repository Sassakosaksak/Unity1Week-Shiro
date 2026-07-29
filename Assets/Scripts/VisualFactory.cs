using UnityEngine;

namespace Shiro
{
    public static class VisualFactory
    {
        private static Sprite squareSprite;

        public static GameObject SpriteObject(string name, Color color, Vector2 size, Vector3 position, Transform parent = null, int sortingOrder = 0)
        {
            EnsureSprite();

            var instance = new GameObject(name);
            instance.transform.SetParent(parent);
            instance.transform.position = position;
            instance.transform.localScale = new Vector3(size.x, size.y, 1f);

            var renderer = instance.AddComponent<SpriteRenderer>();
            renderer.sprite = squareSprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            return instance;
        }

        public static void SetColor(GameObject target, Color color)
        {
            var renderer = target.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.color = color;
            }
        }

        private static void EnsureSprite()
        {
            if (squareSprite != null)
            {
                return;
            }

            var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.SetPixel(0, 0, Color.white);
            texture.Apply();
            squareSprite = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }
    }
}
