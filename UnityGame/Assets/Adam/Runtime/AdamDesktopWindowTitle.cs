#if (UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN) && !UNITY_EDITOR
using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace BibleSurvivor.Unity
{
    public sealed partial class AdamGame
    {
        float nextWindowTitleCheck;

        void LateUpdate()
        {
#if UNITY_STANDALONE_WIN
            if (Screen.fullScreen)
                return;
#endif
            if (Time.unscaledTime < nextWindowTitleCheck)
                return;

            // Unity can recreate or retitle the native window during a display-mode transition.
            nextWindowTitleCheck = Time.unscaledTime + 1;
            AdamDesktopWindowTitle.Apply();
        }
    }

    static class AdamDesktopWindowTitle
    {
        public static void Apply()
        {
#if UNITY_STANDALONE_OSX
            // AppKit windows belong to the Cocoa main thread.
            if (pthread_main_np() != 1)
                return;
            var app = Send(objc_getClass("NSApplication"), sel_registerName("sharedApplication"));
            var windows = Send(app, sel_registerName("orderedWindows"));
            var count = Send(windows, sel_registerName("count")).ToInt64();
            for (var index = 0L; index < count; index++)
            {
                var window = IndexedObject(windows, sel_registerName("objectAtIndex:"), (IntPtr)index);
                var title = Send(window, sel_registerName("title"));
                var utf8 = Send(title, sel_registerName("UTF8String"));
                if (Marshal.PtrToStringUTF8(utf8) != Application.productName)
                    continue;
                var name = CreateString(objc_getClass("NSString"), sel_registerName("stringWithUTF8String:"), AdamGameTitle.English);
                SendObject(window, sel_registerName("setTitle:"), name);
                return;
            }
#elif UNITY_STANDALONE_WIN
            using var process = System.Diagnostics.Process.GetCurrentProcess();
            var window = process.MainWindowHandle;
            if (window == IntPtr.Zero)
                return;
            var title = new System.Text.StringBuilder(256);
            if (GetWindowTextW(window, title, title.Capacity) > 0 && title.ToString() == Application.productName)
                SetWindowTextW(window, AdamGameTitle.English);
#endif
        }

#if UNITY_STANDALONE_OSX
        const string ObjC = "/usr/lib/libobjc.A.dylib";
        [DllImport("/usr/lib/libSystem.B.dylib")] static extern int pthread_main_np();
        [DllImport(ObjC)] static extern IntPtr objc_getClass(string name);
        [DllImport(ObjC)] static extern IntPtr sel_registerName(string name);
        [DllImport(ObjC, EntryPoint = "objc_msgSend")] static extern IntPtr Send(IntPtr receiver, IntPtr selector);
        [DllImport(ObjC, EntryPoint = "objc_msgSend")] static extern IntPtr IndexedObject(IntPtr receiver, IntPtr selector, IntPtr index);
        [DllImport(ObjC, EntryPoint = "objc_msgSend")] static extern void SendObject(IntPtr receiver, IntPtr selector, IntPtr value);
        [DllImport(ObjC, EntryPoint = "objc_msgSend")]
        static extern IntPtr CreateString(IntPtr receiver, IntPtr selector, [MarshalAs(UnmanagedType.LPUTF8Str)] string value);
#elif UNITY_STANDALONE_WIN
        [DllImport("user32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        static extern int GetWindowTextW(IntPtr window, System.Text.StringBuilder title, int capacity);
        [DllImport("user32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        static extern bool SetWindowTextW(IntPtr window, string title);
#endif
    }
}
#endif
