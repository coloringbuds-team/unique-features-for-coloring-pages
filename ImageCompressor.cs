using System;
using System.IO;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;

public class ImageCompressor
{
    /// <summary>
    /// Nén ảnh JPG bằng cách giảm chất lượng (Quality)
    /// </summary>
    /// <param name="inputPath">Đường dẫn ảnh gốc</param>
    /// <param name="outputPath">Đường dẫn lưu ảnh sau nén</param>
    /// <param name="quality">Chất lượng ảnh từ 1 - 100 (Khuyên dùng: 60-80)</param>
    public static void CompressJpeg(string inputPath, string outputPath, int quality)
    {
        using (Image image = Image.Load(inputPath))
        {
            var encoder = new JpegEncoder
            {
                Quality = quality // Giảm chỉ số này để dung lượng nhỏ hơn
            };
            
            image.Save(outputPath, encoder);
        }
    }

    /// <summary>
    /// Nén ảnh PNG bằng cách tối ưu thuật toán nén và giảm bảng màu (nếu cần)
    /// </summary>
    public static void CompressPng(string inputPath, string outputPath)
    {
        using (Image image = Image.Load(inputPath))
        {
            // Định cấu hình bộ nén PNG nâng cao
            var encoder = new PngEncoder
            {
                CompressionLevel = PngCompressionLevel.Level9, // Mức nén cao nhất (chậm hơn nhưng file nhỏ hơn)
                ColorType = PngColorType.Palette             // Chuyển sang dạng Palette để giảm dung lượng cực lớn (giống TinyPNG)
            };

            image.Save(outputPath, encoder);
        }
    }
}
