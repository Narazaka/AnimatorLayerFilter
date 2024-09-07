using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VRC.SDKBase;

namespace Narazaka.VRChat.AnimatorLayerFilter
{
    public class AnimatorLayerFilter : MonoBehaviour, IEditorOnly
    {
        public List<string> onlyNames = new List<string>();
        public List<string> ignoreNames = new List<string>();
    }
}
