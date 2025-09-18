using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Yarn.Unity.Attributes;
using System.Text;

namespace Yarn.Unity
{
    [CreateAssetMenu(fileName = "NewPalette", menuName = "Yarn Spinner/Markup Palette", order = 102)]
    public sealed class MarkupPalette : ScriptableObject
    {
        [System.Serializable]
        public struct BasicMarker
        {
            public string Marker;
            public bool CustomColor;
            [ShowIf(nameof(CustomColor))]
            public Color Color;
            public bool Boldened;
            public bool Italicised;
            public bool Underlined;
            public bool Strikedthrough;
        }

        [System.Serializable]
        public struct CustomMarker
        {
            public string Marker;
            public string Start;
            public string End;
            public int MarkerOffset;
        }

        [UnityEngine.Serialization.FormerlySerializedAs("ColourMarkers")]
        public List<BasicMarker> BasicMarkers = new List<BasicMarker>();
        public List<CustomMarker> CustomMarkers = new List<CustomMarker>();

        public bool ColorForMarker(string Marker, out Color colour)
        {
            foreach (var item in BasicMarkers)
            {
                if (item.Marker == Marker)
                {
                    colour = item.Color;
                    return true;
                }
            }
            colour = Color.black;
            return false;
        }

        public bool PaletteForMarker(string markerName, out CustomMarker palette)
        {
            // check basic markers first
            foreach (var item in BasicMarkers)
            {
                if (item.Marker == markerName)
                {
                    StringBuilder front = new();
                    StringBuilder back = new();

                    if (item.CustomColor)
                    {
                        // Use RGB hex for fully opaque colours; use RGBA hex when alpha < 1.
                        // If alpha is accidentally zero, warn and treat it as opaque (fallback to RGB)
                        float alpha = item.Color.a;
                        string hex;

                        if (alpha <= 0.001f)
                        {
                            Debug.LogWarning($"MarkupPalette: Marker '{item.Marker}' has alpha == 0. Treating as opaque to avoid invisible text. If you want transparency, set the alpha intentionally in the palette.", this);
                            hex = ColorUtility.ToHtmlStringRGB(item.Color); // drop alpha
                        }
                        else if (alpha >= 0.999f)
                        {
                            // fully opaque -> prefer #RRGGBB (6 chars)
                            hex = ColorUtility.ToHtmlStringRGB(item.Color);
                        }
                        else
                        {
                            // semi-transparent -> include alpha (#RRGGBBAA)
                            hex = ColorUtility.ToHtmlStringRGBA(item.Color);
                        }

                        front.AppendFormat("<color=#{0}>", hex);
                        back.Append("</color>");
                    }

                    if (item.Boldened)
                    {
                        front.Append("<b>");
                        back.Append("</b>");
                    }
                    if (item.Italicised)
                    {
                        front.Append("<i>");
                        back.Append("</i>");
                    }
                    if (item.Underlined)
                    {
                        front.Append("<u>");
                        back.Append("</u>");
                    }
                    if (item.Strikedthrough)
                    {
                        front.Append("<s>");
                        back.Append("</s>");
                    }

                    palette = new CustomMarker()
                    {
                        Marker = item.Marker,
                        Start = front.ToString(),
                        End = back.ToString(),
                        MarkerOffset = 0,
                    };
                    return true;
                }
            }

            // custom markers (start/end strings) next
            foreach (var item in CustomMarkers)
            {
                if (item.Marker == markerName)
                {
                    palette = item;
                    return true;
                }
            }

            palette = new();
            return false;
        }
    }
}
