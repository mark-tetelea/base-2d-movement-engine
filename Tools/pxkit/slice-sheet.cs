// Slices pxkit player sheets (16x24 frames side by side) in Unity: Multiple sprite mode,
// PPU 16, Point, no compression, every frame pivoted bottom-centre (ART.md rule 7).
// Run with: unity command eval_file --file Tools/pxkit/slice-sheet.cs
// Edit the list below. eval runs this as a method body: no using lines.
string[] sheets = { "player-right", "player-up-right", "player-up", "player-down", "player-down-right" };
string report = "";
foreach (var id in sheets)
{
    string path = "Assets/Art/Generated/" + id + ".png";
    AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
    var ti = (TextureImporter)AssetImporter.GetAtPath(path);
    var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    int n = tex.width / 16;
    ti.textureType = TextureImporterType.Sprite;
    ti.spriteImportMode = SpriteImportMode.Multiple;
    ti.spritePixelsPerUnit = 16;
    ti.filterMode = FilterMode.Point;
    ti.mipmapEnabled = false;
    ti.textureCompression = TextureImporterCompression.Uncompressed;
#pragma warning disable 618
    var metas = new SpriteMetaData[n];
    for (int i = 0; i < n; i++)
        metas[i] = new SpriteMetaData { name = id + "_" + i, rect = new Rect(i * 16, 0, 16, tex.height),
            alignment = (int)SpriteAlignment.BottomCenter, pivot = new Vector2(0.5f, 0f) };
    ti.spritesheet = metas;
#pragma warning restore 618
    ti.SaveAndReimport();
    foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path))
        if (o is Sprite s) report += s.name + " " + s.rect + " ppu " + s.pixelsPerUnit + " pivot " + s.pivot + "\n";
}
return report;
