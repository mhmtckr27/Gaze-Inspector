using UnityEngine;

namespace GrayWolf.Gaze.Demo
{
    [CreateAssetMenu(menuName = "Gaze/Demo/Demo Asset", fileName = "GazeDemoAsset", order = 900)]
    public sealed class GazeDemoAsset : ScriptableObject
    {
        [InfoBox("A ScriptableObject drawn by Gaze, not a component.", InfoMessageType.Info)]
        public string notes = "I am a ScriptableObject reference field.";

        [Min(0)] public int version = 1;
        [SerializeReference] public GazeDemoEffect payload;
    }
}