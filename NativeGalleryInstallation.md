# Native Gallery Plugin Installation Guide

The `NativeGallery` plugin is required for Android functionality but is currently missing from your project. Please follow these steps to install it:

## Installation Steps (via UPM)

1.  Open your project in **Unity Editor**.
2.  Go to **Window** > **Package Manager**.
3.  Click the **+** button in the top-left corner and select **Add package from git URL...**.
4.  Paste the following URL:
    `https://github.com/yasirkula/UnityNativeGallery.git`
5.  Click **Add**. Unity will download and install the plugin automatically.

## Android Permissions
After installation, Unity might require you to set up permissions in the Player Settings:
1.  Go to **Project Settings** > **Player** > **Android**.
2.  Ensure **Write Permission** is set to **External (SDCard)** if you plan to save images.

## Why this happened?
The project code currently uses *Reflection* to check for `NativeGallery`. If the plugin files are not present in the `Assets` folder or as a Package, the reflection check fails, which is what you saw on Android.
