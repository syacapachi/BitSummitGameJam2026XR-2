using System;
using UnityEngine;
[Serializable]
public class TitileAndText
{
    [SerializeField] string titie;
    [TextArea(3,50)]
    [SerializeField] string Description;

    public string Title => titie;
    public string DescriptionText => Description;
}
