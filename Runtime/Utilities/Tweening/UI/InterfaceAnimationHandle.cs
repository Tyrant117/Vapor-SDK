using System;
using System.Collections.Generic;

namespace Vapor.Tweening
{
    public class InterfaceAnimationHandle
    {
        private int NextID => _animations.Count;

        private bool _isInitialized;
        private int _lastCount;
        private int _currentCompletedCount;

        private readonly List<BaseUIAnimation> _rootAnimations = new();
        private readonly List<BaseUIAnimation> _animations = new();
        private readonly Dictionary<Type, BaseUIAnimation> _firstOfTypes = new();


        /// <summary>
        /// Creates an animation handle.
        /// </summary>
        /// <param name="rootAnimations">Same as calling <see cref="Attach(BaseUIAnimation)"/> to this set of animations.</param>
        public InterfaceAnimationHandle(params BaseUIAnimation[] rootAnimations)
        {
            _isInitialized = false;
            foreach (var anim in rootAnimations)
            {
                Attach(anim);
            }
        }

        /// <summary>
        /// Intialized the handle, this should be done after all animations have been added. It will also be done automatically the first time the handle is played.
        /// </summary>
        public void Init()
        {
            foreach (var anim in _animations)
            {
                if (!_firstOfTypes.ContainsKey(anim.GetType()))
                {
                    _firstOfTypes.Add(anim.GetType(), anim);
                }
            }
            _isInitialized = true;
        }

        /// <summary>
        /// Attaches this animation to the root of the handle, root animations will play first.
        /// </summary>
        /// <param name="root"></param>
        public void Attach(BaseUIAnimation root)
        {
            root.Handle = this;
            _rootAnimations.Add(root);
            root.Index = NextID;
            _animations.Add(root);
        }

        internal void AddAnimation(BaseUIAnimation anim)
        {
            anim.Index = NextID;
            _animations.Add(anim);
        }

        /// <summary>
        /// Update must be called to progress the animations.
        /// </summary>
        public void Update()
        {
            foreach (var anim in _animations)
            {
                if (anim.IsPlaying && !anim.OnUpdate())
                {
                    anim.Stop();
                }
            }
        }

        /// <summary>
        /// Plays from the beginning
        /// </summary>
        public void Play()
        {
            if (!_isInitialized)
            {
                Init();
            }

            Reset();
            foreach (var root in _rootAnimations)
            {
                root.TryPlay();
            }
        }

        /// <summary>
        /// Resets the animations to their starting state, and stops any animations still playing.
        /// </summary>
        public void Reset()
        {
            _currentCompletedCount = 0;
            foreach (var anim in _animations)
            {
                if (anim.IsPlaying)
                {
                    anim.Stop(true);
                }
            }

            foreach (var first in _firstOfTypes.Values)
            {
                first.Reset();
            }
        }

        /// <summary>
        /// Pauses any active animations
        /// </summary>
        public void Pause()
        {
            foreach (var anim in _animations)
            {
                if(anim.IsPlaying)
                {
                    anim.Pause();
                }
            }
        }


        /// <summary>
        /// Resumes any paused animations
        /// </summary>
        public void Resume()
        {
            foreach (var anim in _animations)
            {
                if(anim.IsPlaying)
                {
                    anim.Resume();
                }
            }
        }

        /// <summary>
        /// Completes the animation sequence, setting them all to their final state.
        /// </summary>
        public void Complete()
        {
            foreach (var anim in _animations)
            {
                if (anim.IsLast)
                {
                    anim.TryPlay();
                    anim.Complete();
                }
                else if (anim.IsPlaying)
                {
                    anim.Stop(true);
                }
            }
        }        
    }
}
