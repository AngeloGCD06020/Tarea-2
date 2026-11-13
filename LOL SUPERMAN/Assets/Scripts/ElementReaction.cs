using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "ElementReaction", menuName = "Scriptable Objects/ElementReaction")]
public class ElementReaction : ScriptableObject
{
    [Header("Required Elements")]
    public ElementData elementA;
    public ElementData elementB;

    [Header("Result")]
    public ElementData result;
    public AudioClip reactionSound;

    public bool CanReact(ElementData a, ElementData b)
    {
        return (a == elementA && b == elementB) || (a == elementB && == elementA);
    }
    //arfneiurneiurneirun

}
