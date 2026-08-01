using UnityEngine;

namespace PickupCent.UI
{
    /// <summary>
    /// Modal UI pause gate shared by shop and future popups. It only manages pause state;
    /// each popup owns its own full-screen blocking backdrop so UI input still works at timeScale 0.
    /// </summary>
    public static class PopupPauseManager
    {
        private static int pauseDepth;
        private static float previousTimeScale = 1f;

        public static bool IsPausedByPopup => pauseDepth > 0;

        public static void PushPause()
        {
            if (pauseDepth == 0)
            {
                previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }

            pauseDepth++;
        }

        public static void PopPause()
        {
            if (pauseDepth <= 0) return;

            pauseDepth--;
            if (pauseDepth == 0)
                Time.timeScale = previousTimeScale;
        }

        public static void ForceClear()
        {
            pauseDepth = 0;
            Time.timeScale = previousTimeScale;
        }
    }
}
