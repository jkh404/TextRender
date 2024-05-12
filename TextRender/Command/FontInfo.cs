using System;
using System.Collections;

namespace TextRender.Command
{
    public   class FontInfo :IEquatable<FontInfo>
    {
        /// <summary>
        /// 获取或设置字体的家族名称。
        /// </summary>
        public string FamilyName { get; set; }

        /// <summary>
        /// 获取或设置字体的大小。
        /// </summary>
        public float Size { get; set; } = 12;

        /// <summary>
        /// 获取或设置字体在X轴上的倾斜度。
        /// </summary>
        public float SkewX { get; set; }

        ///// <summary>
        ///// 获取或设置字体的字符间距。
        ///// </summary>
        public float Spacing { get; set; }

        /// <summary>
        /// 获取或设置字体在X轴上的缩放。
        /// </summary>
        public float ScaleX { get; set; } = 1;

        ///// <summary>
        ///// 获取或设置字体的权重。
        ///// </summary>
        //public int FontWeight { get; set; }

        /// <summary>
        /// 获取或设置文本的对齐方式。
        /// </summary>
        public TextAlign Align { get; set; }

        /// <summary>
        /// 获取或设置字体的颜色。
        /// </summary>
        public uint Color { get; set; }

        /// <summary>
        /// 获取或设置一个值，该值指示是否绘制描边还是填充。
        /// </summary>
        public bool IsStroke { get; set; }

        /// <summary>
        /// 获取或设置一个值，该值指示是否启用抗锯齿。
        /// </summary>
        public bool IsAntialias { get; set; }

        /// <summary>
        /// 获取或设置一个值，该值指示文本是否为线性。
        /// </summary>
        public bool IsLinearText { get; set; }

        /// <summary>
        /// 获取或设置一个值，该值指示是否启用自动提示。
        /// </summary>
        public bool IsAutohinted { get; set; }
        /// <summary>
        /// 粗体
        /// </summary>
        public bool Embolden { get; set; }
        /// <summary>
        /// 样式
        /// </summary>
        public FontStyle FontStyle { get; set; }

        public override bool Equals(object? obj)
        {

            return obj?.GetHashCode() == GetHashCode();
        }
        public bool Equals(FontInfo? other)
        {
            return other?.GetHashCode() == GetHashCode();
        }
        public override int GetHashCode()
        {
            HashCode hash = new HashCode();
            hash.Add(FamilyName);
            hash.Add(Size);
            hash.Add(SkewX);
            hash.Add(Spacing);
            hash.Add(ScaleX);
            hash.Add(Align);
            hash.Add(Color);
            hash.Add(IsStroke);
            hash.Add(IsAntialias);
            hash.Add(IsLinearText);
            hash.Add(IsAutohinted);
            hash.Add(Embolden);
            hash.Add(FontStyle);
            return hash.ToHashCode();
        }
        
    }
}
