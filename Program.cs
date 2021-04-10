using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using Vanara.PInvoke;

namespace csharp_runtime_issue_20210410
{
    class MouseHook: IDisposable
    {
        private User32.SafeHHOOK? _hookHandle = null;
        private MouseHook() { }
        public static MouseHook Init()
        {
            var obj = new MouseHook();
            obj.SetupHook();
            return obj;
        }

        private void SetupHook()
        {
            _hookHandle = User32.SetWindowsHookEx(User32.HookType.WH_MOUSE_LL, HookCallback, default, 0);
            Win32Error.ThrowLastErrorIfInvalid(_hookHandle, "Failed to set low-level mouse hook");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            // ERROR; We asynchronously set our own handle, and a race happens when
            // the callback is executed before setup completed!
            if (_hookHandle == null || nCode < 0)
            {
                return User32.CallNextHookEx(default, nCode, wParam, lParam);
            }

            if (lParam != default)
            {
                _ = Marshal.PtrToStructure<User32.MSLLHOOKSTRUCT>(lParam);
            }

            return User32.CallNextHookEx(default, nCode, wParam, lParam);
        }

        public void Dispose() => _hookHandle.Dispose();
    }

    class Program : Application
    {
        private MouseHook? _hook;

        [STAThread]
        public static int Main() => new Program().Run();

        protected override void OnStartup(StartupEventArgs e)
        {
            _hook = MouseHook.Init();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            _hook.Dispose();
        }
    }
}
