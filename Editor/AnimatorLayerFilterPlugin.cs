using nadena.dev.modular_avatar.core;
using nadena.dev.ndmf;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Animations;

[assembly: ExportsPlugin(typeof(Narazaka.VRChat.AnimatorLayerFilter.Editor.AnimatorLayerFilterPlugin))]

namespace Narazaka.VRChat.AnimatorLayerFilter.Editor
{
    public class AnimatorLayerFilterPlugin : Plugin<AnimatorLayerFilterPlugin>
    {
        public override string QualifiedName => "net.narazaka.vrchat.animator-layer-filter";

        public override string DisplayName => "Animator Layer Filter";

        protected override void Configure()
        {
            InPhase(BuildPhase.Resolving).BeforePlugin("nadena.dev.modular-avatar").Run("AnimatorLayerFilter", ctx =>
            {
                var animatorLayerFilters = ctx.AvatarRootObject.GetComponentsInChildren<AnimatorLayerFilter>(true);
                if (animatorLayerFilters.Length == 0) return;

                foreach (var animatorLayerFilter in animatorLayerFilters)
                {
                    var mergeAnimators = animatorLayerFilter.GetComponents<ModularAvatarMergeAnimator>();
                    foreach (var mergeAnimator in mergeAnimators)
                    {
                        var animator = mergeAnimator.animator as AnimatorController;
                        if (animator == null) continue;

                        var newAnimator = new AnimatorController();
                        foreach (var parameter in animator.parameters)
                        {
                            newAnimator.AddParameter(parameter);
                        }
                        var layers = new List<AnimatorControllerLayer>();
                        foreach (var layer in animator.layers)
                        {
                            if (animatorLayerFilter.onlyNames.Count > 0 && !animatorLayerFilter.onlyNames.Contains(layer.name)) continue;
                            if (animatorLayerFilter.ignoreNames.Count > 0 && animatorLayerFilter.ignoreNames.Contains(layer.name)) continue;
                            layers.Add(layer);
                        }
                        newAnimator.layers = layers.ToArray();
                        mergeAnimator.animator = newAnimator;
                    }

                    Object.DestroyImmediate(animatorLayerFilter);
                }
            });
        }
    }
}
