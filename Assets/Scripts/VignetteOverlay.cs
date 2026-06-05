using UnityEngine;

[RequireComponent(typeof(Camera))]
public class VignetteOverlay : MonoBehaviour
{
    public Color color = new Color(1f, 0.08f, 0.08f, 0f);

    Material mat;

    void Awake()
    {
        Shader shader = Shader.Find("Hidden/Internal-Colored");
        mat = new Material(shader);
        mat.hideFlags = HideFlags.HideAndDontSave;
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        mat.SetInt("_ZWrite", 0);
    }

    public void SetAlpha(float a) { color.a = Mathf.Clamp01(a); }
    public void SetColor(Color c) { color = c; }

    void OnPostRender()
    {
        if (color.a <= 0f) return;

        GL.PushMatrix();
        GL.LoadOrtho();
        mat.SetPass(0);
        GL.Begin(GL.QUADS);
        GL.Color(color);
        GL.Vertex3(0, 0, 0);
        GL.Vertex3(1, 0, 0);
        GL.Vertex3(1, 1, 0);
        GL.Vertex3(0, 1, 0);
        GL.End();
        GL.PopMatrix();
    }
}
