using UnityEngine;
using UnityEngine.UI;

namespace Convai.Scripts.Runtime.Extensions
{
    public static class ImageExtensions
    {
        public static Image WithColorValue(this Image image, float? r = null, float? g = null, float? b = null, float? a = null)
        {
            image.color = new Color(r ?? image.color.r, g ?? image.color.g, b ?? image.color.b, a ?? image.color.a);
            return image;
        }
    }
}