using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class ColorVisual : MonoBehaviour
{
    public BlockType blockType;  // block için
    public GoalType goalType;    // goal için

    public Color blue = Color.blue;
    public Color red = Color.red;
    public Color green = Color.green;
    public Color yellow = Color.yellow;

    Renderer rend;

    void Awake()
    {
        rend = GetComponent<Renderer>();
        Apply();
    }

    // Spawn sonrasý da çaðýrabilmek için public
    public void Apply()
    {
        BlockColor c;

        if (blockType != null) c = blockType.color;
        else if (goalType != null) c = goalType.color;
        else return;

        rend.material.color = ToUnityColor(c);
    }

    Color ToUnityColor(BlockColor c)
    {
        return c switch
        {
            BlockColor.Blue => blue,
            BlockColor.Red => red,
            BlockColor.Green => green,
            BlockColor.Yellow => yellow,
            _ => Color.white
        };
    }
}
