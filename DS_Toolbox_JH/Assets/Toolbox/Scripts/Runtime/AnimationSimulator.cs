using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ToolboxEngine
{
    public class AnimationSimulator : MonoBehaviour
    {
        private AnimationClip[] allAnimClipsArr = null;
        private float[] allAnimClipsOffsetArr = null;

        public void UpdateAnimClipsArr(AnimationClip[] newAnimClipArr)
        {
            allAnimClipsArr = newAnimClipArr;
        }

        public AnimationClip[] GetAllAnimClips()
        {
            return allAnimClipsArr;
        }

        public void UpdateAnimClipsOffsetArr(float[] newAnimClipOffsetArr)
        {
            allAnimClipsOffsetArr = newAnimClipOffsetArr;
        }

        public float[] GetAllAnimClipsOffset()
        {
            return allAnimClipsOffsetArr;
        }
    }
}
