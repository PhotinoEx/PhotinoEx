using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using PhotinoEx.Core.Enums;
using PhotinoEx.Core.Models;
using Action = System.Action;
using Monitor = PhotinoEx.Core.Models.Monitor;
using Size = System.Drawing.Size;
using Point = System.Drawing.Point;

namespace PhotinoEx.Core;

public class PhotinoWindow
{
    #region Private Fields

    /// <summary>
    /// Parameters sent to Photino.Native to start a new instance of a Photino.Native window.
    /// </summary>
    private PhotinoInitParams _startupParameters = new()
    {
        Resizable = true,
        ContextMenuEnabled = true,
        CustomSchemeNames = new(),
        DevToolsEnabled = true,
        GrantBrowserPermissions = true,
        UserAgent = "PhotinoEx WebView",
        MediaAutoplayEnabled = true,
        FileSystemAccessEnabled = true,
        WebSecurityEnabled = true,
        JavascriptClipboardAccessEnabled = true,
        MediaStreamEnabled = true,
        SmoothScrollingEnabled = true,
        IgnoreCertificateErrorsEnabled = false,
        NotificationsEnabled = true,
        TemporaryFilesPath = IsWindowsPlatform
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PhotinoEx")
            : null,
        Title = "PhotinoEx",
        UseOsDefaultSize = true,
        Zoom = 100,
        MaxHeight = int.MaxValue,
        MaxWidth = int.MaxValue
    };

    private Photino? _instance;
    private readonly int _managedThreadId;

    //There can only be 1 message loop for all windows.
    private static bool _messageLoopIsStarted;

    #endregion

    #region Get Properties

    /// <summary>
    /// Indicates whether the current platform is Windows.
    /// </summary>
    /// <value>
    /// <c>true</c> if the current platform is Windows; otherwise, <c>false</c>.
    /// </value>
    public static bool IsWindowsPlatform
    {
        get { return RuntimeInformation.IsOSPlatform(OSPlatform.Windows); }
    }

    /// <summary>
    /// Indicates whether the current platform is MacOS.
    /// </summary>
    /// <value>
    /// <c>true</c> if the current platform is MacOS; otherwise, <c>false</c>.
    /// </value>
    public static bool IsMacOsPlatform
    {
        get { return RuntimeInformation.IsOSPlatform(OSPlatform.OSX); }
    }

    /// <summary>
    /// Indicates the version of MacOS
    /// </summary>
    public static Version? MacOsVersion
    {
        get { return IsMacOsPlatform ? Version.Parse(RuntimeInformation.OSDescription.Split(' ')[1]) : null; }
    }

    /// <summary>
    /// Indicates whether the current platform is Linux.
    /// </summary>
    /// <value>
    /// <c>true</c> if the current platform is Linux; otherwise, <c>false</c>.
    /// </value>
    public static bool IsLinuxPlatform
    {
        get { return RuntimeInformation.IsOSPlatform(OSPlatform.Linux); }
    }

    /// <summary>
    /// Represents a property that gets the handle of the native window on a Windows platform.
    /// </summary>
    /// <remarks>
    /// Only available on the Windows platform.
    /// If this property is accessed from a non-Windows platform, a PlatformNotSupportedException will be thrown.
    /// If this property is accessed before the window is initialized, an ApplicationException will be thrown.
    /// </remarks>
    /// <value>
    /// The handle of the native window. The value is of type <see cref="IntPtr"/>.
    /// </value>
    /// <exception cref="System.ApplicationException">Thrown when the window is not initialized yet.</exception>
    /// <exception cref="System.PlatformNotSupportedException">Thrown when accessed from a non-Windows platform.</exception>
    public IntPtr WindowHandle
    {
        get
        {
            if (IsWindowsPlatform)
            {
                if (_instance is null)
                {
                    throw new ApplicationException("The Photino window is not initialized yet");
                }

                var handle = IntPtr.Zero;
                Invoke(() => handle = ((WindowsPhotino) _instance).GetHwnd());
                return handle;
            }
            else
            {
                throw new PlatformNotSupportedException($"{nameof(WindowHandle)} is only supported on Windows.");
            }
        }
    }

    /// <summary>
    /// Gets list of information for each monitor from the native window.
    /// This property represents a list of Monitor objects associated to each display monitor.
    /// </summary>
    /// <remarks>
    /// If called when the native instance of the window is not initialized, it will throw an ApplicationException.
    /// </remarks>
    /// <exception cref="ApplicationException">Thrown when the native instance of the window is not initialized.</exception>
    /// <returns>
    /// A read-only list of Monitor objects representing information about each display monitor.
    /// </returns>
    public IReadOnlyList<Monitor> Monitors
    {
        get
        {
            if (_instance is null)
            {
                throw new ApplicationException("The Photino window hasn't been initialized yet.");
            }

            List<Monitor> monitors = new();
            Invoke(() => monitors = _instance.GetAllMonitors());
            return monitors;
        }
    }

    /// <summary>
    /// Retrieves the primary monitor information from the native window instance.
    /// </summary>
    /// <exception cref="ApplicationException"> Thrown when the window hasn't been initialized yet. </exception>
    /// <returns>
    /// Returns a Monitor object representing the main monitor. The main monitor is the first monitor in the list of available monitors.
    /// </returns>
    public Monitor MainMonitor
    {
        get
        {
            if (_instance is null)
            {
                throw new ApplicationException("The Photino window hasn't been initialized yet.");
            }

            return Monitors[0];
        }
    }

    /// <summary>
    /// Gets the dots per inch (DPI) for the primary display from the native window.
    /// </summary>
    /// <exception cref="ApplicationException">
    /// An ApplicationException is thrown if the window hasn't been initialized yet.
    /// </exception>
    public uint ScreenDpi
    {
        get
        {
            if (_instance is null)
            {
                throw new ApplicationException("The Photino window hasn't been initialized yet.");
            }

            uint dpi = 0;
            Invoke(() => dpi = _instance.GetScreenDpi());
            return dpi;
        }
    }

    /// <summary>
    /// Gets a unique GUID to identify the native window.
    /// </summary>
    /// <remarks>
    /// This property is not currently utilized by the Photino framework.
    /// </remarks>
    public Guid Id { get; } = Guid.NewGuid();

    #endregion

    #region get-set Properties

    /// <summary>
    /// When true, the native window will appear centered on the screen. By default, this is set to false.
    /// </summary>
    /// <exception cref="ApplicationException">
    /// Thrown if trying to set value after native window is initalized.
    /// </exception>
    [SupportedOSPlatform("windows")]
    public bool Centered
    {
        get
        {
            if (_instance is null)
            {
                return _startupParameters.CenterOnInitialize;
            }

            return false;
        }
        set
        {
            if (_instance is null)
            {
                _startupParameters.CenterOnInitialize = value;
            }
            else
            {
                Invoke(() => ((WindowsPhotino) _instance).Center());
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the native window should be chromeless.
    /// When true, the native window will appear without a title bar or border.
    /// By default, this is set to false.
    /// </summary>
    /// <exception cref="ApplicationException">
    /// Thrown if trying to set value after native window is initalized.
    /// </exception>
    /// <remarks>
    /// The user has to supply titlebar, border, dragging and resizing manually.
    /// </remarks>
    public bool Chromeless
    {
        get { return _startupParameters.Chromeless; }
        set
        {
            if (_instance is null)
            {
                _startupParameters.Chromeless = value;
            }
            else
            {
                throw new ApplicationException("Chromeless can only be set before the native window is instantiated.");
            }
        }
    }

    /// <summary>
    /// When true, the native window and browser control can be displayed with transparent background.
    /// Html document's body background must have alpha-based value.
    /// WebView2 on Windows can only be fully transparent or fully opaque.
    /// By default, this is set to false.
    /// </summary>
    /// <exception cref="ApplicationException">
    /// On Windows, thrown if trying to set value after native window is initalized.
    /// </exception>
    public bool Transparent
    {
        get
        {
            if (_instance is null)
            {
                return _startupParameters.Transparent;
            }

            var enabled = false;
            Invoke(() => enabled = _instance.GetTransparentEnabled());
            return enabled;
        }
        set
        {
            if (Transparent != value)
            {
                if (_instance is null)
                {
                    _startupParameters.Transparent = value;
                }
                else
                {
                    if (IsWindowsPlatform)
                    {
                        throw new ApplicationException("Transparent can only be set on Windows before the native window is instantiated.");
                    }
                    else
                    {
                        Log($"Invoking Photino_SetTransparentEnabled({value})");
                        Invoke(() => _instance.SetTransparentEnabled(value));
                    }
                }
            }
        }
    }

    /// <summary>
    /// When true, the user can access the browser control's context menu.
    /// By default, this is set to true.
    /// </summary>
    public bool ContextMenuEnabled
    {
        get
        {
            if (_instance is null)
            {
                return _startupParameters.ContextMenuEnabled;
            }

            var enabled = false;
            Invoke(() => enabled = _instance.GetContextMenuEnabled());
            return enabled;
        }
        set
        {
            if (ContextMenuEnabled != value)
            {
                if (_instance is null)
                {
                    _startupParameters.ContextMenuEnabled = value;
                }
                else
                {
                    Invoke(() => _instance.SetContextMenuEnabled(value));
                }
            }
        }
    }

    /// <summary>
    /// When true, the user can access the browser control's developer tools.
    /// By default, this is set to true.
    /// </summary>
    public bool DevToolsEnabled
    {
        get
        {
            if (_instance is null)
            {
                return _startupParameters.DevToolsEnabled;
            }

            var enabled = false;
            Invoke(() => enabled = _instance.GetDevToolsEnabled());
            return enabled;
        }
        set
        {
            if (DevToolsEnabled != value)
            {
                if (_instance is null)
                {
                    _startupParameters.DevToolsEnabled = value;
                }
                else
                {
                    Invoke(() => _instance.SetDevToolsEnabled(value));
                }
            }
        }
    }

    public bool MediaAutoplayEnabled
    {
        get
        {
            if (_instance is null)
            {
                return _startupParameters.MediaAutoplayEnabled;
            }

            var enabled = false;
            Invoke(() => enabled = _instance.GetMediaAutoplayEnabled());
            return enabled;
        }
        set
        {
            if (MediaAutoplayEnabled != value)
            {
                if (_instance is null)
                {
                    _startupParameters.MediaAutoplayEnabled = value;
                }
                else
                {
                    throw new ApplicationException("MediaAutoplayEnabled can only be set before the native window is instantiated.");
                }
            }
        }
    }

    public string UserAgent
    {
        get
        {
            if (_instance is null)
            {
                return _startupParameters.UserAgent;
            }

            var userAgent = string.Empty;
            Invoke(() => { userAgent = _instance.GetUserAgent(); });
            return userAgent;
        }
        set
        {
            if (UserAgent != value)
            {
                if (_instance is null)
                {
                    _startupParameters.UserAgent = value;
                }
                else
                {
                    throw new ApplicationException("UserAgent can only be set before the native window is instantiated.");
                }
            }
        }
    }

    public bool FileSystemAccessEnabled
    {
        get
        {
            if (_instance is null)
            {
                return _startupParameters.FileSystemAccessEnabled;
            }

            var enabled = false;
            Invoke(() => enabled = _instance.GetFileSystemAccessEnabled());
            return enabled;
        }
        set
        {
            if (FileSystemAccessEnabled != value)
            {
                if (_instance is null)
                {
                    _startupParameters.FileSystemAccessEnabled = value;
                }
                else
                {
                    throw new ApplicationException("FileSystemAccessEnabled can only be set before the native window is instantiated.");
                }
            }
        }
    }

    public bool WebSecurityEnabled
    {
        get
        {
            if (_instance is null)
            {
                return _startupParameters.WebSecurityEnabled;
            }

            var enabled = true;
            Invoke(() => enabled = _instance.GetWebSecurityEnabled());
            return enabled;
        }
        set
        {
            if (WebSecurityEnabled != value)
            {
                if (_instance is null)
                {
                    _startupParameters.WebSecurityEnabled = value;
                }
                else
                {
                    throw new ApplicationException("WebSecurityEnabled can only be set before the native window is instantiated.");
                }
            }
        }
    }

    public bool JavascriptClipboardAccessEnabled
    {
        get
        {
            if (_instance is null)
            {
                return _startupParameters.JavascriptClipboardAccessEnabled;
            }

            var enabled = true;
            Invoke(() => enabled = _instance.GetJavascriptClipboardAccessEnabled());
            return enabled;
        }
        set
        {
            if (JavascriptClipboardAccessEnabled != value)
            {
                if (_instance is null)
                {
                    _startupParameters.JavascriptClipboardAccessEnabled = value;
                }
                else
                {
                    throw new ApplicationException(
                        "JavascriptClipboardAccessEnabled can only be set before the native window is instantiated.");
                }
            }
        }
    }

    public bool MediaStreamEnabled
    {
        get
        {
            if (_instance is null)
            {
                return _startupParameters.MediaStreamEnabled;
            }

            var enabled = true;
            Invoke(() => enabled = _instance.GetMediaStreamEnabled());
            return enabled;
        }
        set
        {
            if (MediaStreamEnabled != value)
            {
                if (_instance is null)
                {
                    _startupParameters.MediaStreamEnabled = value;
                }
                else
                {
                    throw new ApplicationException("MediaStreamEnabled can only be set before the native window is instantiated.");
                }
            }
        }
    }

    public bool SmoothScrollingEnabled
    {
        get
        {
            if (_instance is null)
            {
                return _startupParameters.SmoothScrollingEnabled;
            }

            var enabled = false;
            Invoke(() => enabled = _instance.GetSmoothScrollingEnabled());
            return enabled;
        }
        set
        {
            if (SmoothScrollingEnabled != value)
            {
                if (_instance is null)
                {
                    _startupParameters.SmoothScrollingEnabled = value;
                }
                else
                {
                    throw new ApplicationException("SmoothScrollingEnabled can only be set before the native window is instantiated.");
                }
            }
        }
    }

    public bool IgnoreCertificateErrorsEnabled
    {
        get
        {
            if (_instance is null)
            {
                return _startupParameters.IgnoreCertificateErrorsEnabled;
            }

            var enabled = false;
            Invoke(() => enabled = _instance.GetIgnoreCertificateErrorsEnabled());
            return enabled;
        }
        set
        {
            if (IgnoreCertificateErrorsEnabled != value)
            {
                if (_instance is null)
                {
                    _startupParameters.IgnoreCertificateErrorsEnabled = value;
                }
                else
                {
                    throw new ApplicationException(
                        "IgnoreCertificateErrorsEnabled can only be set before the native window is instantiated.");
                }
            }
        }
    }

    public bool NotificationsEnabled
    {
        get
        {
            if (_instance is null)
            {
                return _startupParameters.NotificationsEnabled;
            }

            var enabled = false;
            Invoke(() => enabled = _instance.GetNotificationsEnabled());
            return enabled;
        }
        set
        {
            if (NotificationsEnabled != value)
            {
                if (_instance is null)
                {
                    _startupParameters.NotificationsEnabled = value;
                }
                else
                {
                    throw new ApplicationException("NotificationsEnabled can only be set before the native window is instantiated.");
                }
            }
        }
    }


    /// <summary>
    /// This property returns or sets the fullscreen status of the window.
    /// When set to true, the native window will cover the entire screen, similar to kiosk mode.
    /// By default, this is set to false.
    /// </summary>
    public bool FullScreen
    {
        get
        {
            if (_instance is null)
            {
                return _startupParameters.FullScreen;
            }

            var fullScreen = false;
            Invoke(() => fullScreen = _instance.GetFullScreen());
            return fullScreen;
        }
        set
        {
            if (FullScreen != value)
            {
                if (_instance is null)
                {
                    _startupParameters.FullScreen = value;
                }
                else
                {
                    Invoke(() => _instance.SetFullScreen(value));
                }
            }
        }
    }

    ///<summary>
    /// Gets or Sets whether the native browser control grants all requests for access to local resources
    /// such as the users camera and microphone. By default, this is set to true.
    /// </summary>
    /// <remarks>
    /// This only works on Windows.
    /// </remarks>
    public bool GrantBrowserPermissions
    {
        get
        {
            if (_instance is null)
            {
                return _startupParameters.GrantBrowserPermissions;
            }

            var grant = false;
            Invoke(() => grant = _instance.GetGrantBrowserPermissions());
            return grant;
        }
        set
        {
            if (GrantBrowserPermissions != value)
            {
                if (_instance is null)
                {
                    _startupParameters.GrantBrowserPermissions = value;
                }
                else
                {
                    throw new ApplicationException("GrantBrowserPermissions can only be set before the native window is instantiated.");
                }
            }
        }
    }

    /// /// <summary>
    /// Gets or Sets the Height property of the native window in pixels.
    /// Default value is 0.
    /// </summary>
    /// <seealso cref="UseOsDefaultSize" />
    public int Height
    {
        get { return Size.Height; }
        set
        {
            var currentSize = Size;
            if (currentSize.Height != value)
            {
                Size = new Size(currentSize.Width, value);
            }
        }
    }

    private string? _iconFile;

    /// <summary>
    /// Gets or sets the icon file for the native window title bar.
    /// The file must be located on the local machine and cannot be a URL. The default is none.
    /// </summary>
    /// <remarks>
    /// This only works on Windows and Linux.
    /// </remarks>
    /// <value>
    /// The file path to the icon.
    /// </value>
    /// <exception cref="System.ArgumentException">Icon file: {value} does not exist.</exception>
    public string? IconFile
    {
        get { return _iconFile; }
        set
        {
            if (_iconFile != value)
            {
                _iconFile = value;

                if (_instance is null)
                {
                    _startupParameters.WindowIconFile = _iconFile!;
                }
                else
                {
                    Invoke(() => _instance.SetIconFile(_iconFile));
                }
            }
        }
    }

    /// <summary>
    /// Gets or sets the native window Left (X) and Top coordinates (Y) in pixels.
    /// Default is 0,0 which means the window will be aligned to the top left edge of the screen.
    /// </summary>
    /// <seealso cref="UseOsDefaultLocation" />
    public Point Location
    {
        get
        {
            if (_instance is null)
            {
                return new Point(_startupParameters.Left, _startupParameters.Top);
            }

            Point position = default;
            Invoke(() => position = _instance.GetPosition());
            return position;
        }
        set
        {
            if (Location.X != value.X || Location.Y != value.Y)
            {
                if (_instance is null)
                {
                    _startupParameters.Left = value.X;
                    _startupParameters.Top = value.Y;
                }
                else
                {
                    Invoke(() => _instance.SetPosition(Location));
                }
            }
        }
    }

    /// <summary>
    /// Gets or sets the native window Left (X) coordinate in pixels.
    /// This represents the horizontal position of the window relative to the screen.
    /// Default value is 0 which means the window will be aligned to the left edge of the screen.
    /// </summary>
    /// <seealso cref="UseOsDefaultLocation" />
    public int Left
    {
        get { return Location.X; }
        set
        {
            if (Location.X != value)
            {
                Location = new Point(value, Location.Y);
            }
        }
    }

    /// <summary>
    /// Gets or sets whether the native window is maximized.
    /// Default is false.
    /// </summary>
    public bool Maximized
    {
        get
        {
            if (_instance is null)
            {
                return _startupParameters.Maximized;
            }

            var maximized = false;
            Invoke(() => maximized = _instance.GetMaximized());
            return maximized;
        }
        set
        {
            if (Maximized != value)
            {
                if (_instance is null)
                {
                    _startupParameters.Maximized = value;
                }
                else
                {
                    Invoke(() => _instance.SetMaximized(value));
                }
            }
        }
    }

    ///<summary>Gets or set the maximum size of the native window in pixels.</summary>
    /// TODO: change to use Size?
    public Point MaxSize
    {
        get { return new Point(MaxWidth, MaxHeight); }
        set
        {
            if (MaxWidth != value.X || MaxHeight != value.Y)
            {
                if (_instance is null)
                {
                    _startupParameters.MaxWidth = value.X;
                    _startupParameters.MaxHeight = value.Y;
                }
                else
                {
                    Invoke(() => ((WindowsPhotino) _instance).SetMaxSize(new Size(value.X, value.Y)));
                }
            }
        }
    }

    ///<summary>Gets or sets the native window maximum height in pixels.</summary>
    private int _maxHeight = int.MaxValue;

    public int MaxHeight
    {
        get { return _maxHeight; }
        set
        {
            if (_maxHeight != value)
            {
                MaxSize = new Point(MaxSize.X, value);
                _maxHeight = value;
            }
        }
    }

    ///<summary>Gets or sets the native window maximum height in pixels.</summary>
    private int _maxWidth = int.MaxValue;

    public int MaxWidth
    {
        get { return _maxWidth; }
        set
        {
            if (_maxWidth != value)
            {
                MaxSize = new Point(value, MaxSize.Y);
                _maxWidth = value;
            }
        }
    }

    /// <summary>
    /// Gets or sets whether the native window is minimized (hidden).
    /// Default is false.
    /// </summary>
    public bool Minimized
    {
        get
        {
            if (_instance is null)
            {
                return _startupParameters.Minimized;
            }

            var minimized = false;
            Invoke(() => minimized = _instance.GetMinimized());
            return minimized;
        }
        set
        {
            if (Minimized != value)
            {
                if (_instance is null)
                {
                    _startupParameters.Minimized = value;
                }
                else
                {
                    Invoke(() => _instance.SetMinimized(value));
                }
            }
        }
    }

    ///<summary>Gets or set the minimum size of the native window in pixels.</summary>
    /// /// TODO: change to use Size?
    public Point MinSize
    {
        get { return new Point(MinWidth, MinHeight); }
        set
        {
            if (MinWidth != value.X || MinHeight != value.Y)
            {
                if (_instance is null)
                {
                    _startupParameters.MinWidth = value.X;
                    _startupParameters.MinHeight = value.Y;
                }
                else
                {
                    Invoke(() => ((WindowsPhotino) _instance).SetMinSize(new Size(value.X, value.Y)));
                }
            }
        }
    }

    ///<summary>Gets or sets the native window minimum height in pixels.</summary>
    private int _minHeight;

    public int MinHeight
    {
        get { return _minHeight; }
        set
        {
            if (_minHeight != value)
            {
                MinSize = new Point(MinSize.X, value);
                _minHeight = value;
            }
        }
    }

    ///<summary>Gets or sets the native window minimum height in pixels.</summary>
    private int _minWidth;

    public int MinWidth
    {
        get { return _minWidth; }
        set
        {
            if (_minWidth != value)
            {
                MinSize = new Point(value, MinSize.Y);
                _minWidth = value;
            }
        }
    }

    private readonly PhotinoWindow? _dotNetParent;

    /// <summary>
    /// Gets the reference to parent PhotinoWindow instance.
    /// This property can only be set in the constructor and it is optional.
    /// </summary>
    public PhotinoWindow? Parent
    {
        get { return _dotNetParent; }
    }

    /// <summary>
    /// Gets or sets whether the native window can be resized by the user.
    /// Default is true.
    /// </summary>
    public bool Resizable
    {
        get
        {
            if (_instance is null)
            {
                return _startupParameters.Resizable;
            }

            var resizable = false;
            Invoke(() => resizable = _instance.GetResizable());
            return resizable;
        }
        set
        {
            if (Resizable != value)
            {
                if (_instance is null)
                {
                    _startupParameters.Resizable = value;
                }
                else
                {
                    Invoke(() => _instance.SetResizable(value));
                }
            }
        }
    }

    /// <summary>
    /// Gets or sets the native window Size. This represents the width and the height of the window in pixels.
    /// The default Size is 0,0.
    /// </summary>
    /// <seealso cref="UseOsDefaultSize"/>
    public Size Size
    {
        get
        {
            if (_instance is null)
            {
                return new Size(_startupParameters.Width, _startupParameters.Height);
            }

            Size size = new Size();
            Invoke(() => size = _instance.GetSize());
            return size;
        }
        set
        {
            if (Size.Width != value.Width || Size.Height != value.Height)
            {
                if (_instance is null)
                {
                    _startupParameters.Height = value.Height;
                    _startupParameters.Width = value.Width;
                }
                else
                {
                    Invoke(() => _instance.SetSize(value));
                }
            }
        }
    }

    /// <summary>
    /// Gets or sets platform specific initialization parameters for the native browser control on startup.
    /// Default is none.
    ///WINDOWS: WebView2 specific string. Space separated.
    ///https://peter.sh/experiments/chromium-command-line-switches/
    ///https://learn.microsoft.com/en-us/dotnet/api/microsoft.web.webview2.core.corewebview2environmentoptions.additionalbrowserarguments?view=webview2-dotnet-1.0.1938.49&viewFallbackFrom=webview2-dotnet-1.0.1901.177view%3Dwebview2-1.0.1901.177
    ///https://www.chromium.org/developers/how-tos/run-chromium-with-flags/
    ///LINUX: Webkit2Gtk specific string. Enter parameter names and values as JSON string.
    ///e.g. { "set_enable_encrypted_media": true }
    ///https://webkitgtk.org/reference/webkit2gtk/2.5.1/WebKitSettings.html
    ///https://lazka.github.io/pgi-docs/WebKit2-4.0/classes/Settings.html
    ///MAC: Webkit specific string. Enter parameter names and values as JSON string.
    ///e.g. { "minimumFontSize": 8 }
    ///https://developer.apple.com/documentation/webkit/wkwebviewconfiguration?language=objc
    ///https://developer.apple.com/documentation/webkit/wkpreferences?language=objc
    /// </summary>
    public string BrowserControlInitParameters
    {
        get { return _startupParameters.BrowserControlInitParameters; }
        set
        {
            var parameters = _startupParameters.BrowserControlInitParameters;
            if (String.Compare(parameters, value, StringComparison.OrdinalIgnoreCase) != 0)
            {
                if (_instance is null)
                {
                    _startupParameters.BrowserControlInitParameters = value;
                }
                else
                {
                    throw new ApplicationException($"{nameof(parameters)} cannot be changed after Photino Window is initialized");
                }
            }
        }
    }

    /// <summary>
    /// Gets or sets an HTML string that the browser control will render when initialized.
    /// Default is none.
    /// </summary>
    /// <remarks>
    /// Either StartString or StartUrl must be specified.
    /// </remarks>
    /// <seealso cref="StartUrl" />
    /// <exception cref="ApplicationException">
    /// Thrown if trying to set value after native window is initalized.
    /// </exception>
    public string StartString
    {
        get { return _startupParameters.StartString; }
        set
        {
            var ss = _startupParameters.StartString;
            if (String.Compare(ss, value, StringComparison.OrdinalIgnoreCase) != 0)
            {
                if (_instance is not null)
                {
                    throw new ApplicationException($"{nameof(ss)} cannot be changed after Photino Window is initialized");
                }

                LoadRawString(value);
            }
        }
    }

    /// <summary>
    /// Gets or sets an URL that the browser control will navigate to when initialized.
    /// Default is none.
    /// </summary>
    /// <remarks>
    /// Either StartString or StartUrl must be specified.
    /// </remarks>
    /// <seealso cref="StartString" />
    /// <exception cref="ApplicationException">
    /// Thrown if trying to set value after native window is initalized.
    /// </exception>
    public string StartUrl
    {
        get { return _startupParameters.StartUrl; }
        set
        {
            var su = _startupParameters.StartUrl;
            if (String.Compare(su, value, StringComparison.OrdinalIgnoreCase) != 0)
            {
                if (_instance is not null)
                {
                    throw new ApplicationException($"{nameof(su)} cannot be changed after Photino Window is initialized");
                }

                Load(value);
            }
        }
    }

    /// <summary>
    /// Gets or sets the local path to store temp files for browser control.
    /// Default is the user's AppDataLocal folder.
    /// </summary>
    /// <remarks>
    /// Only available on Windows.
    /// </remarks>
    /// <exception cref="ApplicationException">
    /// Thrown if platform is not Windows.
    /// </exception>
    public string? TemporaryFilesPath
    {
        get { return _startupParameters.TemporaryFilesPath; }
        set
        {
            var tfp = _startupParameters.TemporaryFilesPath;
            if (tfp != value)
            {
                if (_instance is not null)
                {
                    throw new ApplicationException($"{nameof(tfp)} cannot be changed after Photino Window is initialized");
                }

                _startupParameters.TemporaryFilesPath = value;
            }
        }
    }

    /// <summary>
    /// Gets or sets the registration Id for doing toast notifications.
    /// Default is to use the window title.
    /// </summary>
    /// <remarks>
    /// Only available on Windows.
    /// </remarks>
    /// <exception cref="ApplicationException">
    /// Thrown if platform is not Windows.
    /// </exception>
    public string NotificationRegistrationId
    {
        get { return _startupParameters.NotificationRegistrationId; }
        set
        {
            var nri = _startupParameters.NotificationRegistrationId;
            if (nri != value)
            {
                if (_instance is not null)
                {
                    throw new ApplicationException($"{nameof(nri)} cannot be changed after Photino Window is initialized");
                }

                _startupParameters.NotificationRegistrationId = value;
            }
        }
    }

    /// <summary>
    /// Gets or sets the native window title.
    /// Default is "Photino".
    /// </summary>
    public string Title
    {
        get
        {
            if (_instance is null)
            {
                return _startupParameters.Title;
            }

            var title = string.Empty;
            Invoke(() => title = _instance.GetTitle());
            return title;
        }
        set
        {
            if (Title != value)
            {
                // Due to Linux/Gtk platform limitations, the window title has to be no more than 31 chars
                if (value.Length > 31 && IsLinuxPlatform)
                {
                    value = value[..31];
                }

                if (_instance is null)
                {
                    _startupParameters.Title = value;
                }
                else
                {
                    Invoke(() => _instance.SetTitle(value));
                }
            }
        }
    }

    /// <summary>
    /// Gets or sets the native window Top (Y) coordinate in pixels.
    /// Default is 0.
    /// </summary>
    /// <seealso cref="UseOsDefaultLocation"/>
    public int Top
    {
        get { return Location.Y; }
        set
        {
            if (Location.Y != value)
            {
                Location = new Point(Location.X, value);
            }
        }
    }

    public bool Darkmode
    {
        get
        {
            if (_instance is null)
            {
                return _startupParameters.Darkmode;
            }

            var darkmode = false;
            Invoke(() => darkmode = _instance.GetDarkmodeEnabled());
            return darkmode;
        }
        set
        {
            if (Darkmode != value)
            {
                if (_instance is null)
                {
                    _startupParameters.Darkmode = value;
                }
                else
                {
                    Invoke(() => _instance.SetDarkmodeEnabled(value));
                }
            }
        }
    }

    /// <summary>
    /// Gets or sets whether the native window is always at the top of the z-order.
    /// Default is false.
    /// </summary>
    public bool Topmost
    {
        get
        {
            if (_instance is null)
            {
                return _startupParameters.Topmost;
            }

            var topmost = false;
            Invoke(() => topmost = _instance.GetTopmost());
            return topmost;
        }
        set
        {
            if (Topmost != value)
            {
                if (_instance is null)
                {
                    _startupParameters.Topmost = value;
                }
                else
                {
                    Invoke(() => _instance.SetTopmost(value));
                }
            }
        }
    }

    /// <summary>
    /// When true the native window starts up at the OS Default location.
    /// Default is true.
    /// </summary>
    /// <remarks>
    /// Overrides Left (X) and Top (Y) properties.
    /// </remarks>
    /// <exception cref="ApplicationException">
    /// Thrown if trying to set value after native window is initalized.
    /// </exception>
    public bool UseOsDefaultLocation
    {
        get { return _startupParameters.UseOsDefaultLocation; }
        set
        {
            if (_instance is null)
            {
                if (UseOsDefaultLocation != value)
                {
                    _startupParameters.UseOsDefaultLocation = value;
                }
            }
            else
            {
                throw new ApplicationException("UseOsDefaultLocation can only be set before the native window is instantiated.");
            }
        }
    }

    /// <summary>
    /// When true the native window starts at the OS Default size.
    /// Default is true.
    /// </summary>
    /// <remarks>
    /// Overrides Height and Width properties.
    /// </remarks>
    /// <exception cref="ApplicationException">
    /// Thrown if trying to set value after native window is initalized.
    /// </exception>
    public bool UseOsDefaultSize
    {
        get { return _startupParameters.UseOsDefaultSize; }
        set
        {
            if (_instance is null)
            {
                if (UseOsDefaultSize != value)
                {
                    _startupParameters.UseOsDefaultSize = value;
                }
            }
            else
            {
                throw new ApplicationException("UseOsDefaultSize can only be set before the native window is instantiated.");
            }
        }
    }

    /// <summary>
    /// Gets or Sets the native window width in pixels.
    /// Default is 0.
    /// </summary>
    /// <seealso cref="UseOsDefaultSize"/>
    public int Width
    {
        get { return Size.Width; }
        set
        {
            var currentSize = Size;
            if (currentSize.Width != value)
            {
                Size = new Size(value, currentSize.Height);
            }
        }
    }

    /// <summary>
    /// Gets or sets handlers for WebMessageReceived event.
    /// Set assigns a new handler to the event.
    /// </summary>
    /// <seealso cref="WebMessageReceived"/>
    public EventHandler<string>? WebMessageReceivedHandler
    {
        get { return WebMessageReceived; }
        set { WebMessageReceived += value; }
    }

    /// <summary>
    /// Gets or sets the handlers for WindowClosing event.
    /// Set assigns a new handler to the event.
    /// </summary>
    /// <seealso cref="WindowClosing" />
    public NetClosingDelegate? WindowClosingHandler
    {
        get { return WindowClosing; }
        set { WindowClosing += value; }
    }

    /// <summary>
    /// Gets or sets handlers for WindowCreating event.
    /// Set assigns a new handler to the event.
    /// </summary>
    /// <seealso cref="WindowCreating"/>
    public EventHandler? WindowCreatingHandler
    {
        get { return WindowCreating; }
        set { WindowCreating += value; }
    }

    /// <summary>
    /// Gets or sets handlers for WindowCreated event.
    /// Set assigns a new handler to the event.
    /// </summary>
    /// <seealso cref="WindowCreated"/>
    public EventHandler? WindowCreatedHandler
    {
        get { return WindowCreated; }
        set { WindowCreated += value; }
    }

    /// <summary>
    /// Gets or sets handlers for WindowLocationChanged event.
    /// Set assigns a new handler to the event.
    /// </summary>
    /// <seealso cref="WindowLocationChanged"/>
    public EventHandler<Point>? WindowLocationChangedHandler
    {
        get { return WindowLocationChanged; }
        set { WindowLocationChanged += value; }
    }

    /// <summary>
    /// Gets or sets handlers for WindowSizeChanged event.
    /// Set assigns a new handler to the event.
    /// </summary>
    /// <seealso cref="WindowSizeChanged"/>
    public EventHandler<Size>? WindowSizeChangedHandler
    {
        get { return WindowSizeChanged; }
        set { WindowSizeChanged += value; }
    }

    /// <summary>
    /// Gets or sets handlers for WindowFocusIn event.
    /// Set assigns a new handler to the event.
    /// </summary>
    /// <seealso cref="WindowFocusIn"/>
    public EventHandler? WindowFocusInHandler
    {
        get { return WindowFocusIn; }
        set { WindowFocusIn += value; }
    }

    /// <summary>
    /// Gets or sets handlers for WindowFocusOut event.
    /// Set assigns a new handler to the event.
    /// </summary>
    /// <seealso cref="WindowFocusOut"/>
    public EventHandler? WindowFocusOutHandler
    {
        get { return WindowFocusOut; }
        set { WindowFocusOut += value; }
    }

    /// <summary>
    /// Gets or sets handlers for WindowMaximized event.
    /// Set assigns a new handler to the event.
    /// </summary>
    /// <seealso cref="WindowMaximized"/>
    public EventHandler? WindowMaximizedHandler
    {
        get { return WindowMaximized; }
        set { WindowMaximized += value; }
    }

    /// <summary>
    /// Gets or sets handlers for WindowRestored event.
    /// Set assigns a new handler to the event.
    /// </summary>
    /// <seealso cref="WindowRestored"/>
    public EventHandler? WindowRestoredHandler
    {
        get { return WindowRestored; }
        set { WindowRestored += value; }
    }

    /// <summary>
    /// Gets or sets handlers for WindowMinimized event.
    /// Set assigns a new handler to the event.
    /// </summary>
    /// <seealso cref="WindowMinimized"/>
    public EventHandler? WindowMinimizedHandler
    {
        get { return WindowMinimized; }
        set { WindowMinimized += value; }
    }

    /// <summary>
    /// Gets or sets the native browser control <see cref="PhotinoWindow.Zoom"/>.
    /// Default is 100.
    /// </summary>
    /// <example>100 = 100%, 50 = 50%</example>
    public int Zoom
    {
        get
        {
            if (_instance is null)
            {
                return _startupParameters.Zoom;
            }

            var zoom = 0;
            Invoke(() => zoom = _instance.GetZoom());
            return zoom;
        }
        set
        {
            if (Zoom != value)
            {
                if (_instance is null)
                {
                    _startupParameters.Zoom = value;
                }
                else
                {
                    Invoke(() => _instance.SetZoom(value));
                }
            }
        }
    }

    /// <summary>
    /// Gets or sets the logging verbosity to standard output (Console/Terminal).
    /// 0 = Critical Only
    /// 1 = Critical and Warning
    /// 2 = Verbose
    /// >2 = All Details
    /// Default is 2.
    /// </summary>
    public int LogVerbosity { get; set; } = 2;

    #endregion

    #region Constuctor

    /// <summary>
    /// Initializes a new instance of the PhotinoWindow class.
    /// </summary>
    /// <remarks>
    /// This class represents a native window with a native browser control taking up the entire client area.
    /// If a parent window is specified, this window will be created as a child of the specified parent window.
    /// </remarks>
    /// <param name="parent">The parent PhotinoWindow. This is optional and defaults to null.</param>
    public PhotinoWindow(PhotinoWindow? parent = null)
    {
        _dotNetParent = parent;
        _managedThreadId = Environment.CurrentManagedThreadId;

        _startupParameters.ClosingHandler = OnWindowClosing;
        _startupParameters.ResizedHandler = OnSizeChanged;
        _startupParameters.MaximizedHandler = OnMaximized;
        _startupParameters.RestoredHandler = OnRestored;
        _startupParameters.MinimizedHandler = OnMinimized;
        _startupParameters.MovedHandler = OnLocationChanged;
        _startupParameters.FocusInHandler = OnFocusIn;
        _startupParameters.FocusOutHandler = OnFocusOut;
        _startupParameters.WebMessageRecievedHandler = OnWebMessageReceived;
        _startupParameters.CustomSchemeHandler = OnCustomScheme;
    }

    #endregion

    #region Methods

    //FLUENT METHODS FOR INITIALIZING STARTUP PARAMETERS FOR NEW WINDOWS
    //CAN ALSO BE CALLED AFTER INITIALIZATION TO SET VALUES
    //ONE OF THESE 3 METHODS *MUST* BE CALLED PRIOR TO CALLING WAITFORCLOSE() OR CREATECHILDWINDOW()

    /// <summary>
    /// Dispatches an Action to the UI thread if called from another thread.
    /// </summary>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <param name="workItem">The delegate encapsulating a method / action to be executed in the UI thread.</param>
    public PhotinoWindow Invoke(Action workItem)
    {
        // If we're already on the UI thread, no need to dispatch
        if (Environment.CurrentManagedThreadId == _managedThreadId)
        {
            workItem();
        }
        else
        {
            _instance!.Invoke(workItem);
        }

        return this;
    }

    /// <summary>
    /// Loads a specified <see cref="Uri"/> into the browser control.
    /// </summary>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <remarks>
    /// Load() or LoadString() must be called before native window is initialized.
    /// </remarks>
    /// <param name="uri">A Uri pointing to the file or the URL to load.</param>
    public PhotinoWindow Load(Uri uri)
    {
        Log($".Load({uri})");
        if (_instance is null)
        {
            _startupParameters.StartUrl = uri.ToString();
        }
        else
        {
            Invoke(() => _instance.NavigateToUrl(uri.ToString()));
        }

        return this;
    }

    /// <summary>
    /// Loads a specified path into the browser control.
    /// </summary>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <remarks>
    /// Load() or LoadString() must be called before native window is initialized.
    /// </remarks>
    /// <param name="path">A path pointing to the ressource to load.</param>
    public PhotinoWindow Load(string path)
    {
        Log($".Load({path})");

        // ––––––––––––––––––––––
        // SECURITY RISK!
        // This needs validation!
        // ––––––––––––––––––––––
        // Open a web URL string path
        if (path.Contains("http://") || path.Contains("https://"))
        {
            return Load(new Uri(path));
        }

        // Open a file resource string path
        var absolutePath = Path.GetFullPath(path);

        // For bundled app it can be necessary to consider
        // the app context base directory. Check there too.
        if (!File.Exists(absolutePath))
        {
            absolutePath = $"{AppContext.BaseDirectory}/{path}";

            if (!File.Exists(absolutePath))
            {
                Log($" ** File \"{path}\" could not be found.");
                return this;
            }
        }

        return Load(new Uri(absolutePath, UriKind.Absolute));
    }

    /// <summary>
    /// Loads a raw string into the browser control.
    /// </summary>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <remarks>
    /// Used to load HTML into the browser control directly.
    /// Load() or LoadString() must be called before native window is initialized.
    /// </remarks>
    /// <param name="content">Raw content (such as HTML)</param>
    public PhotinoWindow LoadRawString(string content)
    {
        var shortContent = content.Length > 50 ? string.Concat(content.AsSpan(0, 50), "...") : content;
        Log($".LoadRawString({shortContent})");
        if (_instance is null)
        {
            _startupParameters.StartString = content;
        }
        else
        {
            Invoke(() => _instance.NavigateToString(content));
        }

        return this;
    }

    /// <summary>
    /// Centers the native window on the primary display.
    /// </summary>
    /// <remarks>
    /// If called prior to window initialization, overrides Left (X) and Top (Y) properties.
    /// </remarks>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <seealso cref="UseOsDefaultLocation" />
    public PhotinoWindow Center()
    {
        Log(".Center()");
        // Invoke(() => _instance.Center());
        return this;
    }

    /// <summary>
    /// Moves the native window to the specified location on the screen in pixels using a Point.
    /// </summary>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <param name="location">Position as <see cref="Point"/></param>
    /// <param name="allowOutsideWorkArea">Whether the window can go off-screen (work area)</param>
    public PhotinoWindow MoveTo(Point location, bool allowOutsideWorkArea = false)
    {
        Log($".MoveTo({location}, {allowOutsideWorkArea})");

        if (LogVerbosity > 2)
        {
            Log($"  Current location: {Location}");
            Log($"  New location: {location}");
        }

        // If the window is outside of the work area,
        // recalculate the position and continue.
        //When window isn't initialized yet, cannot determine screen size.
        if (allowOutsideWorkArea == false && _instance is not null)
        {
            var horizontalWindowEdge = location.X + Width;
            var verticalWindowEdge = location.Y + Height;

            var horizontalWorkAreaEdge = MainMonitor.WorkArea.Width;
            var verticalWorkAreaEdge = MainMonitor.WorkArea.Height;

            var isOutsideHorizontalWorkArea = horizontalWindowEdge > horizontalWorkAreaEdge;
            var isOutsideVerticalWorkArea = verticalWindowEdge > verticalWorkAreaEdge;

            var locationInsideWorkArea = new Point(
                isOutsideHorizontalWorkArea ? horizontalWorkAreaEdge - Width : location.X,
                isOutsideVerticalWorkArea ? verticalWorkAreaEdge - Height : location.Y
            );

            location = locationInsideWorkArea;
        }

        // Bug:
        // For some reason the vertical position is not handled correctly.
        // Whenever a positive value is set, the window appears at the
        // very bottom of the screen and the only visible thing is the
        // application window title bar. As a workaround we make a
        // negative value out of the vertical position to "pull" the window up.
        // Note:
        // This behavior seems to be a macOS thing. In the Photino.Native
        // project files it is commented to be expected behavior for macOS.
        // There is some code trying to mitigate this problem but it might
        // not work as expected. Further investigation is necessary.
        // Update:
        // This behavior seems to have changed with macOS Sonoma.
        // Therefore we determine the version of macOS and only apply the
        // workaround for older versions.
        if (IsMacOsPlatform && MacOsVersion?.Major < 23)
        {
            var workArea = MainMonitor.WorkArea;
            location.Y = location.Y >= 0
                ? location.Y - workArea.Height
                : location.Y;
        }

        Location = location;

        return this;
    }

    /// <summary>
    /// Moves the native window to the specified location on the screen in pixels
    /// using <see cref="PhotinoWindow.Left"/> (X) and <see cref="PhotinoWindow.Top"/> (Y) properties.
    /// </summary>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <param name="left">Position from left in pixels</param>
    /// <param name="top">Position from top in pixels</param>
    /// <param name="allowOutsideWorkArea">Whether the window can go off-screen (work area)</param>
    public PhotinoWindow MoveTo(int left, int top, bool allowOutsideWorkArea = false)
    {
        Log($".MoveTo({left}, {top}, {allowOutsideWorkArea})");
        return MoveTo(new Point(left, top), allowOutsideWorkArea);
    }

    /// <summary>
    /// Moves the native window relative to its current location on the screen
    /// using a <see cref="Point"/>.
    /// </summary>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <param name="offset">Relative offset</param>
    public PhotinoWindow Offset(Point offset)
    {
        Log($".Offset({offset})");
        var location = Location;
        var left = location.X + offset.X;
        var top = location.Y + offset.Y;
        return MoveTo(left, top);
    }

    public PhotinoWindow SetDarkmode(bool darkmode)
    {
        Log($".Darkmode({darkmode})");
        Darkmode = darkmode;
        return this;
    }

    /// <summary>
    /// Moves the native window relative to its current location on the screen in pixels
    /// using <see cref="PhotinoWindow.Left"/> (X) and <see cref="PhotinoWindow.Top"/> (Y) properties.
    /// </summary>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <param name="left">Relative offset from left in pixels</param>
    /// <param name="top">Relative offset from top in pixels</param>
    public PhotinoWindow Offset(int left, int top)
    {
        Log($".Offset({left}, {top})");
        return Offset(new Point(left, top));
    }

    /// <summary>
    /// When true, the native window will appear without a title bar or border.
    /// By default, this is set to false.
    /// </summary>
    /// <remarks>
    /// The user has to supply titlebar, border, dragging and resizing manually.
    /// </remarks>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <param name="chromeless">Whether the window should be chromeless</param>
    public PhotinoWindow SetChromeless(bool chromeless)
    {
        Log($".SetChromeless({chromeless})");
        if (_instance is not null)
        {
            throw new ApplicationException("Chromeless can only be set before the native window is instantiated.");
        }

        _startupParameters.Chromeless = chromeless;
        return this;
    }

    /// <summary>
    /// When true, the native window can be displayed with transparent background.
    /// Chromeless must be set to true. Html document's body background must have alpha-based value.
    /// By default, this is set to false.
    /// </summary>
    public PhotinoWindow SetTransparent(bool enabled)
    {
        Log($".SetTransparent({enabled})");
        Transparent = enabled;
        return this;
    }

    /// <summary>
    /// When true, the user can access the browser control's context menu.
    /// By default, this is set to true.
    /// </summary>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <param name="enabled">Whether the context menu should be available</param>
    public PhotinoWindow SetContextMenuEnabled(bool enabled)
    {
        Log($".SetContextMenuEnabled({enabled})");
        ContextMenuEnabled = enabled;
        return this;
    }

    /// <summary>
    /// When true, the user can access the browser control's developer tools.
    /// By default, this is set to true.
    /// </summary>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <param name="enabled">Whether developer tools should be available</param>
    public PhotinoWindow SetDevToolsEnabled(bool enabled)
    {
        Log($".SetDevTools({enabled})");
        DevToolsEnabled = enabled;
        return this;
    }

    /// <summary>
    /// When set to true, the native window will cover the entire screen, similar to kiosk mode.
    /// By default, this is set to false.
    /// </summary>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <param name="fullScreen">Whether the window should be fullscreen</param>
    public PhotinoWindow SetFullScreen(bool fullScreen)
    {
        Log($".SetFullScreen({fullScreen})");
        FullScreen = fullScreen;
        return this;
    }

    ///<summary>
    /// When set to true, the native browser control grants all requests for access to local resources
    /// such as the users camera and microphone. By default, this is set to true.
    /// </summary>
    /// <remarks>
    /// This only works on Windows.
    /// </remarks>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <param name="grant">Whether permissions should be automatically granted.</param>
    public PhotinoWindow SetGrantBrowserPermissions(bool grant)
    {
        Log($".SetGrantBrowserPermission({grant})");
        GrantBrowserPermissions = grant;
        return this;
    }

    /// <summary>
    /// Sets <see cref="PhotinoWindow.UserAgent"/>. Sets the user agent on the browser control at initialization.
    /// </summary>
    /// <param name="userAgent"></param>
    /// <returns>Returns the current <see cref="PhotinoWindow"/> instance.</returns>
    public PhotinoWindow SetUserAgent(string userAgent)
    {
        Log($".SetUserAgent({userAgent})");
        UserAgent = userAgent;
        return this;
    }

    /// <summary>
    /// Sets <see cref="PhotinoWindow.BrowserControlInitParameters"/> platform specific initialization parameters for the native browser control on startup.
    /// Default is none.
    /// <remarks>
    /// WINDOWS: WebView2 specific string. Space separated.
    /// https://peter.sh/experiments/chromium-command-line-switches/
    /// https://learn.microsoft.com/en-us/dotnet/api/microsoft.web.webview2.core.corewebview2environmentoptions.additionalbrowserarguments?view=webview2-dotnet-1.0.1938.49&viewFallbackFrom=webview2-dotnet-1.0.1901.177view%3Dwebview2-1.0.1901.177
    /// https://www.chromium.org/developers/how-tos/run-chromium-with-flags/
    /// LINUX: Webkit2Gtk specific string. Enter parameter names and values as JSON string.
    /// e.g. { "set_enable_encrypted_media": true }
    /// https://webkitgtk.org/reference/webkit2gtk/2.5.1/WebKitSettings.html
    /// https://lazka.github.io/pgi-docs/WebKit2-4.0/classes/Settings.html
    /// MAC: Webkit specific string. Enter parameter names and values as JSON string.
    /// e.g. { "minimumFontSize": 8 }
    /// https://developer.apple.com/documentation/webkit/wkwebviewconfiguration?language=objc
    /// https://developer.apple.com/documentation/webkit/wkpreferences?language=objc
    /// </remarks>
    /// <param name="parameters"></param>
    /// <returns>Returns the current <see cref="PhotinoWindow"/> instance.</returns>
    /// </summary>
    public PhotinoWindow SetBrowserControlInitParameters(string parameters)
    {
        Log($".SetBrowserControlInitParameters({parameters})");
        BrowserControlInitParameters = parameters;
        return this;
    }

    /// <summary>
    /// Sets the registration id for toast notifications.
    /// </summary>
    /// <remarks>
    /// Only available on Windows.
    /// Defaults to window title if not specified.
    /// </remarks>
    /// <exception cref="ApplicationException">
    /// Thrown if platform is not Windows.
    /// </exception>
    /// <param name="notificationRegistrationId"></param>
    /// <returns>Returns the current <see cref="PhotinoWindow"/> instance.</returns>
    public PhotinoWindow SetNotificationRegistrationId(string notificationRegistrationId)
    {
        Log($".SetNotificationRegistrationId({notificationRegistrationId})");
        NotificationRegistrationId = notificationRegistrationId;
        return this;
    }

    /// <summary>
    /// Sets <see cref="PhotinoWindow.MediaAutoplayEnabled"/> on the browser control at initialization.
    /// </summary>
    /// <param name="enable"></param>
    /// <returns>Returns the current <see cref="PhotinoWindow"/> instance.</returns>
    public PhotinoWindow SetMediaAutoplayEnabled(bool enable)
    {
        Log($".SetMediaAutoplayEnabled({enable})");
        MediaAutoplayEnabled = enable;
        return this;
    }

    /// <summary>
    /// Sets <see cref="PhotinoWindow.FileSystemAccessEnabled"/> on the browser control at initialization.
    /// </summary>
    /// <param name="enable"></param>
    /// <returns>Returns the current <see cref="PhotinoWindow"/> instance.</returns>
    public PhotinoWindow SetFileSystemAccessEnabled(bool enable)
    {
        Log($".SetFileSystemAccessEnabled({enable})");
        FileSystemAccessEnabled = enable;
        return this;
    }

    /// <summary>
    /// Sets <see cref="PhotinoWindow.WebSecurityEnabled"/> on the browser control at initialization.
    /// </summary>
    /// <param name="enable"></param>
    /// <returns>Returns the current <see cref="PhotinoWindow"/> instance.</returns>
    public PhotinoWindow SetWebSecurityEnabled(bool enable)
    {
        Log($".SetWebSecurityEnabled({enable})");
        WebSecurityEnabled = enable;
        return this;
    }

    /// <summary>
    /// Sets <see cref="PhotinoWindow.JavascriptClipboardAccessEnabled"/> on the browser control at initialization.
    /// </summary>
    /// <param name="enable"></param>
    /// <returns>Returns the current <see cref="PhotinoWindow"/> instance.</returns>
    public PhotinoWindow SetJavascriptClipboardAccessEnabled(bool enable)
    {
        Log($".SetJavascriptClipboardAccessEnabled({enable})");
        JavascriptClipboardAccessEnabled = enable;
        return this;
    }

    /// <summary>
    /// Sets <see cref="PhotinoWindow.MediaStreamEnabled"/> on the browser control at initialization.
    /// </summary>
    /// <param name="enable"></param>
    /// <returns>Returns the current <see cref="PhotinoWindow"/> instance.</returns>
    public PhotinoWindow SetMediaStreamEnabled(bool enable)
    {
        Log($".SetMediaStreamEnabled({enable})");
        MediaStreamEnabled = enable;
        return this;
    }

    /// <summary>
    /// Sets <see cref="PhotinoWindow.SmoothScrollingEnabled"/> on the browser control at initialization.
    /// </summary>
    /// <param name="enable"></param>
    /// <returns>Returns the current <see cref="PhotinoWindow"/> instance.</returns>
    public PhotinoWindow SetSmoothScrollingEnabled(bool enable)
    {
        Log($".SetSmoothScrollingEnabled({enable})");
        SmoothScrollingEnabled = enable;
        return this;
    }

    /// <summary>
    /// Sets <see cref="PhotinoWindow.IgnoreCertificateErrorsEnabled"/> on the browser control at initialization.
    /// </summary>
    /// <param name="enable"></param>
    /// <returns>Returns the current <see cref="PhotinoWindow"/> instance.</returns>
    public PhotinoWindow SetIgnoreCertificateErrorsEnabled(bool enable)
    {
        Log($".SetIgnoreCertificateErrorsEnabled({enable})");
        IgnoreCertificateErrorsEnabled = enable;
        return this;
    }

    /// <summary>
    /// Sets whether ShowNotification() can be called.
    /// </summary>
    /// <remarks>
    /// Only available on Windows.
    /// </remarks>
    /// <exception cref="ApplicationException">
    /// Thrown if platform is not Windows.
    /// </exception>
    /// <param name="enable"></param>
    /// <returns>Returns the current <see cref="PhotinoWindow"/> instance.</returns>
    public PhotinoWindow SetNotificationsEnabled(bool enable)
    {
        Log($".SetNotificationsEnabled({enable})");
        NotificationsEnabled = enable;
        return this;
    }

    /// <summary>
    /// Sets the native window <see cref="PhotinoWindow.Height"/> in pixels.
    /// Default is 0.
    /// </summary>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <seealso cref="UseOsDefaultSize"/>
    /// <param name="height">Height in pixels</param>
    public PhotinoWindow SetHeight(int height)
    {
        Log($".SetHeight({height})");
        Height = height;
        return this;
    }

    /// <summary>
    /// Sets the icon file for the native window title bar.
    /// The file must be located on the local machine and cannot be a URL. The default is none.
    /// </summary>
    /// <remarks>
    /// This only works on Windows and Linux.
    /// </remarks>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <exception cref="System.ArgumentException">Icon file: {value} does not exist.</exception>
    /// <param name="iconFile">The file path to the icon.</param>
    public PhotinoWindow SetIconFile(string iconFile)
    {
        Log($".SetIconFile({iconFile})");
        IconFile = iconFile;
        return this;
    }

    /// <summary>
    /// Sets the native window to a new <see cref="PhotinoWindow.Left"/> (X) coordinate in pixels.
    /// Default is 0.
    /// </summary>
    /// <seealso cref="UseOsDefaultLocation" />
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <param name="left">Position in pixels from the left (X).</param>
    public PhotinoWindow SetLeft(int left)
    {
        Log($".SetLeft({Left})");
        Left = left;
        return this;
    }

    /// <summary>
    /// Sets whether the native window can be resized by the user.
    /// Default is true.
    /// </summary>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <param name="resizable">Whether the window is resizable</param>
    public PhotinoWindow SetResizable(bool resizable)
    {
        Log($".SetResizable({resizable})");
        Resizable = resizable;
        return this;
    }

    /// <summary>
    /// Sets the native window Size. This represents the <see cref="PhotinoWindow.Width"/> and the <see cref="PhotinoWindow.Height"/> of the window in pixels.
    /// The default Size is 0,0.
    /// </summary>
    /// <seealso cref="UseOsDefaultSize"/>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <param name="size">Width &amp; Height</param>
    public PhotinoWindow SetSize(Size size)
    {
        Log($".SetSize({size})");
        Size = size;
        return this;
    }

    /// <summary>
    /// Sets the native window Size. This represents the <see cref="PhotinoWindow.Width"/> and the <see cref="PhotinoWindow.Height"/> of the window in pixels.
    /// The default Size is 0,0.
    /// </summary>
    /// <seealso cref="UseOsDefaultSize"/>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <param name="width">Width in pixels</param>
    /// <param name="height">Height in pixels</param>
    public PhotinoWindow SetSize(int width, int height)
    {
        Log($".SetSize({width}, {height})");
        Size = new Size(width, height);
        return this;
    }

    /// <summary>
    /// Sets the native window <see cref="PhotinoWindow.Left"/> (X) and <see cref="PhotinoWindow.Top"/> coordinates (Y) in pixels.
    /// Default is 0,0 which means the window will be aligned to the top left edge of the screen.
    /// </summary>
    /// <seealso cref="UseOsDefaultLocation" />
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <param name="location">Location as a <see cref="Point"/></param>
    public PhotinoWindow SetLocation(Point location)
    {
        Log($".SetLocation({location})");
        Location = location;
        return this;
    }

    /// <summary>
    /// Sets the logging verbosity to standard output (Console/Terminal).
    /// 0 = Critical Only
    /// 1 = Critical and Warning
    /// 2 = Verbose
    /// >2 = All Details
    /// Default is 2.
    /// </summary>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <param name="verbosity">Verbosity as integer</param>
    public PhotinoWindow SetLogVerbosity(int verbosity)
    {
        Log($".SetLogVerbosity({verbosity})");
        LogVerbosity = verbosity;
        return this;
    }

    /// <summary>
    /// Sets whether the native window is maximized.
    /// Default is false.
    /// </summary>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <param name="maximized">Whether the window should be maximized.</param>
    public PhotinoWindow SetMaximized(bool maximized)
    {
        Log($".SetMaximized({maximized})");
        Maximized = maximized;
        return this;
    }

    ///<summary>Native window maximum Width and Height in pixels.</summary>
    public PhotinoWindow SetMaxSize(int maxWidth, int maxHeight)
    {
        Log($".SetMaxSize({maxWidth}, {maxHeight})");
        MaxSize = new Point(maxWidth, maxHeight);
        return this;
    }

    ///<summary>Native window maximum Height in pixels.</summary>
    public PhotinoWindow SetMaxHeight(int maxHeight)
    {
        Log($".SetMaxHeight({maxHeight})");
        MaxHeight = maxHeight;
        return this;
    }

    ///<summary>Native window maximum Width in pixels.</summary>
    public PhotinoWindow SetMaxWidth(int maxWidth)
    {
        Log($".SetMaxWidth({maxWidth})");
        MaxWidth = maxWidth;
        return this;
    }

    /// <summary>
    /// Sets whether the native window is minimized (hidden).
    /// Default is false.
    /// </summary>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <param name="minimized">Whether the window should be minimized.</param>
    public PhotinoWindow SetMinimized(bool minimized)
    {
        Log($".SetMinimized({minimized})");
        Minimized = minimized;
        return this;
    }

    ///<summary>Native window maximum Width and Height in pixels.</summary>
    public PhotinoWindow SetMinSize(int minWidth, int minHeight)
    {
        Log($".SetMinSize({minWidth}, {minHeight})");
        MinSize = new Point(minWidth, minHeight);
        return this;
    }

    ///<summary>Native window maximum Height in pixels.</summary>
    public PhotinoWindow SetMinHeight(int minHeight)
    {
        Log($".SetMinHeight({minHeight})");
        MinHeight = minHeight;
        return this;
    }

    ///<summary>Native window maximum Width in pixels.</summary>
    public PhotinoWindow SetMinWidth(int minWidth)
    {
        Log($".SetMinWidth({minWidth})");
        MinWidth = minWidth;
        return this;
    }

    /// <summary>
    /// Sets the local path to store temp files for browser control.
    /// Default is the user's AppDataLocal folder.
    /// </summary>
    /// <remarks>
    /// Only available on Windows.
    /// </remarks>
    /// <exception cref="ApplicationException">
    /// Thrown if platform is not Windows.
    /// </exception>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <param name="tempFilesPath">Path to temp files directory.</param>
    public PhotinoWindow SetTemporaryFilesPath(string tempFilesPath)
    {
        Log($".SetTemporaryFilesPath({tempFilesPath})");
        TemporaryFilesPath = tempFilesPath;
        return this;
    }

    /// <summary>
    /// Sets the native window <see cref="PhotinoWindow.Title"/>.
    /// Default is "Photino".
    /// </summary>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <param name="title">Window title</param>
    public PhotinoWindow SetTitle(string title)
    {
        Log($".SetTitle({title})");
        Title = title;
        return this;
    }

    /// <summary>
    /// Sets the native window <see cref="PhotinoWindow.Top"/> (Y) coordinate in pixels.
    /// Default is 0.
    /// </summary>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <seealso cref="UseOsDefaultLocation"/>
    /// <param name="top">Position in pixels from the top (Y).</param>
    public PhotinoWindow SetTop(int top)
    {
        Log($".SetTop({top})");
        Top = top;
        return this;
    }

    /// <summary>
    /// Sets whether the native window is always at the top of the z-order.
    /// Default is false.
    /// </summary>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <param name="topMost">Whether the window is at the top</param>
    public PhotinoWindow SetTopMost(bool topMost)
    {
        Log($".SetTopMost({topMost})");
        Topmost = topMost;
        return this;
    }

    /// <summary>
    /// Sets the native window width in pixels.
    /// Default is 0.
    /// </summary>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <seealso cref="UseOsDefaultSize"/>
    /// <param name="width">Width in pixels</param>
    public PhotinoWindow SetWidth(int width)
    {
        Log($".SetWidth({width})");
        Width = width;
        return this;
    }

    /// <summary>
    /// Sets the native browser control <see cref="PhotinoWindow.Zoom"/>.
    /// Default is 100.
    /// </summary>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <param name="zoom">Zoomlevel as integer</param>
    /// <example>100 = 100%, 50 = 50%</example>
    public PhotinoWindow SetZoom(int zoom)
    {
        Log($".SetZoom({zoom})");
        Zoom = zoom;
        return this;
    }

    /// <summary>
    /// When true the native window starts up at the OS Default location.
    /// Default is true.
    /// </summary>
    /// <remarks>
    /// Overrides <see cref="PhotinoWindow.Left"/> (X) and <see cref="PhotinoWindow.Top"/> (Y) properties.
    /// </remarks>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <param name="useOsDefault">Whether the OS Default should be used.</param>
    public PhotinoWindow SetUseOsDefaultLocation(bool useOsDefault)
    {
        Log($".SetUseOsDefaultLocation({useOsDefault})");
        UseOsDefaultLocation = useOsDefault;
        return this;
    }

    /// <summary>
    /// When true the native window starts at the OS Default size.
    /// Default is true.
    /// </summary>
    /// <remarks>
    /// Overrides <see cref="PhotinoWindow.Height"/> and <see cref="PhotinoWindow.Width"/> properties.
    /// </remarks>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <param name="useOsDefault">Whether the OS Default should be used.</param>
    public PhotinoWindow SetUseOsDefaultSize(bool useOsDefault)
    {
        Log($".SetUseOsDefaultSize({useOsDefault})");
        UseOsDefaultSize = useOsDefault;
        return this;
    }

    /// <summary>
    /// Set runtime path for WebView2 so that developers can use Photino on Windows using the "Fixed Version" deployment module of the WebView2 runtime.
    /// </summary>
    /// <remarks>
    /// This only works on Windows.
    /// </remarks>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <seealso href="https://docs.microsoft.com/en-us/microsoft-edge/webview2/concepts/distribution" />
    /// <param name="data">Runtime path for WebView2</param>
    [SupportedOSPlatform("windows")]
    public PhotinoWindow Win32SetWebView2Path(string data)
    {
        if (IsWindowsPlatform)
        {
            Invoke(() => ((WindowsPhotino) _instance!).SetWebView2RuntimePath(data));
        }
        else
        {
            Log("Win32SetWebView2Path is only supported on the Windows platform");
        }

        return this;
    }

    /// <summary>
    /// Clears the auto-fill data in the browser control.
    /// </summary>
    /// <remarks>
    /// This method is only supported on the Windows platform.
    /// </remarks>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    public PhotinoWindow ClearBrowserAutoFill()
    {
        if (IsWindowsPlatform)
        {
            Invoke(() => _instance!.ClearBrowserAutoFill());
        }
        else
        {
            Log("ClearBrowserAutoFill is only supported on the Windows platform");
        }

        return this;
    }

    //NON-FLUENT METHODS - CAN ONLY BE CALLED AFTER WINDOW IS INITIALIZED
    //ONE OF THESE 2 METHODS *MUST* BE CALLED TO CREATE THE WINDOW

    /// <summary>
    /// Responsible for the initialization of the primary native window and remains in operation until the window is closed.
    /// This method is also applicable for initializing child windows, but in this case, it does not inhibit operation.
    /// </summary>
    /// <remarks>
    /// The operation of the message loop is exclusive to the main native window only.
    /// </remarks>
    public void WaitForClose()
    {
        //fill in the fixed size array of custom scheme names
        foreach (var name in CustomSchemes.Take(16))
        {
            _startupParameters.CustomSchemeNames!.Add(name.Key);
        }

        _startupParameters.ParentInstance = _dotNetParent?._instance ?? null;

        var errors = _startupParameters.GetParamErrors();
        if (errors.Count == 0)
        {
            OnWindowCreating();
            try
            {
                Invoke(() => _instance = PhotinoFactory.Create(_startupParameters));
            }
            catch (Exception ex)
            {
                Log($"Exception thrown: {ex.Message}");
                throw new ApplicationException(ex.Message);
            }

            OnWindowCreated();

            if (!_messageLoopIsStarted)
            {
                _messageLoopIsStarted = true;
                try
                {
                    Invoke(() => _instance!.WaitForExit()); // start the message loop. there can only be 1 message loop for all windows.
                }
                catch (Exception ex)
                {
                    Log($"Exception thrown: {ex.Message}");
                    throw new ApplicationException(ex.Message);
                }
            }
        }
        else
        {
            var formattedErrors = "\n";
            foreach (var error in errors)
            {
                formattedErrors += error + "\n";
            }

            throw new ArgumentException($"Startup Parameters Are Not Valid: {formattedErrors}");
        }
    }

    /// <summary>
    /// Closes the native window.
    /// </summary>
    /// <exception cref="ApplicationException">
    /// Thrown when the window is not initialized.
    /// </exception>
    public void Close()
    {
        Log(".Close()");
        if (_instance is null)
        {
            throw new ApplicationException("Close cannot be called until after the Photino window is initialized.");
        }

        Invoke(() => _instance.Close());
    }

    /// <summary>
    /// Send a message to the native window's native browser control's JavaScript context.
    /// </summary>
    /// <remarks>
    /// In JavaScript, messages can be received via <code>window.external.receiveMessage(message)</code>
    /// </remarks>
    /// <exception cref="ApplicationException">
    /// Thrown when the window is not initialized.
    /// </exception>
    /// <param name="message">Message as string</param>
    public async Task SendWebMessageAsync(string message)
    {
        await Task.Run(() =>
        {
            Log($".SendWebMessage({message})");
            if (_instance is null)
            {
                throw new ApplicationException("SendWebMessage cannot be called until after the Photino window is initialized.");
            }

            Invoke(() => _instance.SendWebMessage(message));
        });
    }

    /// <summary>
    /// Sends a native notification to the OS.
    /// Sometimes referred to as Toast notifications.
    /// </summary>
    /// <exception cref="ApplicationException">
    /// Thrown when the window is not initialized.
    /// </exception>
    /// <param name="title">The title of the notification</param>
    /// <param name="body">The text of the notification</param>
    public async Task SendNotificationAsync(string title, string body)
    {
        await Task.Run(() =>
        {
            Log($".SendNotification({title}, {body})");
            if (_instance is null)
            {
                throw new ApplicationException("SendNotification cannot be called until after the Photino window is initialized.");
            }

            Invoke(() => _instance.ShowNotification(title, body));
        });
    }

    /// <summary>
    /// Async version is required for PhotinoEx.Blazor
    /// </summary>
    /// <remarks>
    /// Filter names are not used on macOS. Use async version for PhotinoEx.Blazor as syncronous version crashes.
    /// </remarks>
    /// <exception cref="ApplicationException">
    /// Thrown when the window is not initialized.
    /// </exception>
    /// <param name="title">Title of the dialog</param>
    /// <param name="defaultPath">Default path. Defaults to <see cref="Environment.SpecialFolder.MyDocuments"/></param>
    /// <param name="multiSelect">Whether multiple selections are allowed</param>
    /// <param name="filterPatterns">List of filtering.</param>
    /// <returns>Array of file paths as strings</returns>
    public async Task<List<string>?> ShowOpenFileDialogAsync(string title = "Choose file", string? defaultPath = null,
        bool multiSelect = false, List<FileFilter>? filterPatterns = null)
    {
        return await _instance!.ShowOpenFileAsync(title, defaultPath, multiSelect, filterPatterns);
    }

    /// <summary>
    /// Async version is required for PhotinoEx.Blazor
    /// </summary>
    /// <exception cref="ApplicationException">
    /// Thrown when the window is not initialized.
    /// </exception>
    /// <param name="title">Title of the dialog</param>
    /// <param name="defaultPath">Default path. Defaults to <see cref="Environment.SpecialFolder.MyDocuments"/></param>
    /// <param name="multiSelect">Whether multiple selections are allowed</param>
    /// <returns>Array of folder paths as strings</returns>
    public async Task<List<string>?> ShowOpenFolderDialogAsync(string title = "Choose file", string? defaultPath = null,
        bool multiSelect = false)
    {
        return await _instance!.ShowOpenFolderAsync(title, defaultPath, multiSelect);
    }

    /// <summary>
    /// Async version is required for PhotinoEx.Blazor
    /// </summary>
    /// <remarks>
    /// Filter names are not used on macOS.
    /// </remarks>
    /// <exception cref="ApplicationException">
    /// Thrown when the window is not initialized.
    /// </exception>
    /// <param name="title">Title of the dialog</param>
    /// <param name="defaultPath">Default path. Defaults to <see cref="Environment.SpecialFolder.MyDocuments"/></param>
    /// <param name="filterPatterns">Array for filtering.</param>
    /// <returns></returns>
    public async Task<string?> ShowSaveFileDialogAsync(string title = "Choose file", string? defaultPath = null,
        List<FileFilter>? filterPatterns = null)
    {
        defaultPath ??= Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        filterPatterns ??= new List<FileFilter>();

        var nativeFilters = GetNativeFilters(filterPatterns);

        return await _instance!.ShowSaveFileAsync(title, defaultPath, nativeFilters);
    }

    /// <summary>
    /// Show a message dialog native to the OS.
    /// </summary>
    /// <exception cref="ApplicationException">
    /// Thrown when the window is not initialized.
    /// </exception>
    /// <param name="title">Title of the dialog</param>
    /// <param name="text">Text of the dialog</param>
    /// <param name="buttons">Available interaction buttons <see cref="DialogButtons"/></param>
    /// <param name="icon">Icon of the dialog <see cref="DialogButtons"/></param>
    /// <returns><see cref="DialogResult" /></returns>
    public async Task<DialogResult> ShowMessageDialogAsync(string title, string text, DialogButtons buttons = DialogButtons.Ok,
        DialogIcon icon = DialogIcon.Info)
    {
        return await _instance!.ShowMessageAsync(title, text, buttons, icon);
    }

    /// <summary>
    /// Logs a message.
    /// TODO: change to use .net logging
    /// </summary>
    /// <param name="message">Log message</param>
    private void Log(string message)
    {
        if (LogVerbosity < 1)
        {
            return;
        }

        Console.WriteLine($"Photino.NET: \"{Title}\"{message}");
    }

    /// <summary>
    /// Returns an array of strings for native filters
    /// </summary>
    /// <param name="filters"></param>
    /// <param name="empty"></param>
    /// <returns>String array of filters</returns>
    private static List<FileFilter> GetNativeFilters(List<FileFilter>? filters, bool empty = false)
    {
        var nativeFilters = new List<FileFilter>();

        if (empty || filters == null || !filters.Any())
        {
            return nativeFilters;
        }

        if (IsMacOsPlatform)
        {
            // macOS: Remove wildcards from spec (e.g., "*.txt" -> "txt", "*.*" -> "*")
            foreach (var filter in filters)
            {
                var specs = filter.Spec.Split(';');
                var macSpecs = specs.Select(s =>
                    s == "*.*" || s == "*" ? "*" : s.TrimStart('*', '.'));
                nativeFilters.Add(new FileFilter(filter.Name, string.Join(";", macSpecs)));
            }
        }
        else
        {
            // Windows/Linux: Ensure wildcards are present (e.g., "txt" -> "*.txt")
            foreach (var filter in filters)
            {
                var specs = filter.Spec.Split(';');
                var winSpecs = specs.Select(s =>
                {
                    if (s == "*" || s == "*.*") return "*.*";
                    if (s.StartsWith("*.")) return s;
                    if (s.StartsWith(".")) return $"*{s}";
                    return $"*.{s}";
                });
                nativeFilters.Add(new FileFilter(filter.Name, string.Join(";", winSpecs)));
            }
        }

        return nativeFilters;
    }

    #endregion

    #region Events

    //FLUENT EVENT HANDLER REGISTRATION
    public event EventHandler<Point>? WindowLocationChanged;

    /// <summary>
    /// Registers user-defined handler methods to receive callbacks from the native window when its location changes.
    /// </summary>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <param name="handler"><see cref="EventHandler"/></param>
    public PhotinoWindow RegisterLocationChangedHandler(EventHandler<Point> handler)
    {
        WindowLocationChanged += handler;
        return this;
    }

    /// <summary>
    /// Invokes registered user-defined handler methods when the native window's location changes.
    /// </summary>
    /// <param name="left">Position from left in pixels</param>
    /// <param name="top">Position from top in pixels</param>
    internal void OnLocationChanged(int left, int top)
    {
        var location = new Point(left, top);
        WindowLocationChanged?.Invoke(this, location);
    }

    public event EventHandler<Size>? WindowSizeChanged;

    /// <summary>
    /// Registers user-defined handler methods to receive callbacks from the native window when its size changes.
    /// </summary>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <param name="handler"><see cref="EventHandler"/></param>
    public PhotinoWindow RegisterSizeChangedHandler(EventHandler<Size> handler)
    {
        WindowSizeChanged += handler;
        return this;
    }

    /// <summary>
    /// Invokes registered user-defined handler methods when the native window's size changes.
    /// </summary>
    internal void OnSizeChanged(int width, int height)
    {
        var size = new Size(width, height);
        WindowSizeChanged?.Invoke(this, size);
    }

    public event EventHandler? WindowFocusIn;

    /// <summary>
    /// Registers registered user-defined handler methods to receive callbacks from the native window when it is focused in.
    /// </summary>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <param name="handler"><see cref="EventHandler"/></param>
    public PhotinoWindow RegisterFocusInHandler(EventHandler handler)
    {
        WindowFocusIn += handler;
        return this;
    }

    /// <summary>
    /// Invokes registered user-defined handler methods when the native window focuses in.
    /// </summary>
    internal void OnFocusIn()
    {
        WindowFocusIn?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? WindowMaximized;

    /// <summary>
    /// Registers user-defined handler methods to receive callbacks from the native window when it is maximized.
    /// </summary>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <param name="handler"><see cref="EventHandler"/></param>
    public PhotinoWindow RegisterMaximizedHandler(EventHandler handler)
    {
        WindowMaximized += handler;
        return this;
    }

    /// <summary>
    /// Invokes registered user-defined handler methods when the native window is maximized.
    /// </summary>
    internal void OnMaximized()
    {
        WindowMaximized?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? WindowRestored;

    /// <summary>
    /// Registers user-defined handler methods to receive callbacks from the native window when it is restored.
    /// </summary>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <param name="handler"><see cref="EventHandler"/></param>
    public PhotinoWindow RegisterRestoredHandler(EventHandler handler)
    {
        WindowRestored += handler;
        return this;
    }

    /// <summary>
    /// Invokes registered user-defined handler methods when the native window is restored.
    /// </summary>
    internal void OnRestored()
    {
        WindowRestored?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? WindowFocusOut;

    /// <summary>
    /// Registers registered user-defined handler methods to receive callbacks from the native window when it is focused out.
    /// </summary>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <param name="handler"><see cref="EventHandler"/></param>
    public PhotinoWindow RegisterFocusOutHandler(EventHandler handler)
    {
        WindowFocusOut += handler;
        return this;
    }

    /// <summary>
    /// Invokes registered user-defined handler methods when the native window focuses out.
    /// </summary>
    internal void OnFocusOut()
    {
        WindowFocusOut?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? WindowMinimized;

    /// <summary>
    /// Registers user-defined handler methods to receive callbacks from the native window when it is minimized.
    /// </summary>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <param name="handler"><see cref="EventHandler"/></param>
    public PhotinoWindow RegisterMinimizedHandler(EventHandler handler)
    {
        WindowMinimized += handler;
        return this;
    }

    /// <summary>
    /// Invokes registered user-defined handler methods when the native window is minimized.
    /// </summary>
    internal void OnMinimized()
    {
        WindowMinimized?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler<string>? WebMessageReceived;

    /// <summary>
    /// Registers user-defined handler methods to receive callbacks from the native window when it sends a message.
    /// </summary>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <remarks>
    /// Messages can be sent from JavaScript via <code>window.external.sendMessage(message)</code>
    /// </remarks>
    /// <param name="handler"><see cref="EventHandler"/></param>
    public PhotinoWindow RegisterWebMessageReceivedHandler(EventHandler<string> handler)
    {
        WebMessageReceived += handler;
        return this;
    }

    /// <summary>
    /// Invokes registered user-defined handler methods when the native window sends a message.
    /// </summary>
    internal void OnWebMessageReceived(string message)
    {
        WebMessageReceived?.Invoke(this, message);
    }

    public delegate bool NetClosingDelegate(object sender, EventArgs? e);

    public event NetClosingDelegate? WindowClosing;

    /// <summary>
    /// Registers user-defined handler methods to receive callbacks from the native window when the window is about to close.
    /// Handler can return true to prevent the window from closing.
    /// </summary>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <param name="handler"><see cref="NetClosingDelegate"/></param>
    public PhotinoWindow RegisterWindowClosingHandler(NetClosingDelegate handler)
    {
        WindowClosing += handler;
        return this;
    }

    /// <summary>
    /// Invokes registered user-defined handler methods when the native window is about to close.
    /// </summary>
    internal bool OnWindowClosing()
    {
        return WindowClosing?.Invoke(this, null) ?? false;
    }

    public event EventHandler? WindowCreating;

    /// <summary>
    /// Registers user-defined handler methods to receive callbacks before the native window is created.
    /// </summary>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <param name="handler"><see cref="EventHandler"/></param>
    public PhotinoWindow RegisterWindowCreatingHandler(EventHandler handler)
    {
        WindowCreating += handler;
        return this;
    }

    /// <summary>
    /// Invokes registered user-defined handler methods before the native window is created.
    /// </summary>
    internal void OnWindowCreating()
    {
        WindowCreating?.Invoke(this, null!);
    }

    public event EventHandler? WindowCreated;

    /// <summary>
    /// Registers user-defined handler methods to receive callbacks after the native window is created.
    /// </summary>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <param name="handler"><see cref="EventHandler"/></param>
    public PhotinoWindow RegisterWindowCreatedHandler(EventHandler handler)
    {
        WindowCreated += handler;
        return this;
    }

    /// <summary>
    /// Invokes registered user-defined handler methods after the native window is created.
    /// </summary>
    internal void OnWindowCreated()
    {
        WindowCreated?.Invoke(this, null!);
    }


    //NOTE: There is 1 callback from C++ to C# which is automatically registered. The .NET callback appropriate for the custom scheme is handled in OnCustomScheme().

    public delegate Stream NetCustomSchemeDelegate(object sender, string scheme, string url, out string contentType);

    internal Dictionary<string, NetCustomSchemeDelegate> CustomSchemes = new();

    /// <summary>
    /// Registers user-defined custom schemes (other than 'http', 'https' and 'file') and handler methods to receive callbacks
    /// when the native browser control encounters them.
    /// </summary>
    /// <remarks>
    /// Only 16 custom schemes can be registered before initialization. Additional handlers can be added after initialization.
    /// </remarks>
    /// <returns>
    /// Returns the current <see cref="PhotinoWindow"/> instance.
    /// </returns>
    /// <param name="scheme">The custom scheme</param>
    /// <param name="handler"><see cref="EventHandler"/></param>
    /// <exception cref="ArgumentException">Thrown if no scheme or handler was provided</exception>
    /// <exception cref="ApplicationException">Thrown if more than 16 custom schemes were set</exception>
    public PhotinoWindow RegisterCustomSchemeHandler(string scheme, NetCustomSchemeDelegate handler)
    {
        if (string.IsNullOrWhiteSpace(scheme))
        {
            throw new ArgumentException("A scheme must be provided. (for example 'app' or 'custom'");
        }

        if (handler == null)
        {
            throw new ArgumentException("A handler (method) with a signature matching NetCustomSchemeDelegate must be supplied.");
        }

        scheme = scheme.ToLower();

        if (_instance is null)
        {
            if (CustomSchemes.Count > 15 && !CustomSchemes.ContainsKey(scheme))
            {
                throw new ApplicationException(
                    $"No more than 16 custom schemes can be set prior to initialization. Additional handlers can be added after initialization.");
            }
            else
            {
                if (!CustomSchemes.ContainsKey(scheme))
                {
                    CustomSchemes.Add(scheme, null!);
                }
            }
        }
        else
        {
            _instance.AddCustomSchemeName(scheme);
        }

        CustomSchemes[scheme] += handler;

        return this;
    }

    /// <summary>
    /// Invokes registered user-defined handler methods for user-defined custom schemes (other than 'http','https', and 'file')
    /// when the native browser control encounters them.
    /// </summary>
    /// <param name="url">URL of the Scheme</param>
    /// <param name="contentType">Content type of the response</param>
    /// <returns><see cref="IntPtr"/></returns>
    /// <exception cref="ApplicationException">
    /// Thrown when the URL does not contain a colon.
    /// </exception>
    /// <exception cref="ApplicationException">
    /// Thrown when no handler is registered.
    /// </exception>
    public MemoryStream OnCustomScheme(string url, out string contentType)
    {
        int length = url.IndexOf(':');
        string scheme = length >= 0
            ? url.Substring(0, length).ToLower()
            : throw new ApplicationException($"URL: '{url}' does not contain a colon.");

        if (!this.CustomSchemes.ContainsKey(scheme))
        {
            throw new ApplicationException($"A handler for the custom scheme '{scheme}' has not been registered.");
        }

        var result = this.CustomSchemes[scheme].Invoke(this, scheme, url, out contentType);

        var memoryStream = new MemoryStream();
        result.CopyTo(memoryStream);

        return memoryStream;
    }

    #endregion
}
