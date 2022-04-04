using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SmashysFramework
{
    public static class Statics
    {
        public static int MaxPlayerCount
        {
            get
            {
                GameModeConfiguration configuration = GameManager.GetGameModeConfiguration();
                if (configuration)
                {
                    return configuration.MaxPlayerCount;
                }

                return 0;
            }
        }

        public static int StringToHash(string name)
        {
            // At the moment this function is just a wrapper for Animator.StringToHash.
            // Still here in case I wanna change the implementation somewhere in the future!
            return Animator.StringToHash(name);
        }


        /// <summary>
        /// Returns Application.isPlaying, but also prints a debug message if false.
        /// This will always return true for compiled games.
        /// </summary>
        /// <param name="msg">The message to print.</param>
        public static bool AssertIsPlaying(string msg)
        {
    #if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                if (msg != string.Empty)
                    Debug.LogError(msg);

                return false;
            }
    #endif
            return true;
        }
    }
}
