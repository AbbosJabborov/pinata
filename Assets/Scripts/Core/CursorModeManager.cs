using System.Collections.Generic;
using UnityEngine;

namespace Pinata.Core
{
    /// <summary>
    /// Reference-counted cursor unlock manager.
    /// Any panel calls RequestUnlock(this) on open and ReleaseUnlock(this) on close.
    /// The cursor only re-locks once every requester has released.
    /// </summary>
    public static class CursorModeManager
    {
        private static readonly HashSet<object> _requesters = new HashSet<object>();

        public static bool IsUnlocked => _requesters.Count > 0 || Cursor.lockState != CursorLockMode.Locked;

        public static void RequestUnlock(object requester)
        {
            if (requester != null)
            {
                _requesters.Add(requester);
            }
            ApplyCursorState();
        }

        public static void ReleaseUnlock(object requester)
        {
            if (requester != null)
            {
                _requesters.Remove(requester);
            }
            ApplyCursorState();
        }

        public static void Clear()
        {
            _requesters.Clear();
            ApplyCursorState();
        }

        private static void ApplyCursorState()
        {
            if (_requesters.Count > 0)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }
}
