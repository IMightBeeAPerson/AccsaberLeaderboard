using AccsaberLeaderboard.Configuration;
using BeatSaberMarkupLanguage.Attributes;
using CountersPlus.ConfigModels;
using System.ComponentModel;

namespace AccsaberLeaderboard.Counter.Settings
{
    public class CounterConfigHandler : ConfigModel
    {
        private static PluginConfig PC => PluginConfig.Instance;

        [UIValue(nameof(DecimalPlaces))] public int DecimalPlaces
        {
            get => PC.DecimalPlaces;
            set => PC.DecimalPlaces = value;
        }
        [UIValue(nameof(FontSize))] public float FontSize
        {
            get => PC.FontSize;
            set => PC.FontSize = value;
        }

    }
}
