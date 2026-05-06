using AccsaberLeaderboard.Configuration;
using AccsaberLeaderboard.Counter.Utils;
using BeatSaberMarkupLanguage.Attributes;
using CountersPlus.ConfigModels;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace AccsaberLeaderboard.Counter.Settings
{
    public class CounterConfigHandler : ConfigModel
    {
#pragma warning disable IDE0044
        private static PluginConfig PC => PluginConfig.Instance;

        [UIValue(nameof(DecimalPlaces))] private int DecimalPlaces
        {
            get => PC.DecimalPlaces;
            set => PC.DecimalPlaces = value;
        }
        [UIValue(nameof(FontSize))] private float FontSize
        {
            get => PC.FontSize;
            set => PC.FontSize = value;
        }
        [UIValue(nameof(CounterMode))] private CounterModes CounterMode
        {
            get => PC.CounterMode;
            set => PC.CounterMode = value;
        }
        [UIValue(nameof(CounterModeTypes))] private List<object> CounterModeTypes = [.. Enum.GetValues(typeof(CounterModes))];

    }
}
