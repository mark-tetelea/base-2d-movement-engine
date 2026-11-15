// Builds the pixel font asset for UI Toolkit from Assets/UI/Fonts/KitPixel.ttf (EDIT mode):
//     python Tools/font/make-pixel-font.py
//     unity command eval_file --file Tools/measure/setup-pixel-font.cs
// A static bitmap atlas (hinted raster at 8 px, point filtered), so the font's pixels stay
// square at every whole-number UI scale. Also makes Assets/UI/KitThemePixel.asset: the
// kit's theme with this font (point Resources/KitTheme's font at it, or copy the asset).
const string ttf = "Assets/UI/Fonts/KitPixel.ttf";
const string assetPath = "Assets/UI/Fonts/KitPixel.asset";
AssetDatabase.ImportAsset(ttf, ImportAssetOptions.ForceUpdate);
var font = AssetDatabase.LoadAssetAtPath<Font>(ttf);
if (font == null) return "no font at " + ttf;

var old = AssetDatabase.LoadAssetAtPath<UnityEngine.TextCore.Text.FontAsset>(assetPath);
if (old != null) AssetDatabase.DeleteAsset(assetPath);
var fa = UnityEngine.TextCore.Text.FontAsset.CreateFontAsset(font, 8, 1, UnityEngine.TextCore.LowLevel.GlyphRenderMode.RASTER_HINTED, 256, 256,
    UnityEngine.TextCore.Text.AtlasPopulationMode.Dynamic, true);
if (fa == null) return "CreateFontAsset failed";
fa.name = "KitPixel";
string chars = "";
for (int c = 32; c < 127; c++) chars += (char)c;
bool all = fa.TryAddCharacters(chars, out string missing);
fa.atlasPopulationMode = UnityEngine.TextCore.Text.AtlasPopulationMode.Static;
AssetDatabase.CreateAsset(fa, assetPath);
foreach (var t in fa.atlasTextures)
{
    if (t == null) continue;
    t.filterMode = FilterMode.Point;
    t.name = "KitPixel Atlas";
    AssetDatabase.AddObjectToAsset(t, fa);
}
if (fa.material != null) { fa.material.name = "KitPixel Material"; AssetDatabase.AddObjectToAsset(fa.material, fa); }
EditorUtility.SetDirty(fa);

// The pixel theme: a copy of the kit theme with this font.
var baseTheme = AssetDatabase.LoadAssetAtPath<TopDownKit.UITheme>("Assets/Resources/KitTheme.asset");
const string pixelThemePath = "Assets/UI/KitThemePixel.asset";
var pt = AssetDatabase.LoadAssetAtPath<TopDownKit.UITheme>(pixelThemePath);
if (pt == null)
{
    pt = baseTheme != null ? UnityEngine.Object.Instantiate(baseTheme) : ScriptableObject.CreateInstance<TopDownKit.UITheme>();
    AssetDatabase.CreateAsset(pt, pixelThemePath);
}
pt.fontAsset = fa;
pt.font = null;
pt.pixelFontSize = 8;
pt.textShadow = true;
EditorUtility.SetDirty(pt);
AssetDatabase.SaveAssets();
return "font asset: " + fa.characterTable.Count + " characters" + (all ? "" : ", missing: " + missing)
     + ", atlas " + (fa.atlasTexture != null ? fa.atlasTexture.width + "x" + fa.atlasTexture.height + " " + fa.atlasTexture.filterMode : "none")
     + "; theme " + pixelThemePath;
