# LARP - A Simple DNS Switcher for Windows

LARP (a pun on Cloudflare WARP) is a simple DNS switcher for Windows that allows you to easily switch between different DNS configurations. It sits snugly on the system tray, with a simple indicator and lightweight UI made in native .NET Framework that is built-in on modern Windows.

## Why I made this

I used Cloudflare WARP a lot, but I noticed that the app is too bloated for such a simple task (apparently it uses Electr\*n framework, which I try to disdain for such a simple utility). So I want to make a lightweight solution that works with a native Windows tool.

## Features

- Simple, beautiful and modern UI inspired by Catppuccin theme.
- Supports Cloudflare `1.1.1.1` and Google `8.8.8.8` DNS providers, and also custom user-provided ones.
- Light and dark theme that syncs with OS theme.
- Can be set to open in startup
- Extremely lightweight and feels native.

## How to Build

```powershell
& C:\Windows\Microsoft.NET\Framework\v4.0.30319\csc.exe /target:winexe /out:larp.exe larp.cs
```