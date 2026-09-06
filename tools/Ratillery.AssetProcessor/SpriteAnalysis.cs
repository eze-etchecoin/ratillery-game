using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Ratillery.AssetProcessor;

public sealed record FrameBounds(int Left, int Top, int Width, int Height);

public static class SpriteAnalysis
{
    public static int[] ColumnTop(Image<Rgba32> image, int alphaThreshold)
    {
        int w = image.Width;
        var top = new int[w];
        Array.Fill(top, -1);

        for (int y = 0; y < image.Height; y++)
        {
            image.ProcessPixelRows(accessor =>
            {
                var row = accessor.GetRowSpan(y);
                for (int x = 0; x < w; x++)
                {
                    if (top[x] >= 0 || row[x].A <= alphaThreshold)
                    {
                        continue;
                    }
                    top[x] = y;
                }
            });
        }

        return top;
    }

    public static (int Left, int Top, int Right, int Bottom) ContentBounds(Image<Rgba32> image, int alphaThreshold)
    {
        int w = image.Width;
        int minX = w, maxX = -1, minY = image.Height, maxY = -1;

        for (int y = 0; y < image.Height; y++)
        {
            image.ProcessPixelRows(accessor =>
            {
                var row = accessor.GetRowSpan(y);
                for (int x = 0; x < w; x++)
                {
                    if (row[x].A <= alphaThreshold)
                    {
                        continue;
                    }
                    if (x < minX) minX = x;
                    if (x > maxX) maxX = x;
                    if (y < minY) minY = y;
                    if (y > maxY) maxY = y;
                }
            });
        }

        if (maxX < 0)
        {
            throw new InvalidOperationException("the image contains no opaque pixels above the alpha threshold");
        }

        return (minX, minY, maxX, maxY);
    }

    public static (int[] Separators, int Cut) FindSplit(int[] top, int frameCount, int minRunWidth)
    {
        int minTop = top.Where(t => t >= 0).DefaultIfEmpty(int.MaxValue).Min();
        int maxTop = top.Where(t => t >= 0).Max();
        int bestCut = -1;
        int bestMinGap = -1;

        for (int cut = minTop; cut <= maxTop; cut++)
        {
            List<(int Start, int End)> wide = WideRuns(top, cut, minRunWidth);
            if (wide.Count != frameCount)
            {
                continue;
            }

            int minGap = int.MaxValue;
            for (int i = 0; i < wide.Count - 1; i++)
            {
                int gap = wide[i + 1].Start - wide[i].End - 1;
                if (gap < minGap)
                {
                    minGap = gap;
                }
            }

            if (minGap > bestMinGap)
            {
                bestMinGap = minGap;
                bestCut = cut;
            }
        }

        if (bestCut < 0)
        {
            throw new InvalidOperationException(
                $"could not isolate {frameCount} frames from the alpha silhouette (try --frames, " +
                $"--alpha-threshold or --min-run-width); use inspect for guidance");
        }

        List<(int Start, int End)> runs = WideRuns(top, bestCut, minRunWidth);
        var separators = new int[frameCount - 1];
        for (int i = 0; i < separators.Length; i++)
        {
            separators[i] = (runs[i].End + runs[i + 1].Start) / 2;
        }

        return (separators, bestCut);
    }

    public static List<(int Start, int End)> WideRuns(int[] top, int cut, int minRunWidth)
    {
        var runs = new List<(int Start, int End)>();
        int width = top.Length;
        int start = -1;

        for (int x = 0; x <= width; x++)
        {
            bool inside = x < width && top[x] >= 0 && top[x] <= cut;
            if (inside && start < 0)
            {
                start = x;
            }
            else if (!inside && start >= 0)
            {
                if (x - start >= minRunWidth)
                {
                    runs.Add((start, x - 1));
                }
                start = -1;
            }
        }

        return runs;
    }

    public static FrameBounds[] FrameBoxes(
        Image<Rgba32> image,
        int[] separators,
        int alphaThreshold)
    {
        int width = image.Width;
        int count = separators.Length + 1;
        var boxes = new FrameBounds[count];

        for (int k = 0; k < count; k++)
        {
            int regionLeft = k == 0 ? 0 : separators[k - 1];
            int regionRight = k == count - 1 ? width : separators[k];
            int minX = regionRight, maxX = regionLeft - 1;
            int minY = image.Height, maxY = -1;

            for (int y = 0; y < image.Height; y++)
            {
                image.ProcessPixelRows(accessor =>
                {
                    var row = accessor.GetRowSpan(y);
                    for (int x = regionLeft; x < regionRight; x++)
                    {
                        if (row[x].A <= alphaThreshold)
                        {
                            continue;
                        }
                        if (x < minX) minX = x;
                        if (x > maxX) maxX = x;
                        if (y < minY) minY = y;
                        if (y > maxY) maxY = y;
                    }
                });
            }

            if (maxY < 0)
            {
                throw new InvalidOperationException($"frame {k} (x {regionLeft}..{regionRight - 1}) has no visible content");
            }

            boxes[k] = new FrameBounds(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }

        return boxes;
    }

    public static void CopyRegion(Image<Rgba32> source, Image<Rgba32> destination, FrameBounds box, int destX, int destY)
    {
        for (int y = 0; y < box.Height; y++)
        {
            for (int x = 0; x < box.Width; x++)
            {
                destination[destX + x, destY + y] = source[box.Left + x, box.Top + y];
            }
        }
    }
}
