using TypeReader.Core;

namespace TypeReader.Core.Tests;

public class CropMathTests
{
    [Fact]
    public void ToPixels_MapsNormalizedToPixels()
    {
        var crop = new ImageCrop { X = 0.25, Y = 0.5, Width = 0.5, Height = 0.25 };
        var r = CropMath.ToPixels(crop, 400, 600);
        Assert.Equal((100, 300, 200, 150), (r.X, r.Y, r.Width, r.Height));
    }

    [Fact]
    public void ToNormalized_InvertsToPixels()
    {
        var crop = new ImageCrop { X = 0.25, Y = 0.5, Width = 0.5, Height = 0.25 };
        var px = CropMath.ToPixels(crop, 400, 600);
        var back = CropMath.ToNormalized(px, 400, 600);
        Assert.Equal(crop.X, back.X, 2);
        Assert.Equal(crop.Y, back.Y, 2);
        Assert.Equal(crop.Width, back.Width, 2);
        Assert.Equal(crop.Height, back.Height, 2);
    }

    [Fact]
    public void CenterCropToRatio_WideImage_CropsWidth()
    {
        var c = CropMath.CenterCropToRatio(1000, 100, 2.5);
        Assert.Equal(0.375, c.X, 3);
        Assert.Equal(0, c.Y, 3);
        Assert.Equal(0.25, c.Width, 3);
        Assert.Equal(1, c.Height, 3);
    }

    [Fact]
    public void CenterCropToRatio_PortraitImage_CropsHeight()
    {
        var c = CropMath.CenterCropToRatio(400, 600, 2.5);
        Assert.Equal(0, c.X, 3);
        Assert.Equal(220.0 / 600, c.Y, 3);
        Assert.Equal(1, c.Width, 3);
        Assert.Equal(160.0 / 600, c.Height, 3);
    }

    [Fact]
    public void ClampCropRect_ClampsSizePositionAndRatio()
    {
        var r = CropMath.ClampCropRect(10, 10, 100000, 5, 400, 600, 2.5, 24);
        Assert.Equal((0, 10, 400, 160), (r.X, r.Y, r.Width, r.Height));
    }

    [Fact]
    public void ClampCropRect_EnforcesMinWidth()
    {
        var r = CropMath.ClampCropRect(-50, -50, 5, 5, 400, 600, 2.5, 24);
        Assert.Equal((0, 0, 24, 10), (r.X, r.Y, r.Width, r.Height));
    }

    [Fact]
    public void ClampCropRect_MoveKeepsSize()
    {
        var r = CropMath.ClampCropRect(-30, 700, 100, 40, 400, 600, 2.5, 24);
        Assert.Equal((0, 560, 100, 40), (r.X, r.Y, r.Width, r.Height));
    }
}
