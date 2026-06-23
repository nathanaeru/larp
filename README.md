# LARP - A Simple DNS Switcher for Windows

![App logo](LIGHT.png)

LARP (a pun on Cloudflare WARP) is a simple DNS switcher for Windows that allows you to easily switch between different DNS configurations. It sits snugly on the system tray, with a simple indicator and lightweight UI made in native .NET Framework that is built-in on modern Windows.

## Why I made this

I used Cloudflare WARP a lot, but I noticed that the app is too bloated for such a simple task (apparently it uses Electr\*n framework, which I try to disdain for such a simple utility). So I want to make a lightweight solution that works with a native Windows tool.

## Features

- Simple, beautiful and modern UI.
- Supports Cloudflare `1.1.1.1` and Google `8.8.8.8` DNS providers, and also custom user-provided ones.
- Light and dark theme that syncs with OS theme.
- Can be set to open in startup
- Extremely lightweight.

## How to Build

Requirements: .NET Framework v4.0.30319 (built-in in Windows 10 or later).

```powershell
& C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe /target:winexe /win32icon:ICON.ico /out:build/larp.exe /resource:CONNECT.png /resource:DISCONNECT.png /resource:LIGHT.png /resource:DARK.png larp.cs
```

## How to Install

Download the latest setup file from the [Releases](https://github.com/nathanaeru/larp/releases/tag/Release) page, then install it.