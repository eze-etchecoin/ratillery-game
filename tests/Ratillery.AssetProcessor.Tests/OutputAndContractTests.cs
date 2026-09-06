using System.Text.Json;
using SixLabors.ImageSharp.PixelFormats;

namespace Ratillery.AssetProcessor.Tests;

public sealed class OutputAndContractTests : IClassFixture<TileFixtures>
{
    private readonly TileFixtures _fx;

    public OutputAndContractTests(TileFixtures fx) => _fx = fx;

    [Fact]
    public void KindFromStem_AcceptsExactlyTheThreeCanonicalNames()
    {
        Assert.Equal(TileKind.SurfaceCap, TileProcessing.KindFromStem("surface-cap"));
        Assert.Equal(TileKind.Rock, TileProcessing.KindFromStem("rock"));
        Assert.Equal(TileKind.Interior, TileProcessing.KindFromStem("interior"));
        Assert.Null(TileProcessing.KindFromStem("idle"));
        Assert.Null(TileProcessing.KindFromStem("Rock"));
        Assert.Null(TileProcessing.KindFromStem("rock-copy"));
        Assert.Null(TileProcessing.KindFromStem("surface_cap"));
    }

    [Fact]
    public void MirrorTarget_MapsAssetsSourceToAssets()
    {
        Assert.Equal("assets/terrain/layers/rock.png", TileProcessing.MirrorTarget("assets-source/terrain/layers/rock.png"));
        Assert.Equal("assets/terrain/layers/rock.png", TileProcessing.MirrorTarget("assets-source\\terrain\\layers\\rock.png"));
        Assert.Null(TileProcessing.MirrorTarget("somewhere/else/rock.png"));
    }

    [Fact]
    public void Ok_WritesBitIdenticalPng_AndCorrectSidecar()
    {
        string source = _fx.SaveRgba("rock.png", 32, 32, (x, y) => TileFixtures.SeededNoise(x, y));
        string output = Path.Combine(_fx.Dir, "out", "deep", "rock.png");
        TileReport report = _fx.Validate(source, TileKind.Rock, output);
        Assert.Equal("ok", report.Verdict);

        TileProcessing.WriteOutputs(report, source, output);

        Assert.True(File.Exists(output));
        Assert.Equal(File.ReadAllBytes(source), File.ReadAllBytes(output));

        string sidecarPath = Path.ChangeExtension(output, ".json");
        Assert.True(File.Exists(sidecarPath));
        using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(sidecarPath));
        JsonElement root = doc.RootElement;
        Assert.Equal("rock", root.GetProperty("name").GetString());
        Assert.Equal("rock", root.GetProperty("kind").GetString());
        Assert.Equal(32, root.GetProperty("width").GetInt32());
        Assert.Equal(32, root.GetProperty("height").GetInt32());
        Assert.True(root.GetProperty("horizontalSeamless").GetBoolean());
        Assert.False(root.TryGetProperty("verticalSeamless", out _));
        Assert.False(root.TryGetProperty("earthTopRow", out _));
        Assert.False(root.TryGetProperty("frameCount", out _));
        Assert.False(root.TryGetProperty("fps", out _));
        Assert.False(root.TryGetProperty("pivot", out _));
    }

    [Fact]
    public void OkSurfaceCap_SidecarCarriesEarthTopFields()
    {
        string source = _fx.SaveRgba("surface-cap.png", 32, 16, (x, y) =>
            y < 4 ? new Rgba32(0, 0, 0, 0) : TileFixtures.Periodic(x, 1, 32));
        string output = Path.Combine(_fx.Dir, "cap", "surface-cap.png");
        TileReport report = _fx.Validate(source, TileKind.SurfaceCap, output);
        Assert.Equal("ok", report.Verdict);
        TileProcessing.WriteOutputs(report, source, output);

        using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(Path.ChangeExtension(output, ".json")));
        Assert.Equal(4, doc.RootElement.GetProperty("earthTopRow").GetInt32());
        Assert.Equal(12, doc.RootElement.GetProperty("bodyHeight").GetInt32());
        Assert.True(doc.RootElement.GetProperty("horizontalSeamless").GetBoolean());
    }

    [Fact]
    public void OkInterior_SidecarCarriesVerticalSeamless()
    {
        string source = _fx.SaveRgba("interior.png", 32, 32, (x, y) => TileFixtures.SeededNoise(x, y));
        string output = Path.Combine(_fx.Dir, "int", "interior.png");
        TileReport report = _fx.Validate(source, TileKind.Interior, output);
        Assert.Equal("ok", report.Verdict);
        TileProcessing.WriteOutputs(report, source, output);

        using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(Path.ChangeExtension(output, ".json")));
        Assert.True(doc.RootElement.GetProperty("verticalSeamless").GetBoolean());
    }

    [Fact]
    public void NeedsSourceFix_ReportTargetsNothing_AndProgramWritesNothing()
    {
        string source = _fx.SaveRgba("rock.png", 32, 32, (x, y) => TileFixtures.HardStepHorizontal(x, 32));
        string output = Path.Combine(_fx.Dir, "nope", "rock.png");
        TileReport report = _fx.Validate(source, TileKind.Rock, output);
        Assert.Equal("needs-source-fix", report.Verdict);
        Assert.Null(report.Output);

        int exit = Program.Main(["tile", source, "--output", output, "--json"]);
        Assert.Equal(0, exit);
        Assert.False(Directory.Exists(Path.Combine(_fx.Dir, "nope")));
    }

    [Fact]
    public void Program_TileOkRun_ExitZero_AndWritesOutput()
    {
        string source = _fx.SaveRgba("rock.png", 32, 32, (x, y) => TileFixtures.SeededNoise(x, y));
        string output = Path.Combine(_fx.Dir, "prog", "rock.png");
        int exit = Program.Main(["tile", source, "--output", output, "--report", Path.Combine(_fx.Dir, "r.json")]);
        Assert.Equal(0, exit);
        Assert.True(File.Exists(output));
        Assert.Equal(File.ReadAllBytes(source), File.ReadAllBytes(output));
        Assert.True(File.Exists(Path.ChangeExtension(output, ".json")));
        Assert.True(File.Exists(Path.Combine(_fx.Dir, "r.json")));
    }

    [Fact]
    public void Program_WrongStem_UsageError_NothingWritten()
    {
        string source = _fx.SaveRgba("idle.png", 32, 32, (_, _) => TileFixtures.Gray(50));
        string output = Path.Combine(_fx.Dir, "never", "idle.png");
        int exit = Program.Main(["tile", source, "--output", output]);
        Assert.NotEqual(0, exit);
        Assert.False(Directory.Exists(Path.Combine(_fx.Dir, "never")));
    }

    [Fact]
    public void Program_InputOutsideAssetsSource_WithoutOutput_UsageError()
    {
        string source = _fx.SaveRgba("rock.png", 32, 32, (_, _) => TileFixtures.Gray(50));
        int exit = Program.Main(["tile", source]);
        Assert.NotEqual(0, exit);
        Assert.False(Directory.Exists(Path.Combine(_fx.Dir, "assets")));
    }

    [Fact]
    public void Determinism_SameInputTwice_YieldsIdenticalReportsAndBytes()
    {
        string source = _fx.SaveRgba("interior.png", 32, 32, (x, y) => TileFixtures.SeededNoise(x, y));
        string output1 = Path.Combine(_fx.Dir, "d1", "interior.png");
        string output2 = Path.Combine(_fx.Dir, "d2", "interior.png");

        TileReport first = TileProcessing.Validate(source, TileKind.Interior, output1);
        TileProcessing.WriteOutputs(first, source, output1);
        TileReport second = TileProcessing.Validate(source, TileKind.Interior, output1);
        TileProcessing.WriteOutputs(second, source, output2);

        Assert.Equal(TileProcessing.RenderJson(first), TileProcessing.RenderJson(second));
        Assert.Equal(File.ReadAllBytes(output1), File.ReadAllBytes(output2));
        Assert.Equal(File.ReadAllBytes(Path.ChangeExtension(output1, ".json")), File.ReadAllBytes(Path.ChangeExtension(output2, ".json")));
    }

    [Fact]
    public void ReportContract_IncludesTileSpecificFields()
    {
        string source = _fx.SaveRgba("surface-cap.png", 32, 16, (x, y) =>
            y < 4 ? new Rgba32(0, 0, 0, 0) : TileFixtures.Periodic(x, 1, 32));
        TileReport report = _fx.Validate(source, TileKind.SurfaceCap, "out/surface-cap.png");
        string json = TileProcessing.RenderJson(report);
        using JsonDocument doc = JsonDocument.Parse(json);
        Assert.Equal("ok", doc.RootElement.GetProperty("verdict").GetString());
        Assert.Equal("surface-cap", doc.RootElement.GetProperty("kind").GetString());
        Assert.Equal(4, doc.RootElement.GetProperty("earthTopRow").GetInt32());
        Assert.Equal(12, doc.RootElement.GetProperty("bodyHeight").GetInt32());
        Assert.True(doc.RootElement.GetProperty("hasAlpha").GetBoolean());
        Assert.True(doc.RootElement.GetProperty("horizontalSeam").GetProperty("pass").GetBoolean());
        Assert.True(doc.RootElement.TryGetProperty("thresholds", out _));
        Assert.Equal("out/surface-cap.png", doc.RootElement.GetProperty("output").GetString());
        Assert.True(doc.RootElement.GetProperty("issues").GetArrayLength() == 0);
    }
}
