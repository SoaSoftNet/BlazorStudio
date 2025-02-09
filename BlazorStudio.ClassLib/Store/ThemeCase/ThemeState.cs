﻿using Fluxor;

namespace BlazorStudio.ClassLib.Store.ThemeCase;

[FeatureState]
public record ThemeState(ThemeKey ThemeKey)
{
    public ThemeState() : this(ThemeFacts.Default.Dark.BstudioDefaultDarkTheme)
    {

    }
}