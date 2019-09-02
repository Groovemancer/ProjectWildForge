#region License
// ====================================================
// Project Porcupine Copyright(C) 2016 Team Porcupine
// This program comes with ABSOLUTELY NO WARRANTY; This is free software, 
// and you are welcome to redistribute it under certain conditions; See 
// file LICENSE, which is part of this source code package, for details.
// ====================================================
#endregion

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Animation
{
    /// <summary>
    /// Animations for structure. Can have several "states" that can be switched using SetState.
    /// </summary>
    public class StructureAnimation
    {
        // current shown frame
        private int prevFrameIndex;

        // Holds the actual animation sprites from the spritesheet
        private Sprite[] sprites;

        // Collection of animations
        private Dictionary<string, SpritenameAnimation> animations;
        private SpritenameAnimation currentAnimation;
        private string currentAnimationState;

        public StructureAnimation()
        {
            animations = new Dictionary<string, SpritenameAnimation>();
        }

        public StructureAnimation(Dictionary<string, SpritenameAnimation> incomingAnimations)
        {
            animations = incomingAnimations;
            if (string.IsNullOrEmpty(currentAnimationState))
            {
                currentAnimationState = animations.Keys.First();
                currentAnimation = animations[currentAnimationState];
                prevFrameIndex = 0;
            }
        }

        public SpriteRenderer Renderer { get; set; }

        public StructureAnimation Clone()
        {
            StructureAnimation newSA = new StructureAnimation();
            newSA.sprites = sprites;
            newSA.animations = new Dictionary<string, SpritenameAnimation>();

            foreach (KeyValuePair<string, SpritenameAnimation> entry in animations)
            {
                newSA.animations.Add(entry.Key, (SpritenameAnimation)entry.Value.Clone());
            }

            newSA.currentAnimationState = currentAnimationState;
            newSA.currentAnimation = newSA.animations[currentAnimationState];
            newSA.prevFrameIndex = 0;
            return newSA;
        }

        public void Update(float deltaTime)
        {
            currentAnimation.Update(deltaTime);
            CheckFrameChange();
        }

        /// <summary>
        /// Structure has changed, so make sure the sprite is updated.
        /// </summary>
        public void OnStructureChanged()
        {
            ShowSprite(GetSpriteName());
        }

        /// <summary>
        /// Set the animation frame depending on a value. The currentvalue percent of the maxvalue will determine which frame is shown.
        /// </summary>
        public void SetProgressValue(float percent)
        {
            currentAnimation.SetProgressValue(percent);
            CheckFrameChange();
        }

        public void SetFrameIndex(int frameIndex)
        {
            currentAnimation.SetFrame(frameIndex);
            CheckFrameChange();
        }

        /// <summary>
        /// Set the animation state. Will only have an effect if stateName is different from current animation stateName.
        /// </summary>
        public void SetState(string stateName)
        {
            if (stateName == currentAnimationState)
            {
                return;
            }

            SpritenameAnimation animation;
            if (animations.TryGetValue(stateName, out animation) == false)
            {
                return;
            }

            currentAnimationState = stateName;
            currentAnimation = animation;
            currentAnimation.Play();
            ShowSprite(currentAnimation.CurrentFrameName);
        }

        /// <summary>
        /// Get spritename from the current animation.
        /// </summary>
        public string GetSpriteName()
        {
            return currentAnimation.CurrentFrameName;
        }

        /// <summary>
        /// Add animation to structure. First animation added will be default for sprites e.g. ghost image when placing structure.
        /// </summary>
        public void AddAnimation(string state, List<string> spriteNames, float fps, bool looping, bool valueBased)
        {
            animations.Add(state, new SpritenameAnimation(state, spriteNames.ToArray(), 1 / fps, looping, false, valueBased));

            // set default state to first state entered - most likely "idle"
            // TODO: Is it really best practice to base the default on first in?
            if (string.IsNullOrEmpty(currentAnimationState))
            {
                currentAnimationState = state;
                currentAnimation = animations[currentAnimationState];
                prevFrameIndex = 0;
            }
        }

        // check if time or value requires us to show a new animationframe
        private void CheckFrameChange()
        {
            if (prevFrameIndex != currentAnimation.CurrentFrame)
            {
                ShowSprite(currentAnimation.CurrentFrameName);
                prevFrameIndex = currentAnimation.CurrentFrame;
            }
        }

        private void ShowSprite(string spriteName)
        {
            if (Renderer != null)
            {
                Renderer.sprite = SpriteManager.GetSprite("Structure", spriteName);
            }
        }
    }
}